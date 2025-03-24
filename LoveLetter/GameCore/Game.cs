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
    public Task Run()
    {
        while (!Players.Any(p => p.Score < 3))
        {

        }
        return Task.CompletedTask;
    }
}

public class Round
{
   public Deck Deck { get; }
   public Game Game { get; }
   public Round(Game game)
   {
       Deck = new Deck();
       Game = game;
   }
}