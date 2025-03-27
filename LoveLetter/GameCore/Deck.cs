using System.Collections.Concurrent;

namespace LoveLetter.GameCore;

public class Deck
{

    private readonly ConcurrentQueue<Card> _deckCards;
    public Card DiscardedCard { get; private set; }
    public DeckType Type { get; }
    public bool IsEmpty => _deckCards.IsEmpty;

    public Deck(DeckType deckType = DeckType.Classic)
    {
        (_deckCards, DiscardedCard) = NewDeck(deckType);
        Type = deckType;
    }


    private static (ConcurrentQueue<Card>, Card) NewDeck(DeckType deckType)
    {
        var random = new Random();
        var specs = DeckConfiguration.GetDeckSpecs(deckType);
        var newDeck = specs.Keys.SelectMany(CreateCardAmount);

        var shuffledDeck = newDeck.OrderBy(_ => random.Next());

        var deck = new ConcurrentQueue<Card>(shuffledDeck);

        deck.TryDequeue(out var discarded);

        return (deck, discarded!);

        IEnumerable<Card> CreateCardAmount(CardType type) =>
            Enumerable.Range(0, specs[type])
                .Select(n => new Card(type));
    }

    public Card Draw() => DrawMany(1).First();
    public IEnumerable<Card> DrawMany(int amount)
    {
        int drawnCount = 0;

        while (drawnCount < amount && _deckCards.TryDequeue(out var card))
        {
            yield return card;
            drawnCount++;
        }
    }
    public void PutCardsOnBottom(params List<Card> cards)
    {
        foreach (var card in cards) _deckCards.Enqueue(card);
    }
    public void Shuffle()
    {
        var random = new Random();
        var currentCards = _deckCards.ToList();
        var shuffledList = currentCards.OrderBy(_ => random.Next()).ToList();
        while (_deckCards.TryDequeue(out _)) { }
        foreach (var card in shuffledList)
        {
            _deckCards.Enqueue(card);
        }
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
        [CardType.Princess] = 1,
        [CardType.Countess] = 1,
        [CardType.King] = 1,
        [CardType.Prince] = 2,
        [CardType.HandMaid] = 2,
        [CardType.Baron] = 2,
        [CardType.Priest] = 2,
        [CardType.Guard] = 5
    };
}