namespace LoveLetter.GameCore;

public interface IGameAction { }


#region Player
public sealed record Draw(Player Source, int Amount) : IGameAction;
public sealed record Discard(Player Source, Card Card) : IGameAction;
public sealed record DiscardSelection(Player Source, int Amount) : IGameAction;
public sealed record Eliminate(params Player[] Players) : IGameAction;
public sealed record AddInfo(Player Source, Player Target, params CardType[] Cards) : IGameAction;
public sealed record AddEffect(Player Source, params Effect[] Effects) : IGameAction;
public sealed record RemoveEffect(Player Source, params Effect[] Effects) : IGameAction;
public sealed record Swap(Player Source, Player Target) : IGameAction;
public sealed record NoEffect() : IGameAction;
public sealed record PlaceCardsOnBottom(Player Source, int Amount) : IGameAction;
#endregion

#region Game
public sealed record Log(params TextSegment[] Text) : IGameAction
{
    DateTime Timestamp { get; } = DateTime.UtcNow;
};
#endregion


#region Deck
public sealed record Shuffle() : IGameAction;
#endregion