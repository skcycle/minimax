namespace MotionControl.Infrastructure.Configuration;

/// <summary>
/// 配置提供器接口
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// 获取配置值，带默认值
    /// </summary>
    T GetOrDefault<T>(string key, T defaultValue);

    /// <summary>
    /// 设置配置值
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// 保存配置
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// 重新加载配置
    /// </summary>
    Task ReloadAsync(CancellationToken ct = default);
}

/// <summary>
/// 轴配置
/// </summary>
public class AxisConfiguration
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public double MaxVelocity { get; set; } = 1000;
    public double MaxAcceleration { get; set; } = 10000;
    public double HomeSpeed { get; set; } = 100;
    public double HomeLatchSpeed { get; set; } = 10;
    public double SoftwareLowLimit { get; set; } = -100000;
    public double SoftwareHighLimit { get; set; } = 100000;
    public int HomeMode { get; set; } = 0;
    public int HomeDirection { get; set; } = 0;
}

/// <summary>
/// 控制器配置
/// </summary>
public class ControllerConfiguration
{
    public string IpAddress { get; set; } = "192.168.0.100";
    public int Port { get; set; } = 5005;
    public int ConnectionTimeoutMs { get; set; } = 5000;
    public int ReconnectIntervalMs { get; set; } = 3000;
    public int MaxReconnectAttempts { get; set; } = 5;
}

/// <summary>
/// 轮询配置
/// </summary>
public class PollingConfiguration
{
    public int IntervalMs { get; set; } = 100;
    public bool BatchRead { get; set; } = true;
}

/// <summary>
/// 日志配置
/// </summary>
public class LoggingConfiguration
{
    public string LogLevel { get; set; } = "Debug";
    public bool ConsoleEnabled { get; set; } = true;
    public bool FileEnabled { get; set; } = false;
    public string FilePath { get; set; } = "logs/motioncontrol.log";
}

/// <summary>
/// 安全配置
/// </summary>
public class SafetyConfiguration
{
    public bool EnableEStopCheck { get; set; } = true;
    public bool EnableSoftLimits { get; set; } = true;
    public bool EnableFollowErrorCheck { get; set; } = true;
    public double FollowErrorThreshold { get; set; } = 1.0;
}

/// <summary>
/// 系统配置
/// </summary>
public class SystemConfiguration
{
    public LoggingConfiguration Logging { get; set; } = new();
    public ControllerConfiguration Controller { get; set; } = new();
    public PollingConfiguration Polling { get; set; } = new();
    public List<AxisConfiguration> Axes { get; set; } = new();
    public SafetyConfiguration Safety { get; set; } = new();
}
