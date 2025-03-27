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
    Countess,
    Princess
}
public record Effect(CardType CardType, Duration Duration, TextSegment Description);
public abstract record TextSegment;
public sealed record PlainText(string Text) : TextSegment;
public sealed record Link(Uri Uri) : TextSegment;



public record Card(CardType Type) : IComparable<Card>
{
    public int CompareTo(Card? other) => other?.Type.CompareTo(Type) ?? 1;
}
public record GameContext(Game Game, Player Source);
public record ActionParameters(CardType CardPlayed, string? TargetId = null, CardType? Guess = null);

public static class CardEffects
{
    private static readonly Dictionary<
        CardType,
        Func<GameContext, ActionParameters, IEnumerable<IGameAction>>> Handlers = new()
        {
            [CardType.Spy] = HandleSpy,
            [CardType.Guard] = HandleGuard,
            [CardType.Priest] = HandlePriest,
            [CardType.Baron] = HandleBaron,
            [CardType.HandMaid] = HandleHandMaid,
            [CardType.Prince] = HandlePrince,
            [CardType.Chancellor] = HandleChancellor,
            [CardType.King] = HandleKing,
            [CardType.Countess] = HandleCountess,
            [CardType.Princess] = HandlePrincess,
        };
    public static IEnumerable<IGameAction> GetGameActions(this Card card, GameContext context, ActionParameters parameters)
    {
        if (card.Type != parameters.CardPlayed)
            throw new ArgumentException("Card type mismatch");
        if (!Handlers.TryGetValue(card.Type, out var handler))
            throw new NotImplementedException($"No handler for {card.Type}");

        return handler(context, parameters);
    }
    private static IEnumerable<IGameAction> HandleSpy(GameContext context, ActionParameters _)
    {
        var effect = new Effect(CardType.Spy, Duration.Round, new PlainText(""));
        yield return new AddEffect(context.Source, effect);
    }
    private static IEnumerable<IGameAction> HandleGuard(GameContext context, ActionParameters parameters)
    {
        if (parameters.Guess is null)
            throw new ArgumentNullException(nameof(parameters.Guess));
        if (parameters.Guess == CardType.Guard)
            throw new InvalidOperationException("Cannot guess Guard.");

        var target = IsTargetInValidState(context, parameters);

        if(target.IsSelfTarget(context.Source.Id))
            throw new InvalidOperationException("Cannot target self");

        var result = target.Hand.First().Type == parameters.Guess;

        if (result)
        {
            yield return new Eliminate(target);
            yield return new Log(new PlainText($"{context.Source.Name} played Guard targeting {target.Name}, guessing {parameters.Guess}: correct"));
        }
        else
        {
            yield return new NoEffect();
            yield return new Log(new PlainText($"{context.Source.Name} played Guard targeting {target.Name}, guessing {parameters.Guess}: incorrect"));
        }
    }
    private static IEnumerable<IGameAction> HandlePriest(GameContext context, ActionParameters parameters)
    {
        var target = IsTargetInValidState(context, parameters);
        if (target.IsSelfTarget(context.Source.Id))
            throw new InvalidOperationException("Cannot target self");
        var card = target.Hand.First();
        yield return new AddInfo(context.Source, target, card.Type);
    }
    private static IEnumerable<IGameAction> HandleBaron(GameContext context, ActionParameters parameters)
    {
        var target = IsTargetInValidState(context, parameters);
        if (target.IsSelfTarget(context.Source.Id))
            throw new InvalidOperationException("Cannot target self");
        var result = context.Source.Hand.First().CompareTo(target.Hand.First());
        if (result > 0) yield return new Eliminate(target);
        else if (result < 0) yield return new Eliminate(context.Source);
        else yield return new NoEffect();
    }
    private static IEnumerable<IGameAction> HandleHandMaid(GameContext context, ActionParameters _)
    {
        var effect = new Effect(CardType.HandMaid, Duration.Turn, new PlainText("protected till next turn"));
        yield return new AddEffect(context.Source, effect);
    }
    private static IEnumerable<IGameAction> HandlePrince(GameContext context, ActionParameters parameters)
    {
        var target = IsTargetInValidState(context, parameters);
        yield return new Discard(target, target.Hand.First());
        yield return new Draw(target, 1);
    }
    private static IEnumerable<IGameAction> HandleChancellor(GameContext context, ActionParameters _)
    {       
        yield return new Draw(context.Source, 2);
        yield return new PlaceCardsOnBottom(context.Source, 2);
    }
    private static IEnumerable<IGameAction> HandleKing(GameContext context, ActionParameters parameters)
    {
        var target = IsTargetInValidState(context, parameters);
        if (target.IsSelfTarget(context.Source.Id))
            throw new InvalidOperationException("Cannot target self");
        yield return new Swap(context.Source, target);
        yield return new Log(new PlainText($"{context.Source.Name} swapped hands with {target.Name}"));
    }
    private static IEnumerable<IGameAction> HandleCountess(GameContext context, ActionParameters _)
    {
        yield return new NoEffect();
    }
    private static IEnumerable<IGameAction> HandlePrincess(GameContext context, ActionParameters _)
    {
        yield return new Eliminate(context.Source);
    }
    private static Player IsTargetInValidState(GameContext context, ActionParameters parameters)
    {
        if (parameters.TargetId is null)
            throw new ArgumentNullException(nameof(parameters.TargetId));

        var target = GetTargetPlayer(context, parameters.TargetId);

        if (target.ActiveEffects.Any(x => x.CardType == CardType.HandMaid))
            throw new InvalidOperationException("Cannot target protected Player");

        if (target.Hand.Count == 0)
            throw new InvalidOperationException("Target is in invalidState");
        return target;
    }
    private static bool IsSelfTarget(this Player player, string source) => player.Id == source;
    private static Player GetTargetPlayer(GameContext context, string targetId) =>
      context.Game.Players.FirstOrDefault(p => p.Id == targetId) ??
      throw new InvalidOperationException("Target not found within game players");
}









