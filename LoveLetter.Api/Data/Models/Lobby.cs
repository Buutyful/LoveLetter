using LoveLetter.Api.Hubs;
using LoveLetter.Common;

namespace LoveLetter.Api.Data.Models;

public class Lobby(Guid id, string name, User owner)
{
    private readonly Lock _lock = new();
    private readonly List<User> _users = [owner];
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    public Guid Id { get; } = id;
    public bool IsOpen { get; private set; } = true;
    public string Name { get; } = name;
    public User Owner { get; private set; } = owner;

    public Result<bool> TryJoin(User user)
    {
        lock (_lock)
        {
            if (_users.Count < 6)
            {
                _users.Add(user);
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure(new Error("lobby is full"));
        }
    }
    public void RemoveUser(User user)
    {
        lock (_lock)
        {
            _users.Remove(user);
            if (user == Owner || _users.Count == 0) ChangeOwner();
        }
    }
    private void ChangeOwner()
    {
        if (_users.Count > 0)
            Owner = _users.First();
        else InMemoryData.Lobbies.Remove(Id, out var _);
    }
    public LobbyDto ToLobbyDto => new(Id, Name, Owner, _users.Count, IsOpen);
    public record LobbyDto(Guid Id, string Name, User Owner, int Partecipants, bool State);
}

