using LoveLetter.Common;

namespace LoveLetter.GameCore;

public class LobbyEvents
{
    public sealed record PlayerJoined(Guid GameId, PlayerDto Source) : IEvent;
    public sealed record PlayerLeft(Guid GameId, PlayerDto Source) : IEvent;
    public sealed record RoundStarted(Guid GameId, int Number, List<PlayerDto> Participants) : IEvent;
    public sealed record RoundEnded(Guid GameId, PlayerDto Winner) : IEvent;
}

public class RoundEvents
{
    public sealed record PlayerDrew(Guid GameId, PlayerDto Source) : IEvent;
    public sealed record CardPlayed(Guid GameId, PlayerDto Source, CardType Card) : IEvent;
    public sealed record CardPlayedWithTarget(Guid GameId, PlayerDto Source, CardType Card, PlayerDto Target) : IEvent;
    public sealed record GuardPlayed(Guid GameId, PlayerDto Source, CardType Card, PlayerDto Target, CardType Guess) : IEvent;
}