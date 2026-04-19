using System.Text.Json;
using MotionControl.Contracts.Events;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Diagnostics.Services;

/// <summary>
/// 诊断快照
/// </summary>
public class DiagnosticSnapshot
{
    public DateTime Timestamp { get; set; }
    public bool ControllerConnected { get; set; }
    public bool EtherCatOnline { get; set; }
    public int ActiveAlarmCount { get; set; }
    public Dictionary<int, double> AxisPositions { get; set; } = new();
    public Dictionary<int, string> AxisStates { get; set; } = new();
    public double MemoryUsageMb { get; set; }
    public double CpuUsage { get; set; }
}

/// <summary>
/// 诊断服务接口
/// </summary>
public interface IDiagnosticService
{
    /// <summary>
    /// 获取当前诊断快照
    /// </summary>
    DiagnosticSnapshot GetSnapshot();

    /// <summary>
    /// 导出诊断报告
    /// </summary>
    Task ExportReportAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// 诊断服务实现
/// </summary>
public class DiagnosticService : IDiagnosticService
{
    private readonly ILogger _logger;
    private readonly IAlarmRecorder _alarmRecorder;
    private readonly List<DiagnosticSnapshot> _history = new();
    private const int MaxHistorySize = 1000;

    public DiagnosticService(ILogger logger, IAlarmRecorder alarmRecorder)
    {
        _logger = logger;
        _alarmRecorder = alarmRecorder;
    }

    public DiagnosticSnapshot GetSnapshot()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        return new DiagnosticSnapshot
        {
            Timestamp = DateTime.UtcNow,
            ActiveAlarmCount = _alarmRecorder.GetActiveAlarms().Count,
            MemoryUsageMb = process.WorkingSet64 / 1024.0 / 1024.0,
            CpuUsage = 0 // 需要性能计数器实现
        };
    }

    public async Task ExportReportAsync(string filePath, CancellationToken ct = default)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            CurrentSnapshot = GetSnapshot(),
            RecentAlarms = _alarmRecorder.GetHistory(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json, ct);
        _logger.Info($"Diagnostic report exported to {filePath}");
    }
}
