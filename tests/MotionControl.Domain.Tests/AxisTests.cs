using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;

namespace MotionControl.Domain.Tests;

/// <summary>
/// Axis实体单元测试
/// </summary>
public class AxisTests
{
    [Fact]
    public void Axis_Construction_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var axis = new Axis(0);

        // Assert
        Assert.Equal(0, axis.Id.Value);
        Assert.Equal(0, axis.ControllerAxisNo);
        Assert.Equal(AxisState.Disabled, axis.State);
        Assert.Equal(ServoState.Disabled, axis.ServoState);
        Assert.Equal(HomeState.NotHomed, axis.HomeState);
        Assert.False(axis.IsEnabled);
        Assert.False(axis.IsHomed);
        Assert.False(axis.HasAlarm);
    }

    [Fact]
    public void Axis_Enable_ShouldChangeServoState()
    {
        // Arrange
        var axis = new Axis(0);

        // Act
        axis.Enable();

        // Assert
        Assert.Equal(ServoState.Enabled, axis.ServoState);
        Assert.True(axis.IsEnabled);
    }

    [Fact]
    public void Axis_Disable_ShouldChangeServoState()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();

        // Act
        axis.Disable();

        // Assert
        Assert.Equal(ServoState.Disabled, axis.ServoState);
        Assert.False(axis.IsEnabled);
    }

    [Fact]
    public void Axis_MarkHomed_ShouldSetHomeState()
    {
        // Arrange
        var axis = new Axis(0);

        // Act
        axis.MarkHomed(100.0);

        // Assert
        Assert.Equal(HomeState.Homed, axis.HomeState);
        Assert.True(axis.IsHomed);
        Assert.Equal(100.0, axis.CurrentPosition);
    }

    [Fact]
    public void Axis_SetAlarm_ShouldSetAlarmAndChangeState()
    {
        // Arrange
        var axis = new Axis(0);

        // Act
        axis.SetAlarm(1, "Test alarm");

        // Assert
        Assert.True(axis.HasAlarm);
        Assert.Equal(AxisState.Alarm, axis.State);
        Assert.Equal(1, axis.CurrentAlarm.Code);
    }

    [Fact]
    public void Axis_ClearAlarm_ShouldClearAlarmState()
    {
        // Arrange
        var axis = new Axis(0);
        axis.SetAlarm(1, "Test alarm");

        // Act
        axis.ClearAlarm();

        // Assert
        Assert.False(axis.HasAlarm);
        Assert.Equal(0, axis.CurrentAlarm.Code);
    }

    [Fact]
    public void Axis_CanMove_WhenConditionsMet_ShouldReturnTrue()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);

        // Act
        var canMove = axis.CanMove();

        // Assert
        Assert.True(canMove);
    }

    [Fact]
    public void Axis_CanMove_WhenNotEnabled_ShouldReturnFalse()
    {
        // Arrange
        var axis = new Axis(0);
        axis.MarkHomed(0);

        // Act
        var canMove = axis.CanMove();

        // Assert
        Assert.False(canMove);
    }

    [Fact]
    public void Axis_CanMove_WhenNotHomed_ShouldReturnFalse()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();

        // Act
        var canMove = axis.CanMove();

        // Assert
        Assert.False(canMove);
    }

    [Fact]
    public void Axis_CanMove_WhenHasAlarm_ShouldReturnFalse()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);
        axis.SetAlarm(1, "Alarm");

        // Act
        var canMove = axis.CanMove();

        // Assert
        Assert.False(canMove);
    }

    [Fact]
    public void Axis_UpdateFeedback_ShouldUpdatePositionAndStatus()
    {
        // Arrange
        var axis = new Axis(0);
        uint statusWord = 0x0003; // bit 0 = enabled, bit 1 = homed

        // Act
        axis.UpdateFeedback(150.5, 100.0, statusWord);

        // Assert
        Assert.Equal(150.5, axis.CurrentPosition);
        Assert.Equal(100.0, axis.CurrentVelocity);
    }

    [Fact]
    public void Axis_PositionChanged_Event_ShouldFire()
    {
        // Arrange
        var axis = new Axis(0);
        double oldPos = 0;
        double newPos = 100;
        axis.PositionChanged += (s, e) => { oldPos = e.Old; newPos = e.New; };

        // Act
        axis.CurrentPosition = 100;

        // Assert
        Assert.Equal(100, newPos);
    }

    [Fact]
    public void Axis_IsWithinSoftLimits_ShouldCheckCorrectly()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Parameters.SoftwareLowLimit = -1000;
        axis.Parameters.SoftwareHighLimit = 1000;

        // Act & Assert
        Assert.True(axis.IsWithinSoftLimits(0));
        Assert.True(axis.IsWithinSoftLimits(-500));
        Assert.True(axis.IsWithinSoftLimits(500));
        Assert.False(axis.IsWithinSoftLimits(-1500));
        Assert.False(axis.IsWithinSoftLimits(1500));
    }
}
