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
    IReadOnlyList<AlarmDto> GetActiveAlarms();

    /// <summary>
    /// 获取报警历史
    /// </summary>
    IReadOnlyList<AlarmDto> GetAlarmHistory(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 清除报警
    /// </summary>
    Task<CommandResultDto> ClearAlarmAsync(int alarmId, CancellationToken ct = default);

    /// <summary>
    /// 清除所有报警
    /// </summary>
    Task<CommandResultDto> ClearAllAlarmsAsync(CancellationToken ct = default);

    /// <summary>
    /// 复位轴报警
    /// </summary>
    Task<CommandResultDto> ResetAxisAlarmAsync(int axisNumber, CancellationToken ct = default);
}

/// <summary>
/// 报警DTO
/// </summary>
public record AlarmDto(
    int Id,
    int AxisId,
    int AlarmCode,
    string Description,
    AlarmLevel Level,
    DateTime OccurredAt,
    DateTime? ClearedAt,
    bool IsActive
);
