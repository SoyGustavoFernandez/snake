# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SnakeQuest.API.csproj", "./"]
RUN dotnet restore "SnakeQuest.API.csproj"

# Copy all source files and build
COPY . .
RUN dotnet build "SnakeQuest.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SnakeQuest.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Production Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for production (standard port for containerized environment is 8080)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "SnakeQuest.API.dll"]
