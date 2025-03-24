namespace LoveLetter.GameCore;

public class Deck
{

    private readonly Queue<Card> _deckCards;
    public Card DiscardedCard { get; private set; }
    public DeckType Type { get; }

    public Deck(DeckType deckType = DeckType.Classic)
    {
        (_deckCards, DiscardedCard) = NewDeck(deckType);
        Type = deckType;
    }


    private static (Queue<Card>, Card) NewDeck(DeckType deckType)
    {
        var random = new Random();
        var specs = DeckConfiguration.GetDeckSpecs(deckType);
        var newDeck = specs.Keys.SelectMany(CreateCardAmount);

        var shuffledDeck = newDeck.OrderBy(_ => random.Next());

        var deck = new Queue<Card>(shuffledDeck);

        var discarded = deck.Dequeue();

        return (deck, discarded);

        IEnumerable<Card> CreateCardAmount(CardType type) =>
            Enumerable.Range(0, specs[type])
                .Select(n => new Card(type));
    }

    public IEnumerable<Card> Draw() => DrawMany(1);
    public IEnumerable<Card> DrawMany(int amount)
    {
        while (_deckCards.Count > 0 && amount > 0)
        {
            yield return _deckCards.Dequeue();
            amount--;
        }
    }
    public void PutCardsOnBottom(params List<Card> cards)
    {
        foreach (var card in cards) _deckCards.Enqueue(card);
    }
}

public enum DeckType
{
    Classic,
}

public static class DeckConfiguration
{
    public static Dictionary<CardType, int> GetDeckSpecs(DeckType deckType) =>
        deckType switch
        {
            DeckType.Classic => ClassicDeck,
            _ => throw new ArgumentOutOfRangeException(nameof(deckType), deckType, "deck not implemented")
        };

    private static Dictionary<CardType, int> ClassicDeck => new()
    {
        [CardType.Principessa] = 1,
        [CardType.Contessa] = 1,
        [CardType.Re] = 1,        
        [CardType.Principe] = 2,
        [CardType.Serva] = 2,
        [CardType.Barone] = 2,
        [CardType.Prete] = 2,
        [CardType.Guardia] = 5
    };
}