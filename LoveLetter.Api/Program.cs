using LoveLetter.Api.Data;
using LoveLetter.Api.Hubs;
using LoveLetter.Api.Services;
using LoveLetter.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
{
    builder.Services.AddSignalR();
    builder.Services.AddScoped<IGameService, GameService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/lobbies", () => InMemoryData.Lobbies.Values.Select(l => l.ToLobbyDto).ToList());
app.MapHub<LobbyHub>("/lobbyhub");
app.MapHub<GameHub>("/gamehub");
app.Run();

