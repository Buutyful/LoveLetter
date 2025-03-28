using LoveLetter.Common;
using LoveLetter.GameCore;

public class Game
{
    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 6;
    private readonly List<Player> _players;
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();
    public List<string> LastRoundWinnerIds { get; private set; } = [];
    public Guid Id { get; }
    public IGameService GameService { get; }
    public List<Log> EventsHistory { get; } = [];

    public Game(List<Player> players, IGameService gameService)
    {
        if (players.Count < MIN_PLAYERS || players.Count > MAX_PLAYERS)
            throw new ArgumentException($"Players must be between {MIN_PLAYERS} and {MAX_PLAYERS}");

        Id = Guid.NewGuid();
        GameService = gameService;
        _players = [.. players];
    }

    public async Task Run()
    {
        int pointsToWin = 3;

        while (_players.All(p => p.Score < pointsToWin))
        {
            var round = new Round(this, LastRoundWinnerIds);
            var winners = await round.Run();

            if (winners.Count != 0)
            {
                LastRoundWinnerIds = [.. winners.Select(p => p.Id)];
                winners.ForEach(p => p.Score++);
            }

            await Task.Delay(1000);
        }

        var gameWinners = DetermineGameWinners(pointsToWin);
        await GameService.NotifyGameEnded(Id, gameWinners);
    }

    private List<Player> DetermineGameWinners(int targetScore)
    {
        
        var candidates = _players
            .Where(p => p.Score >= targetScore)
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PlayedCards.Sum(x => (int)x.Type))
            .ToList();

        return candidates.GroupBy(p => p.Score)
            .OrderByDescending(g => g.Key)
            .First()
            .ToList();
    }
}
