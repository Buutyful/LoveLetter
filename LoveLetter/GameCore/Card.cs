namespace LoveLetter.GameCore;

public enum Duration
{
    Turn,
    Round,
    Game,
}

public enum CardType
{
    Spy = 0,
    Guard,
    Priest,
    Baron,
    HandMaid,
    Prince,
    Chancellor,
    King,
    Contess,
    Princess
}
public record Card(CardType Type) : IComparable<Card>
{
    public int CompareTo(Card? other) => other?.Type.CompareTo(Type) ?? 1;
}
public record GameContext(Game Game, Player Source);
public record ActionParameters(CardType CardPlayed, string? TargetId = null, CardType? Guess = null);

public static class CardEffects
{
    private static readonly Dictionary<CardType, Func<GameContext, ActionParameters, ValueTask>> Handlers = new()
    {
        [CardType.Spy] = HandleSpia,
        [CardType.Guard] = HandleGuardia,
        [CardType.Priest] = HandlePrete,
        [CardType.Baron] = HandleBarone,
        [CardType.HandMaid] = HandleServa,
        [CardType.Prince] = HandlePrincipe,
        [CardType.Chancellor] = HandleCancelliere
    };

    public static ValueTask Use(this Card card, GameContext context, ActionParameters parameters)
    {
        if (card.Type != parameters.CardPlayed)
            throw new ArgumentException("Card type mismatch");

        if (!Handlers.TryGetValue(card.Type, out var handler))
            throw new NotImplementedException($"No handler for {card.Type}");

        return handler(context, parameters);
    }
    private static ValueTask HandleSpia(GameContext context, ActionParameters _)
    {
        context.Source.ActiveEffects.Add(new Effect(CardType.Spy, Duration.Round, new PlainText("")));
        return ValueTask.CompletedTask;
    }
    private static ValueTask HandleGuardia(GameContext context, ActionParameters parameters)
    {
        if(parameters.TargetId is null) 
            throw new ArgumentNullException(nameof(parameters.TargetId));

        var target = GetTargetPlayer(context, parameters.TargetId);

        var result = target.Hand.First().Type == parameters.Guess;

        if (result)
        {
            target.Die();
        }
        return ValueTask.CompletedTask;
    }
    private static ValueTask HandlePrete(GameContext context, ActionParameters parameters)
    {
        var source = context.Source;
        if (parameters.TargetId is null || parameters.TargetId == context.Source.Id)
            throw new InvalidOperationException();
        var target = GetTargetPlayer(context, parameters.TargetId);
        source.OpponentInfo[target.Id] = [.. target.Hand];
        return ValueTask.CompletedTask;
    }
    private static ValueTask HandleBarone(GameContext context, ActionParameters parameters)
    {
        if(parameters.TargetId is null) 
            throw new ArgumentNullException(nameof(parameters.TargetId));

        var target = GetTargetPlayer(context, parameters.TargetId);
        var result = context.Source.Hand.First().CompareTo(target.Hand.First());

        if (result > 0)
            target.Die();
        else if (result < 0)
            context.Source.Die();

        return ValueTask.CompletedTask;
    }
    private static ValueTask HandleServa(GameContext context, ActionParameters _)
    {
        context.Source.ActiveEffects.Add(new Effect(CardType.HandMaid, Duration.Turn, new PlainText("")));
        return ValueTask.CompletedTask;
    }
    private static ValueTask HandlePrincipe(GameContext context, ActionParameters parameters)
    {
        if(parameters.TargetId is null) 
            throw new ArgumentNullException(nameof(parameters.TargetId));

        var target = GetTargetPlayer(context, parameters.TargetId);

        target.Discard();
        target.Draw(context.Game.Deck.Draw());

        return ValueTask.CompletedTask;
    }
    private static async ValueTask HandleCancelliere(GameContext context, ActionParameters _)
    {
        context.Source.Draw(context.Game.Deck.DrawMany(2));
        var playerHand = context.Source.Hand;
        var res = await context.Game.GameService.RequestCardSelection(context.Source.Id, 2, [.. playerHand]);
        //TODO: this is so ugly, improve the flow
        if (res.Length == 0)
        {
            context.Source.Die();
            res = [.. playerHand];
        }
        else context.Source.RemoveCards(res);

        context.Game.Deck.PutCardsOnBottom([.. res]);
    }
    private static Player GetTargetPlayer(GameContext context, string targetId) =>
    context.Game.Players.FirstOrDefault(p => p.Id == targetId) ??
    throw new InvalidOperationException("Target not found within game players");
}







public record Effect(CardType CardType, Duration Duration, TextSegment Description);
public abstract record TextSegment;
public sealed record PlainText(string Text) : TextSegment;
public sealed record Link(Uri Uri) : TextSegment;
