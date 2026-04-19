namespace MotionControl.Device.Abstractions.Exceptions;

/// <summary>
/// 设备异常基类
/// </summary>
public class DeviceException : Exception
{
    public int ErrorCode { get; }
    public string? DeviceAddress { get; }

    public DeviceException(int errorCode, string? message = null)
        : base(message ?? $"Device error: {errorCode}")
    {
        ErrorCode = errorCode;
    }

    public DeviceException(string message)
        : base(message)
    {
        ErrorCode = -1;
    }
}

/// <summary>
/// 连接异常
/// </summary>
public class DeviceConnectionException : DeviceException
{
    public DeviceConnectionException(string message)
        : base(message)
    {
    }

    public DeviceConnectionException(int errorCode, string? message = null)
        : base(errorCode, message)
    {
    }
}

/// <summary>
/// 运动命令异常
/// </summary>
public class MotionCommandException : DeviceException
{
    public int AxisNumber { get; }
    public MotionCommandType CommandType { get; }

    public MotionCommandException(int axisNumber, MotionCommandType commandType, int errorCode, string? message = null)
        : base(errorCode, message)
    {
        AxisNumber = axisNumber;
        CommandType = commandType;
    }
}

/// <summary>
/// 运动命令类型
/// </summary>
public enum MotionCommandType
{
    MoveAbsolute,
    MoveRelative,
    Jog,
    Stop,
    Home,
    Enable,
    Disable,
    Reset
}

/// <summary>
/// 超时异常
/// </summary>
public class CommandTimeoutException : DeviceException
{
    public int AxisNumber { get; }
    public TimeSpan Timeout { get; }

    public CommandTimeoutException(int axisNumber, TimeSpan timeout)
        : base($"Command timeout for axis {axisNumber} after {timeout.TotalMilliseconds}ms")
    {
        AxisNumber = axisNumber;
        Timeout = timeout;
    }
}

/// <summary>
/// 安全联锁异常
/// </summary>
public class SafetyInterlockException : DeviceException
{
    public string InterlockName { get; }

    public SafetyInterlockException(string interlockName, string reason)
        : base($"Safety interlock '{interlockName}' triggered: {reason}")
    {
        InterlockName = interlockName;
    }
}
