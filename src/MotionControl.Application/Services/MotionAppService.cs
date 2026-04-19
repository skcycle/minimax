using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Interfaces;
using MotionControl.Domain.Specifications;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Application.Services;

/// <summary>
/// 运动应用服务实现
/// </summary>
public class MotionAppService : IMotionAppService
{
    private readonly IMotionController _controller;
    private readonly IMachineRepository _machineRepository;
    private readonly ILogger _logger;

    // 默认加减速度
    private const double DefaultAccel = 5000;
    private const double DefaultDecel = 5000;

    public MotionAppService(
        IMotionController controller,
        IMachineRepository machineRepository,
        ILogger logger)
    {
        _controller = controller;
        _machineRepository = machineRepository;
        _logger = logger;
    }

    public async Task<CommandResultDto> MoveAbsoluteAsync(
        int axisNumber,
        double position,
        double velocity,
        double? accel = null,
        double? decel = null,
        CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        // 安全检查
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            return CommandResultDto.Fail(-1, reason!);
        }

        // 软限位检查
        if (!axis.IsWithinSoftLimits(position))
        {
            return CommandResultDto.Fail(-1, $"Position {position} exceeds soft limits [{axis.Parameters.SoftwareLowLimit}, {axis.Parameters.SoftwareHighLimit}]");
        }

        var command = new AxisMoveCommand(
            AxisNumber: axisNumber,
            Position: position,
            Velocity: velocity,
            Acceleration: accel ?? axis.Parameters.DefaultAccel,
            Deceleration: decel ?? axis.Parameters.DefaultDecel
        );

        var result = await _controller.MoveAbsoluteAsync(axisNumber, command, ct);
        if (result.Success)
        {
            axis.TargetPosition = position;
            axis.State = AxisState.Moving;
            _logger.Info($"Axis {axisNumber} move to {position} at {velocity}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> MoveRelativeAsync(
        int axisNumber,
        double distance,
        double velocity,
        double? accel = null,
        double? decel = null,
        CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        // 安全检查
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            return CommandResultDto.Fail(-1, reason!);
        }

        var targetPos = axis.CurrentPosition + distance;

        // 软限位检查
        if (!axis.IsWithinSoftLimits(targetPos))
        {
            return CommandResultDto.Fail(-1, $"Target position {targetPos} exceeds soft limits");
        }

        var command = new AxisMoveCommand(
            AxisNumber: axisNumber,
            Position: distance, // 相对运动的distance
            Velocity: velocity,
            Acceleration: accel ?? axis.Parameters.DefaultAccel,
            Deceleration: decel ?? axis.Parameters.DefaultDecel
        );

        var result = await _controller.MoveRelativeAsync(axisNumber, command, ct);
        if (result.Success)
        {
            axis.TargetPosition = targetPos;
            axis.State = AxisState.Moving;
            _logger.Info($"Axis {axisNumber} move relative {distance} at {velocity}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> JogAsync(int axisNumber, double velocity, CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        // 安全检查
        var (canMove, reason) = AxisCanMoveSpecification.Check(axis);
        if (!canMove)
        {
            return CommandResultDto.Fail(-1, reason!);
        }

        var result = await _controller.JogAsync(axisNumber, velocity, ct);
        if (result.Success)
        {
            axis.State = AxisState.Jogging;
            _logger.Info($"Axis {axisNumber} jog at {velocity}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> StopAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        var result = await _controller.StopAxisAsync(axisNumber, ct);
        if (result.Success)
        {
            axis.State = AxisState.Stopping;
            _logger.Info($"Axis {axisNumber} stop requested");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> EmergencyStopAsync(CancellationToken ct = default)
    {
        var result = await _controller.EmergencyStopAsync(ct);
        if (result.Success)
        {
            var machine = _machineRepository.GetMachine();
            foreach (var axis in machine.Axes)
            {
                axis.State = AxisState.Stopping;
            }
            _logger.Warning("Emergency stop executed");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> StopAllAsync(CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var results = new List<CommandResultDto>();

        foreach (var axis in machine.Axes)
        {
            var result = await _controller.StopAxisAsync(axis.Id.Value, ct);
            results.Add(new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage));
            axis.State = AxisState.Stopping;
        }

        var allSuccess = results.All(r => r.Success);
        return allSuccess
            ? CommandResultDto.Ok()
            : CommandResultDto.Fail(-1, "Some axes failed to stop");
    }
}
