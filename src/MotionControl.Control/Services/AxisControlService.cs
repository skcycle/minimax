using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Specifications;
using MotionControl.Domain.ValueObjects;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Control.Services;

/// <summary>
/// 轴控制服务实现
/// </summary>
public class AxisControlService : IAxisControlService
{
    private readonly IMotionController _controller;
    private readonly ISafetyInterlockService _safetyService;
    private readonly ILogger _logger;

    public AxisControlService(
        IMotionController controller,
        ISafetyInterlockService safetyService,
        ILogger logger)
    {
        _controller = controller;
        _safetyService = safetyService;
        _logger = logger;
    }

    public async Task<bool> EnableAsync(Axis axis, CancellationToken ct = default)
    {
        // 安全联锁检查
        if (!_safetyService.CanEnableAxis(axis))
        {
            _logger.Warning($"Cannot enable axis {axis.Id}: safety interlock");
            return false;
        }

        var result = await _controller.EnableAxisAsync(axis.ControllerAxisNo, ct);
        if (result.Success)
        {
            axis.ServoState = ServoState.Enabled;
            _logger.Info($"Axis {axis.Id} enabled");
        }
        else
        {
            _logger.Error($"Failed to enable axis {axis.Id}: {result.ErrorMessage}");
        }
        return result.Success;
    }

    public async Task<bool> DisableAsync(Axis axis, CancellationToken ct = default)
    {
        var result = await _controller.DisableAxisAsync(axis.ControllerAxisNo, ct);
        if (result.Success)
        {
            axis.ServoState = ServoState.Disabled;
            _logger.Info($"Axis {axis.Id} disabled");
        }
        return result.Success;
    }

    public async Task<bool> MoveAbsAsync(Axis axis, Position target, Velocity velocity, CancellationToken ct = default)
    {
        // 安全检查
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: {reason}");
            return false;
        }

        // 软限位检查
        if (!axis.IsWithinSoftLimits(target.Value))
        {
            _logger.Warning($"Cannot move axis {axis.Id}: target {target} exceeds soft limits");
            return false;
        }

        var command = new AxisMoveCommand(
            axis.ControllerAxisNo,
            target.Value,
            velocity.Value,
            axis.Parameters.DefaultAccel,
            axis.Parameters.DefaultDecel
        );

        var result = await _controller.MoveAbsoluteAsync(axis.ControllerAxisNo, command, ct);
        if (result.Success)
        {
            axis.TargetPosition = target.Value;
            axis.State = AxisState.Moving;
            _logger.Debug($"Axis {axis.Id} move to {target} at {velocity}");
        }
        return result.Success;
    }

    public async Task<bool> MoveRelAsync(Axis axis, double distance, Velocity velocity, CancellationToken ct = default)
    {
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: {reason}");
            return false;
        }

        var command = new AxisMoveCommand(
            axis.ControllerAxisNo,
            distance,
            velocity.Value,
            axis.Parameters.DefaultAccel,
            axis.Parameters.DefaultDecel
        );

        var result = await _controller.MoveRelativeAsync(axis.ControllerAxisNo, command, ct);
        if (result.Success)
        {
            axis.TargetPosition = axis.CurrentPosition + distance;
            axis.State = AxisState.Moving;
        }
        return result.Success;
    }

    public async Task<bool> JogAsync(Axis axis, Velocity velocity, CancellationToken ct = default)
    {
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            _logger.Warning($"Cannot jog axis {axis.Id}: {reason}");
            return false;
        }

        var result = await _controller.JogAsync(axis.ControllerAxisNo, velocity.Value, ct);
        if (result.Success)
        {
            axis.State = AxisState.Jogging;
        }
        return result.Success;
    }

    public async Task<bool> StopAsync(Axis axis, CancellationToken ct = default)
    {
        var result = await _controller.StopAxisAsync(axis.ControllerAxisNo, ct);
        if (result.Success)
        {
            axis.State = AxisState.Stopping;
        }
        return result.Success;
    }

    public async Task<bool> HomeAsync(Axis axis, HomeProfile profile, CancellationToken ct = default)
    {
        var (canHome, reason) = AxisCanHomeSpecification.Check(axis);
        if (!canHome)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: {reason}");
            return false;
        }

        // 使用控制器HomeProfile
        var zmcProfile = new Device.Abstractions.Controllers.HomeProfile(
            profile.SearchSpeed,
            profile.LatchSpeed,
            profile.Accel,
            profile.HomeMode,
            profile.HomeDirection
        );

        var result = await _controller.HomeAxisAsync(axis.ControllerAxisNo, zmcProfile, ct);
        if (result.Success)
        {
            axis.State = AxisState.Homing;
            axis.HomeState = HomeState.Homing;
        }
        return result.Success;
    }

    public async Task<bool> ResetAlarmAsync(Axis axis, CancellationToken ct = default)
    {
        var result = await _controller.ResetAxisAlarmAsync(axis.ControllerAxisNo, ct);
        if (result.Success)
        {
            axis.ClearAlarm();
        }
        return result.Success;
    }
}
