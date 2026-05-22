using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy to allow any origin (*)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

// In-memory store for game scores (thread-safe)
var scores = new ConcurrentBag<ScoreRecord>();

// GET /api/scores: Returns the Top 10 scores sorted from highest to lowest
app.MapGet("/api/scores", () =>
{
    var topScores = scores
        .OrderByDescending(s => s.Score)
        .Take(10)
        .ToList();
    return Results.Ok(topScores);
});

// POST /api/scores: Receives { name, score } and stores it
app.MapPost("/api/scores", (ScoreRecord request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }

    if (request.Score < 0)
    {
        return Results.BadRequest(new { error = "Score must be a non-negative integer." });
    }

    scores.Add(request);
    return Results.Created($"/api/scores", request);
});

app.Run();

// Data structure representing a score record
public record ScoreRecord(string Name, int Score);
