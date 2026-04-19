namespace MotionControl.Contracts.Constants;

/// <summary>
/// 系统常量
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// 默认轴数量
    /// </summary>
    public const int DefaultAxisCount = 32;

    /// <summary>
    /// 默认轮询周期(毫秒)
    /// </summary>
    public const int DefaultPollingIntervalMs = 100;

    /// <summary>
    /// 默认命令超时(毫秒)
    /// </summary>
    public const int DefaultCommandTimeoutMs = 5000;

    /// <summary>
    /// 默认回零超时(毫秒)
    /// </summary>
    public const int DefaultHomingTimeoutMs = 60000;
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
/// 回零模式
/// </summary>
public enum HomeMode
{
    /// <summary>原点限位回零</summary>
    OriginLimit = 0,
    /// <summary>正向限位回零</summary>
    PositiveLimit = 1,
    /// <summary>负向限位回零</summary>
    NegativeLimit = 2,
    /// <summary>Z相回零</summary>
    ZPhase = 3,
    /// <summary>当前点回零</summary>
    CurrentPosition = 4,
    /// <summary>原点开关回零</summary>
    OriginSwitch = 5
}

/// <summary>
/// 回零方向
/// </summary>
public enum HomeDirection
{
    Positive,
    Negative
}

/// <summary>
/// 控制器状态
/// </summary>
public enum ControllerState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
