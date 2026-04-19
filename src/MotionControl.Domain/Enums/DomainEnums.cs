namespace MotionControl.Domain.Enums;

/// <summary>
/// 轴状态
/// </summary>
public enum AxisState
{
    Disabled,
    Standstill,
    Homing,
    Jogging,
    Moving,
    Stopping,
    Alarm
}

/// <summary>
/// 系统状态
/// </summary>
public enum MachineState
{
    PowerOff,
    Initializing,
    Idle,
    Ready,
    Manual,
    Auto,
    Paused,
    Warning,
    Alarm,
    EmergencyStop,
    Maintenance
}

/// <summary>
/// 运动模式
/// </summary>
public enum MotionMode
{
    Idle,
    Jog,
    MoveAbs,
    MoveRel,
    Home,
    Stop
}

/// <summary>
/// 伺服状态
/// </summary>
public enum ServoState
{
    Disabled,
    Enabled,
    Error
}

/// <summary>
/// 回零状态
/// </summary>
public enum HomeState
{
    NotHomed,
    Homing,
    Homed,
    Failed
}

/// <summary>
/// 报警级别
/// </summary>
public enum AlarmLevel
{
    Info,
    Warning,
    Fault,
    Critical
}

/// <summary>
/// 轴类型
/// </summary>
public enum AxisType
{
    Linear,
    Rotary
}

/// <summary>
/// 组运动模式
/// </summary>
public enum GroupMotionMode
{
    Independent,
    Gantry,
    Cam,
    Gear
}
