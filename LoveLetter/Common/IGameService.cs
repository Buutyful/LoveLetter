using LoveLetter.GameCore;

namespace LoveLetter.Common;

public interface IGameService
{
    Task<Card[]> RequestCardSelection(string connectionId, int count, Card[] available);
    Task<ActionParameters> GetPlayerAction(string connId, Card[] available, string[] validTargetsId);
    Task NotifyTurnStart(Guid id1, string id2);
    Task NotifyCardPlayed(Guid id1, string id2, Card card, ActionParameters action);
    Task NotifyDraw(Guid id1, string id2, IEnumerable<Card> cards);
    Task NotifyDiscard(Guid id1, string id2, Card discarded);
    Task NotifyPlayerEliminated(Guid id1, string id2);
    Task NotifyPrivateInfo(Guid id1, string id2, string v);
    Task NotifyEffectApplied(Guid id1, string id2, Effect effect);
    Task NotifyEffectRemoved(Guid id1, string id2, Effect effect);
    Task NotifyHandUpdate(Guid id1, string id2, IReadOnlyCollection<Card> hand);
    Task NotifyLog(Guid id, string v);
    Task NotifyGameEnded(Guid id, List<Player> gameWinners);
}