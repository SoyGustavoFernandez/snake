using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SnakeQuest.API;

public class GameHub : Hub
{
    // Queue of players waiting for a quick online match
    private static readonly ConcurrentQueue<PlayerConnection> WaitingQueue = new();

    // Active rooms: RoomCode -> RoomInfo
    private static readonly ConcurrentDictionary<string, GameRoom> ActiveRooms = new();

    // Connection ID -> RoomCode mapping for easy disconnect cleanup
    private static readonly ConcurrentDictionary<string, string> ConnectionToRoom = new();

    public async Task JoinLobby(string playerName, string skinId)
    {
        var connectionId = Context.ConnectionId;

        // Try to match with someone in the queue
        while (WaitingQueue.TryDequeue(out var opponent))
        {
            // If the opponent connection is dead or it is ourselves, skip/requeue
            if (opponent.ConnectionId == connectionId)
            {
                continue;
            }

            // Create a room code
            string roomCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            var host = opponent;
            var guest = new PlayerConnection(connectionId, playerName, skinId);

            var room = new GameRoom(roomCode, host, guest);
            ActiveRooms[roomCode] = room;

            ConnectionToRoom[connectionId] = roomCode;
            ConnectionToRoom[host.ConnectionId] = roomCode;

            await Groups.AddToGroupAsync(host.ConnectionId, roomCode);
            await Groups.AddToGroupAsync(connectionId, roomCode);

            // Notify both players: Player 1 is the host, Player 2 is the guest
            await Clients.Client(host.ConnectionId).SendAsync("MatchFound", roomCode, 1, playerName, skinId);
            await Clients.Client(connectionId).SendAsync("MatchFound", roomCode, 2, host.PlayerName, host.SkinId);
            return;
        }

        // No opponent found, place player in queue
        WaitingQueue.Enqueue(new PlayerConnection(connectionId, playerName, skinId));
        await Clients.Caller.SendAsync("WaitingInLobby");
    }

    public async Task CreatePrivateRoom(string playerName, string skinId)
    {
        string roomCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        var host = new PlayerConnection(Context.ConnectionId, playerName, skinId);
        
        var room = new GameRoom(roomCode, host, null);
        ActiveRooms[roomCode] = room;

        ConnectionToRoom[Context.ConnectionId] = roomCode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        await Clients.Caller.SendAsync("RoomCreated", roomCode);
    }

    public async Task JoinPrivateRoom(string roomCode, string playerName, string skinId)
    {
        roomCode = roomCode.Trim().ToUpper();

        if (ActiveRooms.TryGetValue(roomCode, out var room))
        {
            if (room.Player2 != null)
            {
                await Clients.Caller.SendAsync("RoomError", "La sala ya está llena.");
                return;
            }

            var guest = new PlayerConnection(Context.ConnectionId, playerName, skinId);
            room.Player2 = guest;

            ConnectionToRoom[Context.ConnectionId] = roomCode;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            // Notify both players
            await Clients.Client(room.Player1.ConnectionId).SendAsync("MatchFound", roomCode, 1, playerName, skinId);
            await Clients.Client(Context.ConnectionId).SendAsync("MatchFound", roomCode, 2, room.Player1.PlayerName, room.Player1.SkinId);
        }
        else
        {
            await Clients.Caller.SendAsync("RoomError", "Sala no encontrada o código incorrecto.");
        }
    }

    public async Task SendState(string roomCode, string stateJson)
    {
        // Relay game state from Host (Player 1) to Guest (Player 2)
        await Clients.GroupExcept(roomCode, Context.ConnectionId).SendAsync("ReceiveState", stateJson);
    }

    public async Task SendInput(string roomCode, string directionJson)
    {
        // Relay input changes from Guest (Player 2) to Host (Player 1)
        await Clients.GroupExcept(roomCode, Context.ConnectionId).SendAsync("ReceiveInput", directionJson);
    }

    public async Task LeaveRoom()
    {
        await CleanUpConnection(Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await CleanUpConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task CleanUpConnection(string connectionId)
    {
        // Remove room association
        if (ConnectionToRoom.TryRemove(connectionId, out var roomCode))
        {
            if (ActiveRooms.TryRemove(roomCode, out var room))
            {
                // Inform group of the disconnect
                await Clients.Group(roomCode).SendAsync("OpponentDisconnected");
            }
        }
    }
}

public record PlayerConnection(string ConnectionId, string PlayerName, string SkinId);

public class GameRoom
{
    public string RoomCode { get; }
    public PlayerConnection Player1 { get; set; }
    public PlayerConnection? Player2 { get; set; }

    public GameRoom(string roomCode, PlayerConnection player1, PlayerConnection? player2)
    {
        RoomCode = roomCode;
        Player1 = player1;
        Player2 = player2;
    }
}
