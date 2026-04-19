using MotionControl.Contracts.Events;

namespace MotionControl.Infrastructure.Messaging;

/// <summary>
/// 事件总线接口 - 领域事件分发
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    void Publish<T>(T @event) where T : DomainEvent;

    /// <summary>
    /// 订阅事件
    /// </summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : DomainEvent;

    /// <summary>
    /// 异步发布事件
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : DomainEvent;
}
