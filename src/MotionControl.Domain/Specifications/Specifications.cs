using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Specifications;

/// <summary>
/// 轴可运动规整
/// </summary>
public class AxisCanMoveSpecification
{
    public static (bool IsSatisfied, string? Reason) Check(Axis axis)
    {
        if (axis.ServoState != ServoState.Enabled)
            return (false, $"轴 {axis.Id} 未使能");

        if (!axis.IsHomed)
            return (false, $"轴 {axis.Id} 未回零");

        if (axis.HasAlarm)
            return (false, $"轴 {axis.Id} 报警中: {axis.CurrentAlarm}");

        if (axis.PositiveLimitTriggered)
            return (false, $"轴 {axis.Id} 正向限位触发");

        if (axis.NegativeLimitTriggered)
            return (false, $"轴 {axis.Id} 负向限位触发");

        if (axis.State == AxisState.Moving)
            return (false, $"轴 {axis.Id} 正在运动中");

        if (axis.State == AxisState.Homing)
            return (false, $"轴 {axis.Id} 正在回零中");

        return (true, null);
    }
}

/// <summary>
/// 轴可回零规整
/// </summary>
public class AxisCanHomeSpecification
{
    public static (bool IsSatisfied, string? Reason) Check(Axis axis)
    {
        if (axis.ServoState != ServoState.Enabled)
            return (false, $"轴 {axis.Id} 未使能");

        if (axis.HasAlarm)
            return (false, $"轴 {axis.Id} 报警中");

        if (axis.State == AxisState.Moving)
            return (false, $"轴 {axis.Id} 正在运动中");

        if (axis.State == AxisState.Homing)
            return (false, $"轴 {axis.Id} 正在回零中");

        return (true, null);
    }
}

/// <summary>
/// 系统可运动规整
/// </summary>
public class SystemReadySpecification
{
    public static (bool IsSatisfied, string? Reason) Check(Machine machine)
    {
        if (machine.SafetyState.EmergencyStopPressed)
            return (false, "急停按下");

        if (!machine.SafetyState.EtherCatOnline)
            return (false, "EtherCAT离线");

        if (!machine.SafetyState.NoActiveAlarms)
            return (false, "存在报警未清除");

        if (!machine.SafetyState.AllAxesHomed)
            return (false, "存在轴未回零");

        if (!machine.SafetyState.AllAxesEnabled)
            return (false, "存在轴未使能");

        return (true, null);
    }
}

/// <summary>
/// 轴组可运动规整
/// </summary>
public class GroupCanMoveSpecification
{
    public static (bool IsSatisfied, string? Reason) Check(AxisGroup group)
    {
        if (!group.ValidateSynchronization())
            return (false, $"组 {group.GroupId} 同步配置无效");

        if (group.IsBusy)
            return (false, $"组 {group.GroupId} 正在运动中");

        foreach (var axis in group.Axes)
        {
            var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
            if (!canMove)
                return (false, reason);
        }

        return (true, null);
    }
}
