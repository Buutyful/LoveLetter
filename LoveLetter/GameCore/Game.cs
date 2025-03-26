using LoveLetter.Common;

namespace LoveLetter.GameCore;

public class Game
{

    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 6;
    private readonly List<Player> _players = [];
    public Guid Id { get; }
    public IGameService GameService { get; }
    public List<IEvent> EventsHistory { get; } = [];
    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    public Round CurrentRound { get; private set; }
    public Deck Deck => CurrentRound.Deck;

    public Game(List<Player> players, IGameService gameService)
    {
        Id = Guid.NewGuid();
        GameService = gameService;
        CurrentRound = new Round(this);
        _players = [.. players];
    }
    public async Task Run()
    {
        while(Players.All(p => p.Score < 3))
        {
            await CurrentRound.Run();
            CurrentRound = new Round(this);
        }
        //TODO: handle game end
    }
}

public class Round
{
    private static readonly Random _random = new();
    private int _currentPlayer;
    private Player CurrentPlayer => Game.Players[_currentPlayer];
    public Deck Deck { get; }
    public Game Game { get; }
    public Round(Game game)
    {
        Deck = new Deck();
        Game = game;
        SetStartingPlayer();
        SetUpRound();
    }
    public async Task Run()
    {
        while (Game.Players.Count(p => p.IsAlive) > 1)
        {
            if (!Deck.IsEmpty)
                CurrentPlayer.Draw(Deck.Draw());
            //TODO: handle player not responding
            var actionParams = await Game.GameService.GetPlayerAction(CurrentPlayer.Id, [.. CurrentPlayer.Hand]);
            var playedCard = CurrentPlayer.Play(actionParams.CardPlayed);
            await playedCard.Use(new GameContext(Game, CurrentPlayer), actionParams);
            NextPlayer();
        }
        //TODO: handle round end
    }
    private void SetStartingPlayer()
    {
        var maxScore = Game.Players.Max(p => p.Score);
        _currentPlayer = maxScore > 0 ?
            Game.Players.ToList().FindIndex(p => p.Score == maxScore) :
            _random.Next(0, Game.Players.Count);
    }
    private void SetUpRound()
    {
        foreach (var player in Game.Players)
        {
            player.ResetState();
            player.Draw(Deck.Draw());
        }
    }
    private void NextPlayer()
    {
        var max = Game.Players.Count;
        var count = 0;

        _currentPlayer = (_currentPlayer + 1) % Game.Players.Count;

        while (!CurrentPlayer.IsAlive && count < max)
        {
            count++;
            _currentPlayer = (_currentPlayer + 1) % Game.Players.Count;
        }
        if (count >= max)
            throw new InvalidOperationException("No living players");
    }

}