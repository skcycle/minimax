using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Specifications;

namespace MotionControl.Domain.Tests;

/// <summary>
/// 规整(Specification)单元测试
/// </summary>
public class SpecificationTests
{
    [Fact]
    public void AxisCanMove_WhenAxisIsEnabledHomedNoAlarm_ShouldSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);

        // Act
        var (isSatisfied, reason) = AxisCanMoveSpecification.Check(axis);

        // Assert
        Assert.True(isSatisfied);
        Assert.Null(reason);
    }

    [Fact]
    public void AxisCanMove_WhenAxisNotEnabled_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.MarkHomed(0);

        // Act
        var (isSatisfied, reason) = AxisCanMoveSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("未使能", reason);
    }

    [Fact]
    public void AxisCanMove_WhenAxisNotHomed_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();

        // Act
        var (isSatisfied, reason) = AxisCanMoveSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("未回零", reason);
    }

    [Fact]
    public void AxisCanMove_WhenAxisHasAlarm_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);
        axis.SetAlarm(1, "Test");

        // Act
        var (isSatisfied, reason) = AxisCanMoveSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("报警", reason);
    }

    [Fact]
    public void AxisCanMove_WhenPositiveLimitTriggered_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);
        // Simulate positive limit through status
        axis.UpdateFeedback(0, 0, 0x0011); // bit 0=enable, bit 4=positive limit

        // Act
        var (isSatisfied, reason) = AxisCanMoveSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("正向限位", reason);
    }

    [Fact]
    public void AxisCanHome_WhenAxisEnabledNoAlarmNotMoving_ShouldSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();

        // Act
        var (isSatisfied, reason) = AxisCanHomeSpecification.Check(axis);

        // Assert
        Assert.True(isSatisfied);
        Assert.Null(reason);
    }

    [Fact]
    public void AxisCanHome_WhenAxisNotEnabled_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);

        // Act
        var (isSatisfied, reason) = AxisCanHomeSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("未使能", reason);
    }

    [Fact]
    public void AxisCanHome_WhenAxisMoving_ShouldNotSatisfy()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.State = AxisState.Moving;

        // Act
        var (isSatisfied, reason) = AxisCanHomeSpecification.Check(axis);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("正在运动", reason);
    }

    [Fact]
    public void GroupCanMove_WhenAllAxesReady_ShouldSatisfy()
    {
        // Arrange
        var group = new AxisGroup("TestGroup");
        var axis1 = new Axis(0);
        var axis2 = new Axis(1);
        axis1.Enable();
        axis1.MarkHomed(0);
        axis2.Enable();
        axis2.MarkHomed(0);
        group.AddAxis(axis1);
        group.AddAxis(axis2);

        // Act
        var (isSatisfied, reason) = GroupCanMoveSpecification.Check(group);

        // Assert
        Assert.True(isSatisfied);
        Assert.Null(reason);
    }

    [Fact]
    public void SystemReady_WhenAllConditionsMet_ShouldSatisfy()
    {
        // Arrange
        var machine = new Machine();
        foreach (var axis in machine.Axes)
        {
            axis.Enable();
            axis.MarkHomed(0);
        }
        machine.SafetyState.EtherCatOnline = true;
        machine.SafetyState.EmergencyStopPressed = false;

        // Act
        machine.UpdateSafetyState();
        var (isSatisfied, reason) = SystemReadySpecification.Check(machine);

        // Assert
        Assert.True(isSatisfied);
        Assert.Null(reason);
    }

    [Fact]
    public void SystemReady_WhenEmergencyStop_ShouldNotSatisfy()
    {
        // Arrange
        var machine = new Machine();
        machine.SafetyState.EmergencyStopPressed = true;

        // Act
        var (isSatisfied, reason) = SystemReadySpecification.Check(machine);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("急停", reason);
    }

    [Fact]
    public void SystemReady_WhenEtherCatOffline_ShouldNotSatisfy()
    {
        // Arrange
        var machine = new Machine();
        machine.SafetyState.EtherCatOnline = false;

        // Act
        var (isSatisfied, reason) = SystemReadySpecification.Check(machine);

        // Assert
        Assert.False(isSatisfied);
        Assert.Contains("EtherCAT", reason);
    }
}
