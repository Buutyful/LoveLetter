using LoveLetter.Common;

namespace LoveLetter.GameCore;

public class Game
{

    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 6;
    private readonly List<Player> _players = [];
    public List<Player> LastRoundWinner { get; private set; } = [];
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
            LastRoundWinner = await CurrentRound.Run();
            GivePointToWinner();
            CurrentRound = new Round(this);
        }
        //TODO: handle game end
    }
    private void GivePointToWinner()
    {
        var winnersiD = LastRoundWinner.Select(p => p.Id);
        foreach (var player in _players)
        {
            if (winnersiD.Contains(player.Id))
                player.Score++;
        }
    }
}

public class Round
{
    private static readonly Random _random = new();
    private readonly List<Player> _players = [];
    private readonly Queue<Player> _playerOrder = new();

    public Deck Deck { get; }
    public Game Game { get; }
    public Player CurrentPlayer => _playerOrder.Peek();
    public Round(Game game)
    {
        Deck = new Deck();
        Game = game;
        _players = [.. Game.Players];       
        SetUpRound();
        _playerOrder = SetPlayerOrder();
    }
    public async Task<List<Player>> Run()
    {
        while (_playerOrder.Count > 1)
        {
            if (!CurrentPlayer.IsAlive)
            {
                _playerOrder.Dequeue();
                continue;
            }
            if (!Deck.IsEmpty)
                CurrentPlayer.Draw(Deck.Draw());
            //TODO: handle validation and exceptions
            var actionParams = await Game.GameService
                .GetPlayerAction(
                CurrentPlayer.Id,
                [.. CurrentPlayer.Hand],
                [.. _playerOrder.Where(p => p.IsAlive).Select(p => p.Id)]);
            var playedCard = CurrentPlayer.Play(actionParams.CardPlayed);
            await playedCard.Use(new GameContext(Game, CurrentPlayer), actionParams);
            NextPlayer();
        }
        return [.. _playerOrder];
    }
    private Queue<Player> SetPlayerOrder()
    {
        var first = SetStartingPlayer();
        var queue = new Queue<Player>(_players);
        while (queue.Peek().Id != first.Id)
        {
            queue.Enqueue(queue.Dequeue());
        }
        return queue;
    }
    private Player SetStartingPlayer() => Game.LastRoundWinner switch
    {
        [] => _players[_random.Next(0, _players.Count)],
        [var pl] => pl,
        [.. var plrs] => plrs.MaxBy(p => p.Score) ?? throw new InvalidOperationException("No players found"),
        _ => throw new InvalidOperationException("Invalid last round winner")
    };
    private void SetUpRound()
    {
        foreach (var player in _players)
        {
            player.ResetState();
            player.Draw(Deck.Draw());
        }
    }
    private void NextPlayer() => _playerOrder.Enqueue(_playerOrder.Dequeue());   

}