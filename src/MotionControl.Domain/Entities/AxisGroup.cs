using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Entities;

/// <summary>
/// 轴组实体
/// </summary>
public class AxisGroup
{
    public string GroupId { get; }
    public string Name { get; set; } = string.Empty;
    public List<Axis> Axes { get; } = new();
    public Axis? MasterAxis { get; private set; }
    public GroupMotionMode MotionMode { get; set; } = GroupMotionMode.Independent;

    public bool IsReady =>
        Axes.All(a => a.IsEnabled && a.IsHomed && !a.HasAlarm);

    public bool IsBusy =>
        Axes.Any(a => a.IsMoving || a.State == AxisState.Homing || a.State == AxisState.Stopping);

    public int AxisCount => Axes.Count;

    public AxisGroup(string groupId)
    {
        GroupId = groupId;
    }

    public void AddAxis(Axis axis)
    {
        if (!Axes.Contains(axis))
        {
            Axes.Add(axis);
            axis.GroupId = GroupId;
        }
    }

    public void RemoveAxis(Axis axis)
    {
        if (Axes.Remove(axis))
        {
            axis.GroupId = null;
        }
    }

    public void SetMasterAxis(Axis axis)
    {
        if (Axes.Contains(axis))
        {
            MasterAxis = axis;
        }
    }

    public IEnumerable<Axis> GetSlaveAxes()
    {
        return MasterAxis != null
            ? Axes.Where(a => a != MasterAxis)
            : Enumerable.Empty<Axis>();
    }

    public bool CanMoveTogether()
    {
        return IsReady && !IsBusy;
    }

    public bool ValidateSynchronization()
    {
        if (MotionMode == GroupMotionMode.Gantry && MasterAxis == null)
        {
            return false;
        }
        return true;
    }
}
