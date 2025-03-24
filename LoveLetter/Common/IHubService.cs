using LoveLetter.GameCore;

namespace LoveLetter.Common;

public interface IGameService
{
    Task<Card[]> RequestCardSelection(string connectionId, int numberOfCards, Card[] availableCards);
}