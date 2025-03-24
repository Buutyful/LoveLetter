namespace LoveLetter.Common;

public sealed record PlayerDto(string PlayerId, string PlayerName, bool IsAlive, int Score);
public sealed record GameDto(Guid Id, List<PlayerDto> Players, RoundDto Round);
public sealed record RoundDto(PlayerDto CurrentPlayer, int CardsLeft);