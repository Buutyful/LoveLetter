using LoveLetter.GameCore;

public class Round
{
    private readonly Random _random = new();
    private readonly Game _game;
    private readonly Queue<Player> _playerOrder;
    private readonly Deck _deck;

    public Round(Game game, List<string> lastWinnerIds)
    {
        _game = game;
        _deck = new Deck();
        _playerOrder = DeterminePlayerOrder(lastWinnerIds);
        InitializePlayers();
    }
    private Queue<Player> DeterminePlayerOrder(List<string> lastWinnerIds)
    {
        var starters = _game.Players
                          .Where(p => lastWinnerIds.Contains(p.Id))
                          .ToList();
        var randomOrder = _game.Players.OrderBy(_ => _random.Next()).ToList();
        Player starter = starters.Count switch
        {
            0 => randomOrder.First(),
            1 => starters[0],
            _ => starters.OrderByDescending(p => p.PlayedCards.Sum(c => (int)c.Type))
                        .ThenBy(p => _random.Next())
                        .First()
        };

        var que = new Queue<Player>(randomOrder);
        while (que.Peek() != starter)
        {
            que.Enqueue(que.Dequeue());
        }
        return que;
    }
    private void InitializePlayers()
    {
        Task.Run(async () =>
        {
            foreach (var player in _playerOrder) 
            {
                player.ResetState();
                await ProcessActionResults([new Draw(player, 1)]);
            }
        }).GetAwaiter().GetResult();
    }
    public async Task<List<Player>> Run()
    {
        while (_playerOrder.Count > 1 && !_deck.IsEmpty)
        {
            var currentPlayer = _playerOrder.Dequeue();
            if (!currentPlayer.IsAlive) continue;

            await ProcessPlayerTurn(currentPlayer);

            if (currentPlayer.IsAlive)
                _playerOrder.Enqueue(currentPlayer);
        }

        return DetermineRoundWinners();
    }

    private async Task ProcessPlayerTurn(Player player)
    {
        try
        {
            player.RemoveTurnEffects();
            await _game.GameService.NotifyTurnStart(_game.Id, player.Id);

            await ProcessActionResults([new Draw(player, 1)]);

            if (player.MustPlayCountess())
                await HandleForcedCountessPlay(player);
            else
                await HandleNormalTurn(player);
        }
        catch (Exception ex)
        {
            
        }
    }
    private async Task HandleForcedCountessPlay(Player player)
    {
        var countess = player.Hand.First(c => c.Type == CardType.Countess)
                      ?? throw new InvalidOperationException("Countess not found");
        await PlayCard(player, countess, new ActionParameters(CardType.Countess));
    }
    private async Task HandleNormalTurn(Player player)
    {
        var action = await _game
            .GameService
            .GetPlayerAction(
                player.Id,
                [.. player.Hand],
                ValidTargetrs());

        var card = player.Hand.First(c => c.Type == action.CardPlayed)
                   ?? throw new InvalidOperationException("Invalid card selection");

        await PlayCard(player, card, action);
    }

    private async Task PlayCard(Player player, Card card, ActionParameters action)
    {
        player.Play(card);
        await _game.GameService.NotifyCardPlayed(_game.Id, player.Id, card, action);

        var context = new GameContext(_game, player);
        var actions = card.GetGameActions(context, action);
        await ProcessActionResults(actions);
    }

