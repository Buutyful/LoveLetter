using LoveLetter.Api.Data.Models;
using LoveLetter.Api.Hubs;
using LoveLetter.GameCore;
using System.Collections.Concurrent;

namespace LoveLetter.Api.Data;

public static class InMemoryData
{
    public static readonly ConcurrentDictionary<Guid, Lobby> Lobbies = [];
    public static readonly ConcurrentDictionary<string, User> ConnectedPlayers = [];
    public static readonly ConcurrentDictionary<Guid, Game> ActiveGames = new();
    public static readonly ConcurrentDictionary<string, ActionRequest<Card[]>> PendingCardsSelection = new();


}

