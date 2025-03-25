using LoveLetter.Api.Data;
using LoveLetter.Api.Data.Models;
using LoveLetter.Api.Hubs;
using LoveLetter.Common;
using LoveLetter.GameCore;
using Microsoft.AspNetCore.SignalR;

namespace LoveLetter.Api.Services;

public class GameService(IHubContext<GameHub, IGameHubClient> hub) : IGameService
{
    public async Task<Card[]> RequestCardSelection(string connId, int count, Card[] available)
    {
        var requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<Card[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));        

        InMemoryData.PendingRequests[connId] = new CardSelectionRequest(requestId, count, tcs, cts);

        await hub.Clients.Client(connId).OnCardSelectionRequested(requestId, count, available);

        return await WaitWithCleanup(tcs, connId, cts.Token);
    }

    //public async Task<Player> RequestPlayerTarget(string connId, Player[] available)
    //{
    //    var requestId = Guid.NewGuid();
    //    var tcs = new TaskCompletionSource<Player>();
    //    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

    //    InMemoryData.PendingRequests[connId] = (requestId, tcs, cts, typeof(Player));

    //    await hub.Clients.Client(connId).RequestPlayerTarget(requestId, available);

    //    return await WaitWithCleanup(tcs, connId, cts.Token);
    //}

    private static async Task<T> WaitWithCleanup<T>(TaskCompletionSource<T> tcs, string connId, CancellationToken ct)
    {
        try
        {
            return await tcs.Task.WaitAsync(ct);
        }
        finally
        {
            InMemoryData.PendingRequests.TryRemove(connId, out _);
        }
    }

  
}