    private async Task ProcessActionResults(IEnumerable<IGameAction> actions)
    {
        foreach (var action in actions)
        {
            try
            {
                switch (action)
                {
                    case Draw draw:
                        await HandleDraw(draw);
                        break;

                    case Discard discard:
                        await HandleDiscard(discard);
                        break;

                    case DiscardSelection discardSelection:
                        await HandleDiscardSelection(discardSelection);
                        break;

                    case Eliminate eliminate:
                        await HandleElimination(eliminate);
                        break;

                    case AddInfo addInfo:
                        await HandleAddInfo(addInfo);
                        break;

                    case AddEffect addEffect:
                        await HandleAddEffect(addEffect);
                        break;

                    case RemoveEffect removeEffect:
                        await HandleRemoveEffect(removeEffect);
                        break;

                    case Swap swap:
                        await HandleSwap(swap);
                        break;

                    case NoEffect:
                        await HandleNoEffect();
                        break;

                    case PlaceCardsOnBottom dapob:
                        await HandlePlaceOnBottom(dapob);
                        break;

                    case Log log:
                        await HandleLog(log);
                        break;

                    case Shuffle:
                        await HandleShuffle();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }

    private async Task HandleDraw(Draw draw)
    {
        var player = draw.Source;
        var cards = _deck.DrawMany(draw.Amount);        
        player.Draw(cards);
        await _game.GameService.NotifyDraw(_game.Id, player.Id, cards);
    }

    private async Task HandleDiscard(Discard discard)
    {
        var player = discard.Source;
        var discarded = player.Play(discard.Card);
        await _game.GameService.NotifyDiscard(_game.Id, player.Id, discarded);

        // Special case: Princess discard
        if (discarded.Type == CardType.Princess)
        {
            await ProcessActionResults([new Eliminate(player)]);
        }
    }

    private async Task HandleDiscardSelection(DiscardSelection discardSelection)
    {
        var player = discardSelection.Source;
        var selection = await _game.GameService.RequestCardSelection(
            player.Id,
            discardSelection.Amount,
            [.. player.Hand]);

        var tsks = new List<Task>();
        foreach (var card in selection)
        {
            tsks.Add(ProcessActionResults([new Discard(player, card)]));
        }
        await Task.WhenAll(tsks);
    }

    private async Task HandleElimination(Eliminate eliminate)
    {
        foreach (var player in eliminate.Players)
        {
            if (player.IsAlive)
            {
                player.Die();
                await _game.GameService.NotifyPlayerEliminated(_game.Id, player.Id);
            }
        }
    }

    private async Task HandleAddInfo(AddInfo addInfo)
    {
        addInfo.Source.OpponentInfo[addInfo.Target.Id] = [.. addInfo.Cards.Select(t => new Card(t))];
        await _game.GameService.NotifyPrivateInfo(
            _game.Id,
            addInfo.Source.Id,
            $"{addInfo.Target.Name}'s cards: {string.Join(", ", addInfo.Cards)}");
    }

    private async Task HandleAddEffect(AddEffect addEffect)
    {
        foreach (var effect in addEffect.Effects)
        {
            addEffect.Source.ActiveEffects.Add(effect);
            await _game.GameService.NotifyEffectApplied(_game.Id, addEffect.Source.Id, effect);
        }
    }

    private async Task HandleRemoveEffect(RemoveEffect removeEffect)
    {
        foreach (var effect in removeEffect.Effects)
        {
            removeEffect.Source.ActiveEffects.Remove(effect);
            await _game.GameService.NotifyEffectRemoved(_game.Id, removeEffect.Source.Id, effect);
        }
    }

    private async Task HandleSwap(Swap swap)
    {
        var tempHand = swap.Source.Hand.ToList();
        swap.Source.SetHand(swap.Target.Hand.ToList());
        swap.Target.SetHand(tempHand);

        var t1 = _game.GameService.NotifyHandUpdate(_game.Id, swap.Source.Id, swap.Source.Hand);
        var t2 = _game.GameService.NotifyHandUpdate(_game.Id, swap.Target.Id, swap.Target.Hand);
        var t3 = _game.GameService.NotifyLog(_game.Id, $"{swap.Source.Name} swapped hands with {swap.Target.Name}");
        await Task.WhenAll(t1, t2, t3);
    }

    private async Task HandleNoEffect()
    {
        await _game.GameService.NotifyLog(_game.Id, "The action had no effect");
    }

    private async Task HandlePlaceOnBottom(PlaceCardsOnBottom dapob)
    {
        var player = dapob.Source;
        var selection = await _game.GameService.RequestCardSelection(
            player.Id,
            dapob.Amount,
            player.Hand.ToArray());

        if (selection.Length != dapob.Amount || !selection.All(c => player.Hand.Contains(c)))
        {
            await ProcessActionResults([new Eliminate(player)]);
            return;
        }

        player.RemoveCards(selection);

        _deck.PutCardsOnBottom([.. selection]);        
    }

    private async Task HandleLog(Log log)
    {
        //TODO: proper patter matching and logging needed 
        var message = string.Join(" ", log.Text.Select(t => t.ToString()));
        _game.EventsHistory.Add(log);
        await _game.GameService.NotifyLog(_game.Id, message);
    }

    private Task HandleShuffle()
    {
        _deck.Shuffle();
        return Task.CompletedTask;
    }

    private List<Player> DetermineRoundWinners()
    {
        var alivePlayers = _playerOrder.Where(p => p.IsAlive).ToList();

        return alivePlayers.Count switch
        {
            0 => [], // Edge case
            1 => alivePlayers,
            _ => alivePlayers
                .GroupBy(p => p.Hand.Max(x => x.Type))
                .OrderByDescending(g => g.Key)
                .First()
                .ToList()
        };
    }
    private string[] ValidTargetrs() => _playerOrder
        .Where(p => p.IsAlive && !p.IsProtected())
        .Select(p => p.Id)
        .ToArray();
    private Player GetPlayerById(string id) =>
       _playerOrder.FirstOrDefault(p => p.Id == id)
        ?? throw new InvalidOperationException("Player not found");
}