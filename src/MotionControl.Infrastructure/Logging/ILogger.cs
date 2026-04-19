namespace MotionControl.Infrastructure.Logging;

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4
}

/// <summary>
/// 日志接口
/// </summary>
public interface ILogger
{
    void Debug(string message, params object[] args);
    void Info(string message, params object[] args);
    void Warning(string message, params object[] args);
    void Error(string message, Exception? ex = null, params object[] args);
    void Fatal(string message, Exception? ex = null, params object[] args);
}
