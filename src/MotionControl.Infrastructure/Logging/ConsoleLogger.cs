namespace MotionControl.Infrastructure.Logging;

/// <summary>
/// 控制台日志实现
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;

    public ConsoleLogger(string categoryName, LogLevel minLevel = LogLevel.Debug)
    {
        _categoryName = categoryName;
        _minLevel = minLevel;
    }

    public void Debug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, args);
    }

    public void Info(string message, params object[] args)
    {
        Log(LogLevel.Info, message, args);
    }

    public void Warning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, args);
    }

    public void Error(string message, Exception? ex = null, params object[] args)
    {
        var fullMessage = ex != null
            ? $"{string.Format(message, args)} | Exception: {ex.Message}"
            : string.Format(message, args);
        Log(LogLevel.Error, fullMessage);
    }

    public void Fatal(string message, Exception? ex = null, params object[] args)
    {
        var fullMessage = ex != null
            ? $"{string.Format(message, args)} | Exception: {ex.Message}\n{ex.StackTrace}"
            : string.Format(message, args);
        Log(LogLevel.Fatal, fullMessage);
    }

    private void Log(LogLevel level, string message, params object[] args)
    {
        if (level < _minLevel) return;

        var formattedMessage = args.Length > 0
            ? string.Format(message, args)
            : message;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var output = $"[{timestamp}] [{levelStr}] [{_categoryName}] {formattedMessage}";

        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Fatal:
                Console.Error.WriteLine(output);
                break;
            default:
                Console.WriteLine(output);
                break;
        }
    }
}
