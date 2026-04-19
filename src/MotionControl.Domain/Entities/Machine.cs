using MotionControl.Domain.Enums;
using MotionControl.Domain.ValueObjects;
using MotionControl.Contracts.Events;

namespace MotionControl.Domain.Entities;

/// <summary>
/// 安全状态
/// </summary>
public class SafetyState
{
    public bool EmergencyStopPressed { get; set; }
    public bool EtherCatOnline { get; set; } = true;
    public bool AllAxesHomed { get; set; }
    public bool AllAxesEnabled { get; set; }
    public bool NoActiveAlarms { get; set; } = true;
    public bool DoorClosed { get; set; } = true;

    public bool CanEnableAxes => EtherCatOnline && !EmergencyStopPressed && NoActiveAlarms;
    public bool CanStartMotion => AllAxesHomed && AllAxesEnabled && NoActiveAlarms && !EmergencyStopPressed;
    public bool IsSafeForOperation => CanStartMotion;
}

/// <summary>
/// 报警实体
/// </summary>
public class Alarm
{
    public int Id { get; }
    public int AxisId { get; }
    public AlarmCode Code { get; }
    public AlarmLevel Level { get; }
    public string Description { get; }
    public DateTime OccurredAt { get; }
    public DateTime? ClearedAt { get; private set; }
    public bool IsActive => ClearedAt == null;

    public Alarm(int id, int axisId, AlarmCode code, AlarmLevel level, string description)
    {
        Id = id;
        AxisId = axisId;
        Code = code;
        Level = level;
        Description = description;
        OccurredAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        ClearedAt = DateTime.UtcNow;
    }

    public TimeSpan Duration => ClearedAt.HasValue
        ? ClearedAt.Value - OccurredAt
        : DateTime.UtcNow - OccurredAt;
}

/// <summary>
/// 配方实体
/// </summary>
public class Recipe
{
    public string Id { get; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<int, AxisParameters> AxisParameters { get; } = new();
    public Dictionary<string, double> CustomParameters { get; } = new();
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; private set; }

    public Recipe(string id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateParameter(int axisId, string key, double value)
    {
        if (!AxisParameters.TryGetValue(axisId, out var param))
        {
            param = new AxisParameters();
            AxisParameters[axisId] = param;
        }

        // 使用反射或动态设置
        var property = typeof(AxisParameters).GetProperty(key);
        property?.SetValue(param, value);
        ModifiedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 机器聚合根 - 整台设备
/// </summary>
public class Machine
{
    private readonly List<Alarm> _alarms = new();
    private readonly object _lock = new();

    public IReadOnlyList<Axis> Axes => _axes.AsReadOnly();
    public IReadOnlyList<AxisGroup> Groups => _groups.AsReadOnly();
    public IReadOnlyList<Alarm> ActiveAlarms => _alarms.Where(a => a.IsActive).ToList().AsReadOnly();
    public SafetyState SafetyState { get; } = new();

    private readonly List<Axis> _axes = new();
    private readonly List<AxisGroup> _groups = new();

    private MachineState _currentState = MachineState.PowerOff;
    public MachineState CurrentState
    {
        get => _currentState;
        set => SetState(value);
    }

    public Recipe? CurrentRecipe { get; private set; }

    // 事件
    public event EventHandler<MachineState>? StateChanged;
    public event EventHandler<Alarm>? AlarmRaised;
    public event EventHandler<Alarm>? AlarmCleared;

    public Machine()
    {
        // 初始化32个轴
        for (int i = 0; i < 32; i++)
        {
            _axes.Add(new Axis(i));
        }
    }

    public Axis GetAxis(int axisNumber)
    {
        return _axes.First(a => a.ControllerAxisNo == axisNumber);
    }

    public AxisGroup? GetGroup(string groupId)
    {
        return _groups.FirstOrDefault(g => g.GroupId == groupId);
    }

    public void AddGroup(AxisGroup group)
    {
        if (!_groups.Any(g => g.GroupId == group.GroupId))
        {
            _groups.Add(group);
        }
    }

    public void RemoveGroup(string groupId)
    {
        _groups.RemoveAll(g => g.GroupId == groupId);
    }

    private void SetState(MachineState newState)
    {
        if (_currentState != newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            StateChanged?.Invoke(this, newState);
        }
    }

    public void RaiseAlarm(int axisId, AlarmCode code, AlarmLevel level, string description)
    {
        lock (_lock)
        {
            if (_alarms.Any(a => a.IsActive && a.AxisId == axisId && a.Code.Code == code.Code))
            {
                return; // 防止重复报警
            }

            var alarm = new Alarm(
                id: _alarms.Count + 1,
                axisId: axisId,
                code: code,
                level: level,
                description: description
            );

            _alarms.Add(alarm);
            AlarmRaised?.Invoke(this, alarm);

            // 更新安全状态
            SafetyState.NoActiveAlarms = false;

            if (level == AlarmLevel.Critical)
            {
                CurrentState = MachineState.Alarm;
            }
        }
    }

    public void ClearAlarm(int alarmId)
    {
        lock (_lock)
        {
            var alarm = _alarms.FirstOrDefault(a => a.Id == alarmId && a.IsActive);
            if (alarm != null)
            {
                alarm.Clear();
                AlarmCleared?.Invoke(this, alarm);
            }

            SafetyState.NoActiveAlarms = !_alarms.Any(a => a.IsActive);
        }
    }

    public void ClearAllAlarms()
    {
        lock (_lock)
        {
            foreach (var alarm in _alarms.Where(a => a.IsActive))
            {
                alarm.Clear();
                AlarmCleared?.Invoke(this, alarm);
            }
            SafetyState.NoActiveAlarms = true;
        }
    }

    public void UpdateSafetyState()
    {
        SafetyState.AllAxesHomed = _axes.All(a => a.IsHomed);
        SafetyState.AllAxesEnabled = _axes.All(a => a.IsEnabled);
        SafetyState.EtherCatOnline = true; // 由诊断服务更新
    }

    public void LoadRecipe(Recipe recipe)
    {
        CurrentRecipe = recipe;
        foreach (var (axisId, parameters) in recipe.AxisParameters)
        {
            var axis = _axes.FirstOrDefault(a => a.Id.Value == axisId);
            axis?.Parameters.MergeFrom(parameters);
        }
    }
}

/// <summary>
/// 轴参数合并扩展
/// </summary>
public static class AxisParametersExtensions
{
    public static void MergeFrom(this AxisParameters target, AxisParameters source)
    {
        target.MaxVelocity = source.MaxVelocity;
        target.MaxAcceleration = source.MaxAcceleration;
        target.DefaultAccel = source.DefaultAccel;
        target.DefaultDecel = source.DefaultDecel;
        target.HomeSpeed = source.HomeSpeed;
        target.HomeLatchSpeed = source.HomeLatchSpeed;
        target.PositionFactor = source.PositionFactor;
        target.SoftwareLowLimit = source.SoftwareLowLimit;
        target.SoftwareHighLimit = source.SoftwareHighLimit;
        target.EncoderResolution = source.EncoderResolution;
        target.FollowingErrorLimit = source.FollowingErrorLimit;
    }
}
