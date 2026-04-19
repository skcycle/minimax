namespace MotionControl.Contracts.Events;

/// <summary>
/// 领域事件基类
/// </summary>
public abstract record DomainEvent(DateTime OccurredAt)
{
    protected DomainEvent() : this(DateTime.UtcNow) { }
}

/// <summary>
/// 轴状态变更事件
/// </summary>
public record AxisStateChangedEvent(int AxisId, string OldState, string NewState) : DomainEvent();

/// <summary>
/// 轴位置变更事件
/// </summary>
public record AxisPositionChangedEvent(int AxisId, double OldPosition, double NewPosition) : DomainEvent();

/// <summary>
/// 轴报警事件
/// </summary>
public record AxisAlarmRaisedEvent(int AxisId, int AlarmCode, string AlarmDescription) : DomainEvent();

/// <summary>
/// 轴报警清除事件
/// </summary>
public record AxisAlarmClearedEvent(int AxisId) : DomainEvent();

/// <summary>
/// 轴回零完成事件
/// </summary>
public record AxisHomedEvent(int AxisId, double HomePosition) : DomainEvent();

/// <summary>
/// 轴使能状态变更事件
/// </summary>
public record AxisServoStateChangedEvent(int AxisId, bool IsEnabled) : DomainEvent();

/// <summary>
/// 系统状态变更事件
/// </summary>
public record SystemStateChangedEvent(string OldState, string NewState) : DomainEvent();

/// <summary>
/// 报警产生事件
/// </summary>
public record AlarmRaisedEvent(int AlarmId, int AlarmCode, string Description, AlarmLevel Level) : DomainEvent();

/// <summary>
/// 报警清除事件
/// </summary>
public record AlarmClearedEvent(int AlarmId) : DomainEvent();

/// <summary>
/// 控制器连接状态变更事件
/// </summary>
public record ControllerConnectionChangedEvent(bool IsConnected, string? Message) : DomainEvent();

/// <summary>
/// 轴组运动完成事件
/// </summary>
public record GroupMotionCompletedEvent(string GroupId) : DomainEvent();

/// <summary>
/// 安全联锁触发事件
/// </summary>
public record SafetyInterlockTriggeredEvent(string InterlockName, string Reason) : DomainEvent();

/// <summary>
/// 报警级别
/// </summary>
public enum AlarmLevel
{
    Info = 0,
    Warning = 1,
    Fault = 2,
    Critical = 3
}
