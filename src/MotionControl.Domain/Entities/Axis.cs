using MotionControl.Domain.Enums;
using MotionControl.Domain.ValueObjects;

namespace MotionControl.Domain.Entities;

/// <summary>
/// 轴参数配置
/// </summary>
public class AxisParameters
{
    public double MaxVelocity { get; set; } = 1000;      // 最大速度 mm/min
    public double MaxAcceleration { get; set; } = 10000; // 最大加速度 mm/s²
    public double DefaultAccel { get; set; } = 5000;      // 默认加速度
    public double DefaultDecel { get; set; } = 5000;      // 默认减速度
    public double HomeSpeed { get; set; } = 100;          // 回零速度
    public double HomeLatchSpeed { get; set; } = 10;      // 回零捕获速度
    public double PositionFactor { get; set; } = 1;      // 位置系数 (脉冲/单位)
    public double SoftwareLowLimit { get; set; } = -100000;
    public double SoftwareHighLimit { get; set; } = 100000;
    public int EncoderResolution { get; set; } = 10000;   // 编码器分辨率
    public double FollowingErrorLimit { get; set; } = 1.0; // 跟随误差限制
}

/// <summary>
/// 轴实体
/// </summary>
public class Axis
{
    public AxisId Id { get; }
    public string Name { get; set; } = string.Empty;
    public int ControllerAxisNo { get; }
    public string? GroupId { get; set; }

    // 状态
    private AxisState _state = AxisState.Disabled;
    public AxisState State
    {
        get => _state;
        set => SetState(value);
    }

    private ServoState _servoState = ServoState.Disabled;
    public ServoState ServoState
    {
        get => _servoState;
        set => SetServoState(value);
    }

    private HomeState _homeState = HomeState.NotHomed;
    public HomeState HomeState
    {
        get => _homeState;
        set => _homeState = value;
    }

    // 位置和速度
    private double _currentPosition;
    public double CurrentPosition
    {
        get => _currentPosition;
        set => SetPosition(value);
    }

    private double _currentVelocity;
    public double CurrentVelocity
    {
        get => _currentVelocity;
        set => _currentVelocity = value;
    }

    private double _targetPosition;
    public double TargetPosition
    {
        get => _targetPosition;
        set => _targetPosition = value;
    }

    // 标志位
    public bool IsEnabled => _servoState == ServoState.Enabled;
    public bool IsHomed => _homeState == HomeState.Homed;
    public bool HasAlarm { get; private set; }
    public bool IsMoving => _state == AxisState.Moving;
    public bool IsInPosition => Math.Abs(_currentPosition - _targetPosition) < 0.001;
    public bool PositiveLimitTriggered { get; private set; }
    public bool NegativeLimitTriggered { get; private set; }

    // 参数
    public AxisParameters Parameters { get; set; } = new();

    // 报警
    private AlarmCode _currentAlarm;
    public AlarmCode CurrentAlarm
    {
        get => _currentAlarm;
        private set => _currentAlarm = value;
    }

    public Axis(int axisNumber)
    {
        Id = new AxisId(axisNumber);
        ControllerAxisNo = axisNumber;
    }

    // 状态变更
    private void SetState(AxisState newState)
    {
        if (_state != newState)
        {
            var oldState = _state;
            _state = newState;
            StateChanged?.Invoke(this, (oldState, newState));
        }
    }

    private void SetServoState(ServoState newState)
    {
        if (_servoState != newState)
        {
            var oldState = _servoState;
            _servoState = newState;
            ServoStateChanged?.Invoke(this, (oldState, newState));
        }
    }

    private void SetPosition(double newPosition)
    {
        if (Math.Abs(_currentPosition - newPosition) > 0.0001)
        {
            var oldPosition = _currentPosition;
            _currentPosition = newPosition;
            PositionChanged?.Invoke(this, (oldPosition, newPosition));
        }
    }

    // 业务方法
    public void Enable() => ServoState = ServoState.Enabled;
    public void Disable() => ServoState = ServoState.Disabled;

    public void MarkHomed(double position = 0)
    {
        _currentPosition = position;
        _homeState = HomeState.Homed;
        HomeCompleted?.Invoke(this, position);
    }

    public void SetAlarm(int code, string description = "")
    {
        HasAlarm = true;
        CurrentAlarm = new AlarmCode(code, description);
        State = AxisState.Alarm;
    }

    public void ClearAlarm()
    {
        HasAlarm = false;
        CurrentAlarm = new AlarmCode(0);
    }

    public void UpdateFeedback(double position, double velocity, uint statusWord)
    {
        _currentPosition = position;
        _currentVelocity = velocity;

        PositiveLimitTriggered = (statusWord & 0x0010) != 0;
        NegativeLimitTriggered = (statusWord & 0x0020) != 0;

        if ((statusWord & 0x0004) != 0 && !HasAlarm)
        {
            // 驱动器报警
        }
    }

    public bool CanMove()
    {
        return IsEnabled
            && IsHomed
            && !HasAlarm
            && !PositiveLimitTriggered
            && !NegativeLimitTriggered
            && (_state == AxisState.Standstill || _state == AxisState.Disabled);
    }

    public bool CanHome()
    {
        return IsEnabled && !HasAlarm && _state != AxisState.Homing && _state != AxisState.Moving;
    }

    public bool IsWithinSoftLimits(double position)
    {
        return position >= Parameters.SoftwareLowLimit && position <= Parameters.SoftwareHighLimit;
    }

    // 事件
    public event EventHandler<(AxisState Old, AxisState New)>? StateChanged;
    public event EventHandler<(ServoState Old, ServoState New)>? ServoStateChanged;
    public event EventHandler<(double Old, double New)>? PositionChanged;
    public event EventHandler<double>? HomeCompleted;
}
