using LoveLetter.Api.Data;
using LoveLetter.Api.Data.Models;
using Microsoft.AspNetCore.SignalR;
using static LoveLetter.Api.Data.Models.Lobby;

namespace LoveLetter.Api.Hubs;
public interface ILobbyHubClient
{
    Task OnUserConnected(User user);
    Task OnUserDisconnected(User user);
    Task OnUserJoined(User user, Guid lobbyId);
    Task OnUserLeft(User user, Guid lobbyId);
    Task OnNewLobby(LobbyDto lobby);
    Task OnUserNameSet(User user);
}
public class LobbyHub : Hub<ILobbyHubClient>
{
    private User GetCurrentUser()
    {
        var connectionId = Context.ConnectionId;
        if (!InMemoryData.ConnectedPlayers.TryGetValue(connectionId, out var user))
            throw new InvalidOperationException("User not found");
        return user;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        var user = new User(Context.ConnectionId, $"user{InMemoryData.ConnectedPlayers.Count}");
        InMemoryData.ConnectedPlayers.TryAdd(Context.ConnectionId, user);
        await Clients.All.OnUserConnected(user);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        var connectionId = Context.ConnectionId;

        if (InMemoryData.ConnectedPlayers.Remove(connectionId, out var user))
        {
            var affectedLobbies = InMemoryData.Lobbies.Values
                .Where(l => l.Users.Contains(user))
                .ToList();

            foreach (var lobby in affectedLobbies)
            {
                lobby.RemoveUser(user);
                await Clients.Group(lobby.Id.ToString()).OnUserLeft(user, lobby.Id);
            }

            await Clients.All.OnUserDisconnected(user);
        }
    }

    public async Task JoinLobby(Guid lobbyId)
    {
        var user = GetCurrentUser();

        if (!InMemoryData.Lobbies.TryGetValue(lobbyId, out var lobby))
            throw new InvalidOperationException("Lobby not found");

        if (!lobby.TryJoin(user).IsSuccess)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId.ToString());
        await Clients.Group(lobbyId.ToString()).OnUserJoined(user, lobbyId);
    }

    public async Task LeaveLobby(Guid lobbyId)
    {
        var user = GetCurrentUser();

        if (!InMemoryData.Lobbies.TryGetValue(lobbyId, out var lobby))
            throw new InvalidOperationException("Lobby not found");

        lobby.RemoveUser(user);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId.ToString());
        await Clients.Group(lobbyId.ToString()).OnUserLeft(user, lobbyId);
    }

    public async Task CreateLobby(string lobbyName)
    {
        var user = GetCurrentUser();
        var lobby = new Lobby(Guid.NewGuid(), lobbyName, user);

        InMemoryData.Lobbies.TryAdd(lobby.Id, lobby);
        await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id.ToString());
        await Clients.All.OnNewLobby(lobby.ToLobbyDto);
    }

    public async Task SetUserName(string userName)
    {
        var user = GetCurrentUser() with { UserName = userName };
        InMemoryData.ConnectedPlayers[Context.ConnectionId] = user;
        await Clients.All.OnUserNameSet(user);
    }
}

