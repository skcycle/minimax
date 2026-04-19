using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Interfaces;
using MotionControl.Domain.Specifications;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Application.Services;

/// <summary>
/// 回零应用服务实现
/// </summary>
public class HomingAppService : IHomingAppService
{
    private readonly IMotionController _controller;
    private readonly IMachineRepository _machineRepository;
    private readonly ILogger _logger;

    public HomingAppService(
        IMotionController controller,
        IMachineRepository machineRepository,
        ILogger logger)
    {
        _controller = controller;
        _machineRepository = machineRepository;
        _logger = logger;
    }

    public async Task<CommandResultDto> HomeAxisAsync(int axisNumber, HomeProfile profile, CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null)
        {
            return CommandResultDto.Fail(-1, $"Axis {axisNumber} not found");
        }

        // 安全检查
        var (canHome, reason) = AxisCanHomeSpecification.Check(axis);
        if (!canHome)
        {
            return CommandResultDto.Fail(-1, reason!);
        }

        // 使用HomeProfile (已在Device.Abstractions.Controllers中定义)
        var zmcProfile = new HomeProfile(
            profile.SearchSpeed,
            profile.LatchSpeed,
            profile.Accel,
            profile.HomeMode,
            profile.HomeDirection
        );

        var result = await _controller.HomeAxisAsync(axisNumber, zmcProfile, ct);
        if (result.Success)
        {
            axis.State = AxisState.Homing;
            axis.HomeState = HomeState.Homing;
            _logger.Info($"Axis {axisNumber} homing started, mode: {profile.HomeMode}");
        }
        else
        {
            _logger.Error($"Failed to home axis {axisNumber}: {result.ErrorMessage}");
        }

        return new CommandResultDto(result.Success, result.ErrorCode, result.ErrorMessage);
    }

    public async Task<CommandResultDto> HomeAllAxesAsync(CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();

        // 使用默认回零参数
        var defaultProfile = new HomeProfile(
            SearchSpeed: 100,
            LatchSpeed: 10,
            Accel: 5000,
            HomeMode: 0,
            HomeDirection: 0
        );

        var results = new List<CommandResultDto>();

        foreach (var axis in machine.Axes)
        {
            var result = await HomeAxisAsync(axis.Id.Value, defaultProfile, ct);
            results.Add(result);
        }

        var allSuccess = results.All(r => r.Success);
        return allSuccess
            ? CommandResultDto.Ok()
            : CommandResultDto.Fail(-1, "Some axes failed to home");
    }

    public HomeStatusDto? GetHomeStatus(int axisNumber)
    {
        var machine = _machineRepository.GetMachine();
        var axis = machine.GetAxis(axisNumber);
        if (axis == null) return null;

        return new HomeStatusDto(
            AxisNumber: axisNumber,
            IsHomed: axis.IsHomed,
            HomeState: axis.HomeState.ToString(),
            HomePosition: axis.CurrentPosition
        );
    }
}
