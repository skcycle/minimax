using MotionControl.Domain.Enums;
using MotionControl.Domain.Interfaces;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Application.Services;

/// <summary>
/// 系统应用服务实现
/// </summary>
public class SystemAppService : ISystemAppService
{
    private readonly IMotionController _controller;
    private readonly IMachineRepository _machineRepository;
    private readonly IStatePollingService _pollingService;
    private readonly ILogger _logger;

    public SystemAppService(
        IMotionController controller,
        IMachineRepository machineRepository,
        IStatePollingService pollingService,
        ILogger logger)
    {
        _controller = controller;
        _machineRepository = machineRepository;
        _pollingService = pollingService;
        _logger = logger;
    }

    public SystemStatusDto GetSystemStatus()
    {
        var machine = _machineRepository.GetMachine();
        machine.UpdateSafetyState();

        return new SystemStatusDto(
            CurrentState: machine.CurrentState,
            IsConnected: _controller.IsConnected,
            EtherCatOnline: machine.SafetyState.EtherCatOnline,
            TotalAxes: machine.Axes.Count,
            HomedAxes: machine.Axes.Count(a => a.IsHomed),
            EnabledAxes: machine.Axes.Count(a => a.IsEnabled),
            ActiveAlarms: machine.ActiveAlarms.Count,
            IsSafeForOperation: machine.SafetyState.IsSafeForOperation
        );
    }

    public async Task<CommandResultDto> ConnectAsync(string ip, int port = 5005, CancellationToken ct = default)
    {
        try
        {
            _logger.Info($"Connecting to controller at {ip}:{port}...");

            var success = await _controller.ConnectAsync(ip, port, ct);
            if (success)
            {
                var machine = _machineRepository.GetMachine();
                machine.CurrentState = MachineState.Initializing;

                // 启动状态轮询
                _pollingService.Start();

                machine.CurrentState = MachineState.Idle;
                _logger.Info("Controller connected successfully");

                return CommandResultDto.Ok();
            }
            else
            {
                return CommandResultDto.Fail(-1, "Failed to connect to controller");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Connection error: {ex.Message}", ex);
            return CommandResultDto.Fail(-1, ex.Message);
        }
    }

    public async Task<CommandResultDto> DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _pollingService.Stop();
            await _controller.DisconnectAsync();

            var machine = _machineRepository.GetMachine();
            machine.CurrentState = MachineState.PowerOff;

            _logger.Info("Controller disconnected");
            return CommandResultDto.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error($"Disconnect error: {ex.Message}", ex);
            return CommandResultDto.Fail(-1, ex.Message);
        }
    }

    public async Task<CommandResultDto> InitializeAsync(CancellationToken ct = default)
    {
        var machine = _machineRepository.GetMachine();

        try
        {
            machine.CurrentState = MachineState.Initializing;

            // 检查连接
            if (!_controller.IsConnected)
            {
                return CommandResultDto.Fail(-1, "Controller not connected");
            }

            // 获取控制器信息
            var info = await _controller.GetControllerInfoAsync(ct);
            _logger.Info($"Controller info: {info.Model}, {info.FirmwareVersion}");

            machine.CurrentState = MachineState.Idle;
            return CommandResultDto.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error($"Initialization error: {ex.Message}", ex);
            machine.CurrentState = MachineState.Alarm;
            return CommandResultDto.Fail(-1, ex.Message);
        }
    }

    public void EnterManualMode()
    {
        var machine = _machineRepository.GetMachine();

        if (machine.CurrentState != MachineState.Ready &&
            machine.CurrentState != MachineState.Idle)
        {
            _logger.Warning($"Cannot enter manual mode from state: {machine.CurrentState}");
            return;
        }

        machine.CurrentState = MachineState.Manual;
        _logger.Info("Entered manual mode");
    }

    public void EnterAutoMode()
    {
        var machine = _machineRepository.GetMachine();

        // 检查是否满足自动模式条件
        if (!machine.SafetyState.IsSafeForOperation)
        {
            _logger.Warning("Cannot enter auto mode: safety conditions not met");
            return;
        }

        machine.CurrentState = MachineState.Auto;
        _logger.Info("Entered auto mode");
    }

    public void ExitAutoMode()
    {
        var machine = _machineRepository.GetMachine();

        if (machine.CurrentState != MachineState.Auto &&
            machine.CurrentState != MachineState.Paused)
        {
            return;
        }

        machine.CurrentState = MachineState.Ready;
        _logger.Info("Exited auto mode");
    }
}
