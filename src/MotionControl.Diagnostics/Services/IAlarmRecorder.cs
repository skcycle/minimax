using MotionControl.Domain.ValueObjects;
using MotionControl.Domain.Enums;

namespace MotionControl.Diagnostics.Services;

/// <summary>
/// 报警记录器接口
/// </summary>
public interface IAlarmRecorder
{
    /// <summary>
    /// 记录报警
    /// </summary>
    void RecordAlarm(int axisId, AlarmCode code, AlarmLevel level, string description);

    /// <summary>
    /// 清除报警记录
    /// </summary>
    void ClearAlarm(int alarmId);

    /// <summary>
    /// 获取活动报警
    /// </summary>
    IReadOnlyList<AlarmRecord> GetActiveAlarms();

    /// <summary>
    /// 获取报警历史
    /// </summary>
    IReadOnlyList<AlarmRecord> GetHistory(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 导出历史记录
    /// </summary>
    Task ExportAsync(string filePath, CancellationToken ct = default);
}
