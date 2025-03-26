using LoveLetter.Api.Data;
using LoveLetter.Api.Data.Models;
using LoveLetter.Common;
using LoveLetter.GameCore;
using Microsoft.AspNetCore.SignalR;
using static LoveLetter.Api.Data.Models.Lobby;


namespace LoveLetter.Api.Hubs;

public record User(string ConnectionId, string UserName);

public interface IGameHubClient
{
    Task OnCardSelectionRequested(Guid requestId, int cardNumber, Card[] availableCards);
    Task OnCardPlayRequested(Guid requestId, Card[] availableCards);
}
public class GameHub(IGameService gameService) : Hub<IGameHubClient>
{
    private readonly IGameService _gameService = gameService;
   

    private User GetCurrentUser()
    {
        var connectionId = Context.ConnectionId;
        if (!InMemoryData.ConnectedPlayers.TryGetValue(connectionId, out var user))
            throw new InvalidOperationException("User not found");
        return user;
    }

    private static Lobby GetLobby(Guid lobbyId)
    {
        if (!InMemoryData.Lobbies.TryGetValue(lobbyId, out var lobby))
            throw new InvalidOperationException("Lobby not found");
        return lobby;
    }

    public async Task StartGame(Guid lobbyId)
    {
        var user = GetCurrentUser();
        var lobby = GetLobby(lobbyId);

        if (lobby.Owner != user)
            throw new InvalidOperationException("Only the owner can start the game");

        var game = new Game([.. lobby.Users.Select(u =>
            new Player(u.ConnectionId, u.UserName))], _gameService);

        if (!InMemoryData.ActiveGames.TryAdd(game.Id, game))
            throw new InvalidOperationException("Game already exists");

        var groupTasks = game.Players.Select(player =>
            Groups.AddToGroupAsync(player.Id, game.Id.ToString()));

        await Task.WhenAll(groupTasks);

        _ = game.Run();
    }

    public Task SubmitCardSelection(Guid requestId, Card[] cards)
    {
        var connId = Context.ConnectionId;
        if (!InMemoryData.PendingRequests.TryGetValue(connId, out var pending))
            throw new InvalidOperationException("not found pending request");

        if (pending.Id != requestId || pending is not CardSelectionRequest req)
            throw new InvalidOperationException("bad request");

        req.PendingTask.TrySetResult(cards);

        return Task.CompletedTask;
    }
    public Task SubmitGameAction(Guid requestId, ActionParameters parameters)
    {

        var connId = Context.ConnectionId;
        if (!InMemoryData.PendingRequests.TryGetValue(connId, out var pending))
            throw new InvalidOperationException("not found pending request");
        if (pending.Id != requestId || pending is not CardPlayRequest req)
            throw new InvalidOperationException("bad request");

        req.PendingTask.TrySetResult(parameters);
        return Task.CompletedTask;
    }
 
}

