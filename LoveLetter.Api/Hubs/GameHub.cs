using LoveLetter.Api.Data;
using LoveLetter.Api.Data.Models;
using LoveLetter.Common;
using LoveLetter.GameCore;
using Microsoft.AspNetCore.SignalR;
using static LoveLetter.Api.Data.Models.Lobby;

namespace LoveLetter.Api.Hubs;

public record User(string ConnectionId, string UserName);
public record ActionRequest<T>(Guid Id, TaskCompletionSource<T> PendingRequest);
public interface IGameHubClient
{
    Task<Card[]> OnCardSelectionRequested(Guid requestId, int cardNumber, Card[] availableCards);
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

        var game = new Game(lobby.Users.Select(u =>
            new Player(u.ConnectionId, u.UserName)).ToList(), _gameService);

        if (!InMemoryData.ActiveGames.TryAdd(game.Id, game))
            throw new InvalidOperationException("Game already exists");

        var groupTasks = game.Players.Select(player =>
            Groups.AddToGroupAsync(player.Id, game.Id.ToString()));

        await Task.WhenAll(groupTasks);

        _ = game.Run();
    }

    public Task SubmitCardSelection(Guid requestId, Card[] selectedCards)
    {
        var connectionId = Context.ConnectionId;

        if (!InMemoryData.PendingCardsSelection.TryGetValue(connectionId, out var pendingAction) ||
            pendingAction.Id != requestId)
        {
            throw new InvalidOperationException("No pending action found");
        }

        pendingAction.PendingRequest.SetResult(selectedCards);
        InMemoryData.PendingCardsSelection.TryRemove(connectionId, out _);

        return Task.CompletedTask;
    }
}

