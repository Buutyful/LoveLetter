using LoveLetter.GameCore;

namespace LoveLetter.Common;

public interface IGameService
{
    Task<ActionParameters> GetPlayerAction(string connectionId, Card[] availableCards);
    Task<Card[]> RequestCardSelection(string connectionId, int numberOfCards, Card[] availableCards);
}