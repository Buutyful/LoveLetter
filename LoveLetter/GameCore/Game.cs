using LoveLetter.Common;

namespace LoveLetter.GameCore;

public class Game
{

    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 6;
    public Guid Id { get; }
    public IGameService GameService { get; }
    public List<IEvent> EventsHistory { get; } = [];
    public List<Player> Players { get; } = [];
    public Round CurrentRound { get; private set; }
    public Deck Deck => CurrentRound.Deck;

    public Game(List<Player> players, IGameService gameService)
    {
        Id = Guid.NewGuid();
        GameService = gameService;
        CurrentRound = new Round(this);
        Players = players;
    }
    public async Task Run()
    {
        while (!Players.Any(p => p.Score < 3))
        {
            await CurrentRound.Run();
            CurrentRound = new Round(this);
        }
        //TODO: handle game end
    }
}

public class Round
{
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
        while (Game.Players.Any(p => p.IsAlive) && !Deck.IsEmpty)
        {
            CurrentPlayer.Draw(Deck.Draw());
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
            Game.Players.FindIndex(p => p.Score == maxScore) :
            new Random().Next(0, Game.Players.Count);
    }
    private void SetUpRound()
    {
        Game.Players.ForEach(p => p.ResetState());
        Game.Players.ForEach(p => p.Draw(Deck.Draw()));
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
        if (count > max)
            throw new InvalidOperationException("No players left");
    }

}