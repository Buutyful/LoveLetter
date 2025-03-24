namespace LoveLetter.GameCore;

public class Player(string id, string name)
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
    public void Play(Card card)
    {
        if (_hand.Any(x => x == card))
            throw new InvalidOperationException();
        _hand.Remove(card);        
    }
    public void Draw(params IEnumerable<Card> cards) => _hand.AddRange(cards);
    public void Discard() => DiscardAt(0);
    public void RemoveTurnEffects() => RemoveEffectsByDuration(Duration.Turn);
    public void ResetState()
    {
        _hand.Clear();
        ActiveEffects.Clear();
        PlayedCards.Clear();
        OpponentInfo.Clear();
    }
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
    private Card DiscardAt(int index)
    {
        var card = _hand[index];
        _hand.RemoveAt(index);
        PlayedCards.Add(card);
        return card;
    }
    public void RemoveCards(params Card[] cards) => Array.ForEach(cards, Remove);
    private void Remove(Card card) => _hand.Remove(card);

}
