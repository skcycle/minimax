using System.Collections.Concurrent;
using MotionControl.Contracts.Events;

namespace MotionControl.Infrastructure.Messaging;

/// <summary>
/// 内存事件总线实现
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T @event) where T : DomainEvent
    {
        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    ((Action<T>)handler)(@event);
                }
                catch (Exception ex)
                {
                    // 日志记录但不中断其他处理器
                    Console.Error.WriteLine($"Event handler error: {ex.Message}");
                }
            }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : DomainEvent
    {
        var eventType = typeof(T);
        _handlers.AddOrUpdate(
            eventType,
            _ => new List<Delegate> { handler },
            (_, handlers) =>
            {
                handlers.Add(handler);
                return handlers;
            });

        // 返回一个可以取消订阅的句柄
        return new EventSubscription(() => Unsubscribe(handler));

        void Unsubscribe(Action<T> h)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(h);
            }
        }
    }

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : DomainEvent
    {
        return Task.Run(() => Publish(@event), ct);
    }

    private class EventSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public EventSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe();
                _disposed = true;
            }
        }
    }
}
