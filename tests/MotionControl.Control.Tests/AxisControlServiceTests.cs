using Moq;
using MotionControl.Control.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.ValueObjects;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Control.Tests;

/// <summary>
/// AxisControlService单元测试
/// </summary>
public class AxisControlServiceTests
{
    private readonly Mock<IMotionController> _mockController;
    private readonly Mock<ISafetyInterlockService> _mockSafetyService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly AxisControlService _service;

    public AxisControlServiceTests()
    {
        _mockController = new Mock<IMotionController>();
        _mockSafetyService = new Mock<ISafetyInterlockService>();
        _mockLogger = new Mock<ILogger>();
        _service = new AxisControlService(_mockController.Object, _mockSafetyService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task EnableAsync_WhenSafetyAllows_ShouldEnableAxis()
    {
        // Arrange
        var axis = new Axis(0);
        _mockSafetyService.Setup(s => s.CanEnableAxis(axis)).Returns(true);
        _mockController.Setup(c => c.EnableAxisAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Act
        var result = await _service.EnableAsync(axis);

        // Assert
        Assert.True(result);
        Assert.Equal(ServoState.Enabled, axis.ServoState);
    }

    [Fact]
    public async Task EnableAsync_WhenSafetyBlocks_ShouldNotCallController()
    {
        // Arrange
        var axis = new Axis(0);
        _mockSafetyService.Setup(s => s.CanEnableAxis(axis)).Returns(false);

        // Act
        var result = await _service.EnableAsync(axis);

        // Assert
        Assert.False(result);
        _mockController.Verify(c => c.EnableAxisAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisableAsync_ShouldCallController()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        _mockController.Setup(c => c.DisableAxisAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Act
        var result = await _service.DisableAsync(axis);

        // Assert
        Assert.True(result);
        Assert.Equal(ServoState.Disabled, axis.ServoState);
    }

    [Fact]
    public async Task MoveAbsAsync_WhenCanMove_ShouldCallController()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);
        _mockSafetyService.Setup(s => s.CanMoveAxis(axis)).Returns(true);
        _mockController.Setup(c => c.MoveAbsoluteAsync(0, It.IsAny<AxisMoveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Act
        var result = await _service.MoveAbsAsync(axis, new Position(100), new Velocity(1000));

        // Assert
        Assert.True(result);
        Assert.Equal(AxisState.Moving, axis.State);
    }

    [Fact]
    public async Task MoveAbsAsync_WhenNotCanMove_ShouldNotCallController()
    {
        // Arrange
        var axis = new Axis(0);
        _mockSafetyService.Setup(s => s.CanMoveAxis(axis)).Returns(false);

        // Act
        var result = await _service.MoveAbsAsync(axis, new Position(100), new Velocity(1000));

        // Assert
        Assert.False(result);
        _mockController.Verify(c => c.MoveAbsoluteAsync(It.IsAny<int>(), It.IsAny<AxisMoveCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ShouldCallController()
    {
        // Arrange
        var axis = new Axis(0);
        axis.State = AxisState.Moving;
        _mockController.Setup(c => c.StopAxisAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Act
        var result = await _service.StopAsync(axis);

        // Assert
        Assert.True(result);
        Assert.Equal(AxisState.Stopping, axis.State);
    }

    [Fact]
    public async Task ResetAlarmAsync_WhenSuccess_ShouldClearAlarm()
    {
        // Arrange
        var axis = new Axis(0);
        axis.SetAlarm(1, "Test");
        _mockController.Setup(c => c.ResetAxisAlarmAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Act
        var result = await _service.ResetAlarmAsync(axis);

        // Assert
        Assert.True(result);
        Assert.False(axis.HasAlarm);
    }
}

/// <summary>
/// SafetyInterlockService单元测试
/// </summary>
public class SafetyInterlockServiceTests
{
    private readonly Mock<IMotionController> _mockController;
    private readonly Mock<ILogger> _mockLogger;
    private readonly SafetyInterlockService _service;

    public SafetyInterlockServiceTests()
    {
        _mockController = new Mock<IMotionController>();
        _mockLogger = new Mock<ILogger>();
        _service = new SafetyInterlockService(_mockController.Object, _mockLogger.Object);
    }

    [Fact]
    public void CanEnableAxis_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var axis = new Axis(0);
        _mockController.Setup(c => c.IsConnected).Returns(false);

        // Act
        var result = _service.CanEnableAxis(axis);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanEnableAxis_WhenConnectedNoAlarm_ShouldReturnTrue()
    {
        // Arrange
        var axis = new Axis(0);
        _mockController.Setup(c => c.IsConnected).Returns(true);

        // Act
        var result = _service.CanEnableAxis(axis);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanMoveAxis_WhenNotHomed_ShouldReturnFalse()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        _mockController.Setup(c => c.IsConnected).Returns(true);

        // Act
        var result = _service.CanMoveAxis(axis);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanMoveAxis_WhenAllConditionsMet_ShouldReturnTrue()
    {
        // Arrange
        var axis = new Axis(0);
        axis.Enable();
        axis.MarkHomed(0);
        _mockController.Setup(c => c.IsConnected).Returns(true);

        // Act
        var result = _service.CanMoveAxis(axis);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetInterlockReason_WhenNotConnected_ShouldReturnReason()
    {
        // Arrange
        var axis = new Axis(0);
        _mockController.Setup(c => c.IsConnected).Returns(false);

        // Act
        var reason = _service.GetInterlockReason(axis);

        // Assert
        Assert.Contains("未连接", reason);
    }

    [Fact]
    public async Task EmergencyStopAsync_ShouldCallController()
    {
        // Act
        await _service.EmergencyStopAsync();

        // Assert
        _mockController.Verify(c => c.EmergencyStopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
