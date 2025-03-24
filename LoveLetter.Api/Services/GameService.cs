using LoveLetter.Api.Data;
using LoveLetter.Api.Hubs;
using LoveLetter.Common;
using LoveLetter.GameCore;
using Microsoft.AspNetCore.SignalR;

namespace LoveLetter.Api.Services;

public class GameService(IHubContext<GameHub, IGameHubClient> hubContext) : IGameService
{
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext = hubContext;

    // ask the player to select a card
    public async Task<Card[]> RequestCardSelection(string connectionId, int numberOfCards, Card[] availableCards)
    {
        var requestId = Guid.NewGuid();
        var completionSource = new TaskCompletionSource<Card[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingCards = InMemoryData.PendingCardsSelection;
        if (pendingCards.ContainsKey(connectionId))
        {
            throw new InvalidOperationException("a request is already pending");
        }
        pendingCards[connectionId] = new(requestId, completionSource);

        await _hubContext.Clients.Client(connectionId).OnCardSelectionRequested(requestId, numberOfCards, availableCards);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var completedTask = await Task.WhenAny(completionSource.Task, Task.Delay(Timeout.Infinite, cts.Token));

        pendingCards.Remove(connectionId, out var _);

        if (completedTask == completionSource.Task)
        {
            cts.Cancel();
            return await completionSource.Task;
        }
        return [];
    }
}

