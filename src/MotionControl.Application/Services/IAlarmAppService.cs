using MotionControl.Contracts.Events;

namespace MotionControl.Application.Services;

/// <summary>
/// 报警应用服务接口
/// </summary>
public interface IAlarmAppService
{
    /// <summary>
    /// 获取所有活动报警
    /// </summary>
    IReadOnlyList<AlarmInfo> GetActiveAlarms();

    /// <summary>
    /// 获取报警历史
    /// </summary>
    IReadOnlyList<AlarmInfo> GetAlarmHistory(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 清除报警
    /// </summary>
    void ClearAlarm(int alarmId);

    /// <summary>
    /// 清除所有报警
    /// </summary>
    void ClearAllAlarms();

    /// <summary>
    /// 记录报警
    /// </summary>
    void RecordAlarm(int axisId, int code, AlarmLevel level, string description);

    /// <summary>
    /// 导出历史记录
    /// </summary>
    Task ExportHistoryAsync(string filePath, CancellationToken ct = default);
}
