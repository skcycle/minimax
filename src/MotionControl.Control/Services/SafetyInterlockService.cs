using MotionControl.Domain.Entities;
using MotionControl.Domain.Enums;
using MotionControl.Domain.Specifications;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Control.Services;

/// <summary>
/// 安全联锁服务实现
/// </summary>
public class SafetyInterlockService : ISafetyInterlockService
{
    private readonly IMotionController _controller;
    private readonly ILogger _logger;

    public SafetyInterlockService(IMotionController controller, ILogger logger)
    {
        _controller = controller;
        _logger = logger;
    }

    public bool CanEnableAxis(Axis axis)
    {
        // 检查急停
        if (!_controller.IsConnected)
        {
            _logger.Warning($"Cannot enable axis {axis.Id}: controller not connected");
            return false;
        }

        if (axis.HasAlarm)
        {
            _logger.Warning($"Cannot enable axis {axis.Id}: axis has alarm");
            return false;
        }

        return true;
    }

    public bool CanMoveAxis(Axis axis)
    {
        if (!_controller.IsConnected)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: controller not connected");
            return false;
        }

        if (axis.ServoState != ServoState.Enabled)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: servo not enabled");
            return false;
        }

        if (!axis.IsHomed)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: not homed");
            return false;
        }

        if (axis.HasAlarm)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: has alarm");
            return false;
        }

        if (axis.PositiveLimitTriggered)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: positive limit triggered");
            return false;
        }

        if (axis.NegativeLimitTriggered)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: negative limit triggered");
            return false;
        }

        if (axis.State == AxisState.Moving)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: already moving");
            return false;
        }

        if (axis.State == AxisState.Homing)
        {
            _logger.Warning($"Cannot move axis {axis.Id}: homing in progress");
            return false;
        }

        return true;
    }

    public bool CanHomeAxis(Axis axis)
    {
        if (!_controller.IsConnected)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: controller not connected");
            return false;
        }

        if (axis.ServoState != ServoState.Enabled)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: servo not enabled");
            return false;
        }

        if (axis.HasAlarm)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: has alarm");
            return false;
        }

        if (axis.State == AxisState.Moving)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: moving");
            return false;
        }

        if (axis.State == AxisState.Homing)
        {
            _logger.Warning($"Cannot home axis {axis.Id}: already homing");
            return false;
        }

        return true;
    }

    public bool CanStartAutoRun(Machine machine)
    {
        // 检查EtherCAT在线
        if (!machine.SafetyState.EtherCatOnline)
        {
            _logger.Warning("Cannot start auto run: EtherCAT offline");
            return false;
        }

        // 检查急停
        if (machine.SafetyState.EmergencyStopPressed)
        {
            _logger.Warning("Cannot start auto run: emergency stop pressed");
            return false;
        }

        // 检查报警
        if (!machine.SafetyState.NoActiveAlarms)
        {
            _logger.Warning("Cannot start auto run: active alarms exist");
            return false;
        }

        // 检查所有轴已回零
        if (!machine.SafetyState.AllAxesHomed)
        {
            _logger.Warning("Cannot start auto run: not all axes homed");
            return false;
        }

        // 检查所有轴已使能
        if (!machine.SafetyState.AllAxesEnabled)
        {
            _logger.Warning("Cannot start auto run: not all axes enabled");
            return false;
        }

        return true;
    }

    public async Task EmergencyStopAsync()
    {
        _logger.Warning("Emergency stop triggered");
        await _controller.EmergencyStopAsync();
    }

    public string? GetInterlockReason(Axis axis)
    {
        if (!_controller.IsConnected)
            return "控制器未连接";
        if (!axis.IsEnabled)
            return "轴未使能";
        if (!axis.IsHomed)
            return "轴未回零";
        if (axis.HasAlarm)
            return $"轴报警: {axis.CurrentAlarm}";
        if (axis.PositiveLimitTriggered)
            return "正向限位触发";
        if (axis.NegativeLimitTriggered)
            return "负向限位触发";
        if (axis.IsMoving)
            return "轴正在运动";
        return null;
    }
}
