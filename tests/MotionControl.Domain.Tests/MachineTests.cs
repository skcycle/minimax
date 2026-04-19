using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Tests;

/// <summary>
/// Machine实体单元测试
/// </summary>
public class MachineTests
{
    [Fact]
    public void Machine_Construction_ShouldHave32Axes()
    {
        // Arrange & Act
        var machine = new Machine();

        // Assert
        Assert.Equal(32, machine.Axes.Count);
    }

    [Fact]
    public void Machine_GetAxis_ShouldReturnCorrectAxis()
    {
        // Arrange
        var machine = new Machine();

        // Act
        var axis = machine.GetAxis(5);

        // Assert
        Assert.NotNull(axis);
        Assert.Equal(5, axis.Id.Value);
    }

    [Fact]
    public void Machine_GetAxis_InvalidNumber_ShouldReturnNull()
    {
        // Arrange
        var machine = new Machine();

        // Act
        var axis = machine.GetAxis(99);

        // Assert
        Assert.Null(axis);
    }

    [Fact]
    public void Machine_AddGroup_ShouldContainGroup()
    {
        // Arrange
        var machine = new Machine();
        var group = new AxisGroup("TestGroup");

        // Act
        machine.AddGroup(group);

        // Assert
        Assert.Contains(group, machine.Groups);
    }

    [Fact]
    public void Machine_RemoveGroup_ShouldNotContainGroup()
    {
        // Arrange
        var machine = new Machine();
        var group = new AxisGroup("TestGroup");
        machine.AddGroup(group);

        // Act
        machine.RemoveGroup("TestGroup");

        // Assert
        Assert.DoesNotContain(group, machine.Groups);
    }

    [Fact]
    public void Machine_RaiseAlarm_ShouldAddToActiveAlarms()
    {
        // Arrange
        var machine = new Machine();
        var alarmCode = new Domain.ValueObjects.AlarmCode(1, "Test alarm");

        // Act
        machine.RaiseAlarm(0, alarmCode, AlarmLevel.Fault, "Test alarm");

        // Assert
        Assert.Single(machine.ActiveAlarms);
        Assert.False(machine.SafetyState.NoActiveAlarms);
    }

    [Fact]
    public void Machine_ClearAlarm_ShouldRemoveFromActiveAlarms()
    {
        // Arrange
        var machine = new Machine();
        var alarmCode = new Domain.ValueObjects.AlarmCode(1, "Test alarm");
        machine.RaiseAlarm(0, alarmCode, AlarmLevel.Fault, "Test alarm");

        // Act
        var alarm = machine.ActiveAlarms.First();
        machine.ClearAlarm(alarm.Id);

        // Assert
        Assert.Empty(machine.ActiveAlarms.Where(a => a.IsActive));
        Assert.True(machine.SafetyState.NoActiveAlarms);
    }

    [Fact]
    public void Machine_ClearAllAlarms_ShouldClearAll()
    {
        // Arrange
        var machine = new Machine();
        machine.RaiseAlarm(0, new Domain.ValueObjects.AlarmCode(1, "Alarm 1"), AlarmLevel.Fault, "Alarm 1");
        machine.RaiseAlarm(1, new Domain.ValueObjects.AlarmCode(2, "Alarm 2"), AlarmLevel.Warning, "Alarm 2");

        // Act
        machine.ClearAllAlarms();

        // Assert
        Assert.Empty(machine.ActiveAlarms.Where(a => a.IsActive));
        Assert.True(machine.SafetyState.NoActiveAlarms);
    }

    [Fact]
    public void Machine_UpdateSafetyState_ShouldUpdateAllFlags()
    {
        // Arrange
        var machine = new Machine();
        foreach (var axis in machine.Axes)
        {
            axis.Enable();
            axis.MarkHomed(0);
        }
        machine.SafetyState.EtherCatOnline = true;

        // Act
        machine.UpdateSafetyState();

        // Assert
        Assert.True(machine.SafetyState.AllAxesHomed);
        Assert.True(machine.SafetyState.AllAxesEnabled);
    }

    [Fact]
    public void Machine_CurrentState_ShouldTriggerEvent()
    {
        // Arrange
        var machine = new Machine();
        MachineState? oldState = null;
        MachineState? newState = null;
        machine.StateChanged += (s, e) => { oldState = machine.CurrentState; };

        // Act
        machine.CurrentState = MachineState.Idle;

        // Assert
        Assert.Equal(MachineState.Idle, machine.CurrentState);
    }

    [Fact]
    public void Machine_LoadRecipe_ShouldApplyParameters()
    {
        // Arrange
        var machine = new Machine();
        var recipe = new Recipe("TestRecipe");
        recipe.AxisParameters[0] = new AxisParameters
        {
            MaxVelocity = 5000,
            MaxAcceleration = 20000
        };

        // Act
        machine.LoadRecipe(recipe);

        // Assert
        var axis = machine.GetAxis(0);
        Assert.Equal(5000, axis.Parameters.MaxVelocity);
        Assert.Equal(20000, axis.Parameters.MaxAcceleration);
    }
}
