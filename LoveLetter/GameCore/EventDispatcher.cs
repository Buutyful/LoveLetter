namespace LoveLetter.GameCore;
public interface IEvent
{
    Guid GameId { get; }
    DateTime Date { get; }
};

public delegate Task EventHandler<T>(T @event) where T : IEvent;
public static class EventDispatcher
{    
    private static readonly Dictionary<Type, Delegate> _handlers = [];
    public static void Register<T>(EventHandler<T> handler) where T : IEvent
    {
        if (_handlers.TryGetValue(typeof(T), out var existing))
        {
            _handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }
        else
        {
            _handlers.Add(typeof(T), handler);
        }
    }

    public static async Task Raise<T>(T @event) where T : IEvent
    {
        if (_handlers.TryGetValue(typeof(T), out var delegates))
        {
            var handlerList = delegates.GetInvocationList().Cast<EventHandler<T>>();

            var tasks = new List<Task>();

            foreach (var handler in handlerList)
            {
                tasks.Add(handler.Invoke(@event));
            }
            await Task.WhenAll(tasks);
        }
    }
}