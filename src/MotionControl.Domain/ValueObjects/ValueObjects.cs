namespace MotionControl.Domain.ValueObjects;

/// <summary>
/// 轴编号
/// </summary>
public readonly record struct AxisId
{
    public int Value { get; }
    public AxisId(int value) => Value = value;
    public static implicit operator int(AxisId id) => id.Value;
    public static implicit operator AxisId(int value) => new(value);
    public override string ToString() => Value.ToString();
}

/// <summary>
/// 轴位置
/// </summary>
public readonly record struct Position
{
    public double Value { get; }
    public Position(double value) => Value = value;
    public static implicit operator double(Position pos) => pos.Value;
    public static implicit operator Position(double value) => new(value);
    public override string ToString() => $"{Value:F3}";
}

/// <summary>
/// 轴速度
/// </summary>
public readonly record struct Velocity
{
    public double Value { get; }
    public Velocity(double value) => Value = value;
    public static implicit operator double(Velocity vel) => vel.Value;
    public static implicit operator Velocity(double value) => new(value);
    public override string ToString() => $"{Value:F2}";
}

/// <summary>
/// 加速度
/// </summary>
public readonly record struct Acceleration
{
    public double Value { get; }
    public Acceleration(double value) => Value = value;
    public static implicit operator double(Acceleration acc) => acc.Value;
    public static implicit operator Acceleration(double value) => new(value);
    public override string ToString() => $"{Value:F2}";
}

/// <summary>
/// 软限位
/// </summary>
public readonly record struct SoftLimit
{
    public double Min { get; }
    public double Max { get; }
    public SoftLimit(double min, double max)
    {
        Min = min;
        Max = max;
    }
    public bool IsWithinLimits(double position) => position >= Min && position <= Max;
    public override string ToString() => $"[{Min:F3}, {Max:F3}]";
}

/// <summary>
/// 状态字
/// </summary>
public readonly struct StatusWord
{
    private readonly uint _value;
    public StatusWord(uint value) => _value = value;

    public bool IsEnabled => (_value & 0x0001) != 0;
    public bool IsHomed => (_value & 0x0002) != 0;
    public bool HasAlarm => (_value & 0x0004) != 0;
    public bool IsMoving => (_value & 0x0008) != 0;
    public bool PositiveLimit => (_value & 0x0010) != 0;
    public bool NegativeLimit => (_value & 0x0020) != 0;
    public bool IsInPosition => (_value & 0x0040) != 0;
    public bool IsStopping => (_value & 0x0080) != 0;

    public uint RawValue => _value;
    public static implicit operator uint(StatusWord sw) => sw._value;
}

/// <summary>
/// 报警码
/// </summary>
public readonly record struct AlarmCode
{
    public int Code { get; }
    public string Description { get; }
    public AlarmCode(int code, string description = "")
    {
        Code = code;
        Description = description;
    }
    public static implicit operator int(AlarmCode ac) => ac.Code;
    public override string ToString() => Code > 0 ? $"{Code}: {Description}" : "No Alarm";
}

