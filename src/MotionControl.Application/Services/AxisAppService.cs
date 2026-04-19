using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Interfaces;
using MotionControl.Domain.Specifications;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Application.Services;

/// <summary>
/// 轴应用服务实现
/// </summary>
public class AxisAppService : IAxisAppService
{
    private readonly IMotionController _controller;
    private readonly IMachineRepository _machineRepository;
    private readonly ILogger _logger;

    public AxisAppService(
        IMotionController controller,
        IMachineRepository machineRepository,
        ILogger logger)
    {
        _controller = controller;
        _machineRepository = machineRepository;
        _logger = logger;
    }

    public AxisStatusDto? GetAxisStatus(int axisNumber)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null) return null;

        return MapToDto(axis);
    }

    public IReadOnlyList<AxisStatusDto> GetAllAxesStatus()
    {
        var machine = _machineRepository.GetMachine();
        var result = new List<AxisStatusDto>();

        foreach (var axis in machine.Axes)
        {
            result.Add(MapToDto(axis));
        }

        return result;
    }

    public async Task<CommandResultDto> EnableAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        // 安全检查
        if (!machine.SafetyState.CanEnableAxes)
        {
            return CommandResultDto.Fail(-1, "Cannot enable axis: safety condition not met");
        }

        var result = await _controller.EnableAxisAsync(axisNumber, ct);
        if (result.Success)
        {
            axis.ServoState = ServoState.Enabled;
            _logger.Info($"Axis {axisNumber} enabled");
        }
        else
        {
            _logger.Error($"Failed to enable axis {axisNumber}: {result.ErrorMessage}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> DisableAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        var result = await _controller.DisableAxisAsync(axisNumber, ct);
        if (result.Success)
        {
            axis.ServoState = ServoState.Disabled;
            _logger.Info($"Axis {axisNumber} disabled");
        }
        else
        {
            _logger.Error($"Failed to disable axis {axisNumber}: {result.ErrorMessage}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> EnableAllAxesAsync(CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var results = new List<CommandResultDto>();

        foreach (var axis in machine.Axes)
        {
            var result = await EnableAxisAsync(axis.Id.Value, ct);
            results.Add(result);
        }

        var allSuccess = results.All(r => r.Success);
        return allSuccess
            ? CommandResultDto.Ok()
            : CommandResultDto.Fail(-1, "Some axes failed to enable");
    }

    public async Task<CommandResultDto> DisableAllAxesAsync(CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var results = new List<CommandResultDto>();

        foreach (var axis in machine.Axes)
        {
            var result = await DisableAxisAsync(axis.Id.Value, ct);
            results.Add(result);
        }

        var allSuccess = results.All(r => r.Success);
        return allSuccess
            ? CommandResultDto.Ok()
            : CommandResultDto.Fail(-1, "Some axes failed to disable");
    }

    private static AxisStatusDto MapToDto(Axis axis)
    {
        return new AxisStatusDto(
            AxisNumber: axis.Id.Value,
            Name: axis.Name,
            CurrentPosition: axis.CurrentPosition,
            CurrentVelocity: axis.CurrentVelocity,
            TargetPosition: axis.TargetPosition,
            State: axis.State.ToString(),
            ServoState: axis.ServoState.ToString(),
            IsHomed: axis.IsHomed,
            HasAlarm: axis.HasAlarm,
            PositiveLimit: axis.PositiveLimitTriggered,
            NegativeLimit: axis.NegativeLimitTriggered,
            AlarmCode: axis.CurrentAlarm.Code
        );
    }
}
