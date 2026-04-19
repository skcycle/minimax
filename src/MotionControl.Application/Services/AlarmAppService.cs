using MotionControl.Diagnostics.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Interfaces;
using MotionControl.Domain.ValueObjects;
using MotionControl.Infrastructure.Logging;
using MotionControl.Contracts.Events;

namespace MotionControl.Application.Services;

/// <summary>
/// 报警应用服务实现
/// </summary>
public class AlarmAppService : IAlarmAppService
{
    private readonly IMachineRepository _machineRepository;
    private readonly IAlarmRecorder _alarmRecorder;
    private readonly ILogger _logger;

    public AlarmAppService(
        IMachineRepository machineRepository,
        IAlarmRecorder alarmRecorder,
        ILogger logger)
    {
        _machineRepository = machineRepository;
        _alarmRecorder = alarmRecorder;
        _logger = logger;
    }

    public IReadOnlyList<AlarmInfo> GetActiveAlarms()
    {
        var machine = _machineRepository.GetMachine();
        return machine.ActiveAlarms.Select(a => new AlarmInfo
        {
            AlarmId = a.Id,
            AxisId = a.AxisId,
            Code = a.Code.Code,
            Level = a.Level,
            Message = a.Description,
            OccurredAt = a.OccurredAt
        }).ToList();
    }

    public IReadOnlyList<AlarmInfo> GetAlarmHistory(DateTime? startTime, DateTime? endTime)
    {
        var records = _alarmRecorder.GetHistory(startTime, endTime);
        return records.Select(r => new AlarmInfo
        {
            AlarmId = r.Id,
            AxisId = r.AxisId,
            Code = r.AlarmCode,
            Level = r.Level,
            Message = r.Description,
            OccurredAt = r.OccurredAt
        }).ToList();
    }

    public void ClearAlarm(int alarmId)
    {
        var machine = _machineRepository.GetMachine();
        machine.ClearAlarm(alarmId);
        _alarmRecorder.ClearAlarm(alarmId);
        _logger.Info($"Alarm {alarmId} cleared");
    }

    public void ClearAllAlarms()
    {
        var machine = _machineRepository.GetMachine();
        machine.ClearAllAlarms();
        _logger.Info("All alarms cleared");
    }

    public void RecordAlarm(int axisId, int code, AlarmLevel level, string description)
    {
        var machine = _machineRepository.GetMachine();
        var alarmCode = new AlarmCode(code, description);
        machine.RaiseAlarm(axisId, alarmCode, level, description);
        _alarmRecorder.RecordAlarm(axisId, alarmCode, level, description);
        _logger.Warning($"Alarm raised: Axis {axisId}, Code {code}, {description}");
    }

    public async Task ExportHistoryAsync(string filePath, CancellationToken ct = default)
    {
        await _alarmRecorder.ExportAsync(filePath, ct);
        _logger.Info($"Alarm history exported to {filePath}");
    }
}
