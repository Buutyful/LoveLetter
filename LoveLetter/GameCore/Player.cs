namespace LoveLetter.GameCore;

public class Player(string id, string name) : IEquatable<Player>
{
    private readonly List<Card> _hand = [];
    public string Id { get; } = id;
    public string Name { get; set; } = name;
    public bool IsAlive { get; private set; }
    public int Score { get; set; }
    public IReadOnlyCollection<Card> Hand => _hand.AsReadOnly();
    public List<Card> PlayedCards { get; } = [];
    public List<Effect> ActiveEffects { get; } = [];
    public Dictionary<string, List<Card>> OpponentInfo { get; } = [];
    public void Die()
    {
        IsAlive = false;
        Discard();
    }
    public Card Play(Card card)
    {
        var index = _hand.IndexOf(card);
        if (index < 0)
            throw new InvalidOperationException("Card not found");
        return DiscardAt(index);       
    }
    public void Draw(params IEnumerable<Card> cards) => _hand.AddRange(cards);
    public void RemoveTurnEffects() => RemoveEffectsByDuration(Duration.Turn);
    private void RemoveEffectsByDuration(Duration duration)
    {
        var index = 0;
        for (int i = 0; i < ActiveEffects.Count; i++)
        {
            if (ActiveEffects[i].Duration != duration)
            {
                ActiveEffects[index] = ActiveEffects[i];
                index++;
            }
        }
        ActiveEffects.RemoveRange(index, (ActiveEffects.Count - 1) - index);
    }
    public void RemoveCards(params Card[] cards) => Array.ForEach(cards, Remove);
    private void Remove(Card card) => _hand.Remove(card);
    public void ResetState()
    {
        _hand.Clear();
        ActiveEffects.Clear();
        PlayedCards.Clear();
        OpponentInfo.Clear();
    }

    public void DiscardMany(params IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            var index = _hand.IndexOf(card);
            if (index < 0)
                throw new InvalidOperationException("Card not found");
            DiscardAt(index);
        }
    }
    public void Discard() => DiscardAt(0);
    private Card DiscardAt(int index)
    {
        var card = _hand[index];
        _hand.RemoveAt(index);
        PlayedCards.Add(card);
        return card;
    }
    public bool MustPlayCountess() => 
        _hand.Any(x => x.Type == CardType.Countess) &&
        _hand.Any(c => c.Type == CardType.Prince || c.Type == CardType.King);
    public bool IsProtected() => ActiveEffects.Any(x => x.CardType == CardType.HandMaid);
    public bool Equals(Player? other) => other is not null && Id == other.Id;

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public void SetHand(List<Card> cards)
    {
        _hand.Clear();
        _hand.AddRange(cards);
    }
}
