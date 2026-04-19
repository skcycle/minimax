using System.Collections.Concurrent;
using System.Text.Json;
using MotionControl.Contracts.Events;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;

namespace MotionControl.Diagnostics.Services;

/// <summary>
/// 报警记录器 - 持久化报警历史
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

/// <summary>
/// 报警记录
/// </summary>
public class AlarmRecord
{
    public int Id { get; set; }
    public int AxisId { get; set; }
    public int AlarmCode { get; set; }
    public string Description { get; set; } = "";
    public AlarmLevel Level { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public bool IsActive => !ClearedAt.HasValue;
    public TimeSpan Duration => ClearedAt.HasValue
        ? ClearedAt.Value - OccurredAt
        : DateTime.UtcNow - OccurredAt;
}

/// <summary>
/// 报警记录器实现
/// </summary>
public class AlarmRecorder : IAlarmRecorder
{
    private readonly ConcurrentDictionary<int, AlarmRecord> _alarms = new();
    private int _nextId = 1;
    private readonly object _lock = new();
    private readonly string _storagePath;

    public AlarmRecorder(string? storagePath = null)
    {
        _storagePath = storagePath ?? "alarms.json";
        LoadHistory();
    }

    public void RecordAlarm(int axisId, AlarmCode code, AlarmLevel level, string description)
    {
        var record = new AlarmRecord
        {
            Id = Interlocked.Increment(ref _nextId),
            AxisId = axisId,
            AlarmCode = code.Code,
            Description = description,
            Level = level,
            OccurredAt = DateTime.UtcNow
        };

        _alarms[record.Id] = record;
        SaveHistory();
    }

    public void ClearAlarm(int alarmId)
    {
        if (_alarms.TryGetValue(alarmId, out var record))
        {
            record.ClearedAt = DateTime.UtcNow;
            SaveHistory();
        }
    }

    public IReadOnlyList<AlarmRecord> GetActiveAlarms()
    {
        return _alarms.Values
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.OccurredAt)
            .ToList();
    }

    public IReadOnlyList<AlarmRecord> GetHistory(DateTime? startTime = null, DateTime? endTime = null)
    {
        var query = _alarms.Values.AsEnumerable();

        if (startTime.HasValue)
            query = query.Where(a => a.OccurredAt >= startTime.Value);
        if (endTime.HasValue)
            query = query.Where(a => a.OccurredAt <= endTime.Value);

        return query
            .OrderByDescending(a => a.OccurredAt)
            .ToList();
    }

    public async Task ExportAsync(string filePath, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(_alarms.Values.ToList(), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(filePath, json, ct);
    }

    private void SaveHistory()
    {
        try
        {
            var json = JsonSerializer.Serialize(_alarms.Values.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                var records = JsonSerializer.Deserialize<List<AlarmRecord>>(json);
                if (records != null)
                {
                    foreach (var record in records)
                    {
                        _alarms[record.Id] = record;
                        if (record.Id >= _nextId)
                            _nextId = record.Id + 1;
                    }
                }
            }
        }
        catch
        {
            // 忽略加载错误
        }
    }
}
