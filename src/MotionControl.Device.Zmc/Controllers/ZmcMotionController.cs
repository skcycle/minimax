using System.Text;
using cszmcaux;
using MotionControl.Contracts.Constants;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Device.Zmc.Controllers;

/// <summary>
/// ZMC432控制器实现
/// 使用正运动ZAux DLL进行通信
/// </summary>
public class ZmcMotionController : IMotionController
{
    private readonly ILogger _logger;
    private IntPtr _handle;
    private bool _disposed;
    private readonly object _lock = new();

    public bool IsConnected => State == ControllerState.Connected && _handle != IntPtr.Zero;
    public ControllerState State { get; private set; } = ControllerState.Disconnected;

    public event EventHandler<ControllerState>? ConnectionStateChanged;

    public ZmcMotionController(ILogger logger)
    {
        _logger = logger;
        _handle = IntPtr.Zero;
    }

    public async Task<bool> ConnectAsync(string ip, int port = 5005, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (IsConnected)
                    {
                        _logger.Warning("Already connected to ZMC controller");
                        return true;
                    }

                    State = ControllerState.Connecting;
                    ConnectionStateChanged?.Invoke(this, State);

                    var ret = zmcaux.ZAux_OpenEth(ip, out _handle);
                    if (ret != 0)
                    {
                        _logger.Error($"Failed to connect to ZMC at {ip}:{port}, error code: {ret}");
                        State = ControllerState.Error;
                        ConnectionStateChanged?.Invoke(this, State);
                        return false;
                    }

                    State = ControllerState.Connected;
                    ConnectionStateChanged?.Invoke(this, State);
                    _logger.Info($"Connected to ZMC controller at {ip}:{port}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception connecting to ZMC: {ex.Message}", ex);
                    State = ControllerState.Error;
                    ConnectionStateChanged?.Invoke(this, State);
                    return false;
                }
            }
        }, ct);
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_handle != IntPtr.Zero)
                {
                    var ret = zmcaux.ZAux_Close(_handle);
                    if (ret == 0)
                    {
                        _logger.Info("Disconnected from ZMC controller");
                    }
                    else
                    {
                        _logger.Warning($"Error disconnecting from ZMC: {ret}");
                    }
                    _handle = IntPtr.Zero;
                }
                State = ControllerState.Disconnected;
                ConnectionStateChanged?.Invoke(this, State);
            }
        });
    }

    public Task<ControllerInfo> GetControllerInfoAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var info = new ControllerInfo(
                    Model: "ZMC432",
                    SerialNumber: "Unknown",
                    FirmwareVersion: "Unknown",
                    AxisCount: 32
                );

                // 尝试通过命令获取控制器信息
                try
                {
                    var response = new StringBuilder(256);
                    var ret = zmcaux.ZAux_DirectCommand(_handle, "?VERSION", response, 256);
                    if (ret == 0 && response.Length > 0)
                    {
                        info = info with { FirmwareVersion = response.ToString() };
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Could not get controller info: {ex.Message}");
                }

                return info;
            }
        }, ct);
    }

    public Task<AxisFeedback> GetAxisFeedbackAsync(int axisNumber, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var feedback = new AxisFeedback(
                    AxisNumber: axisNumber,
                    Position: 0,
                    Velocity: 0,
                    StatusWord: 0,
                    IsEnabled: false,
                    IsHomed: false,
                    HasAlarm: false,
                    AlarmCode: 0
                );

                try
                {
                    // 获取命令位置
                    float dpos = 0;
                    zmcaux.ZAux_Direct_GetDpos(_handle, axisNumber, ref dpos);
                    feedback = feedback with { Position = dpos };

                    // 获取反馈位置
                    float mpos = 0;
                    zmcaux.ZAux_Direct_GetEncoder(_handle, axisNumber, ref mpos);

                    // 获取速度
                    float speed = 0;
                    zmcaux.ZAux_Direct_GetMspeed(_handle, axisNumber, ref speed);
                    feedback = feedback with { Velocity = speed };

                    // 获取轴状态
                    int axisStatus = 0;
                    zmcaux.ZAux_Direct_GetAxisStatus(_handle, axisNumber, ref axisStatus);
                    feedback = feedback with { StatusWord = (uint)axisStatus };

                    // 判断使能状态 - 通过WDOG参数
                    float wdog = 0;
                    zmcaux.ZAux_Direct_GetParam(_handle, "WDOG", axisNumber, ref wdog);
                    feedback = feedback with { IsEnabled = wdog == 1 };

                    // 判断回零状态 - 通过检查原点信号或HOME_STATUS
                    uint homeStatus = 0;
                    zmcaux.ZAux_Direct_GetHomeStatus(_handle, axisNumber, ref homeStatus);
                    bool isHomed = (homeStatus & 0x01) != 0;  // bit 0 表示已回零
                    feedback = feedback with { IsHomed = isHomed };

                    // 判断报警 - 通过AXISSTATUS的ALM位
                    bool hasAlarm = (axisStatus & 0x00002) != 0;  // bit 1 = alarm
                    int alarmCode = hasAlarm ? 1 : 0;
                    feedback = feedback with { HasAlarm = hasAlarm, AlarmCode = alarmCode };
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error getting axis {axisNumber} feedback: {ex.Message}", ex);
                }

                return feedback;
            }
        }, ct);
    }

    public Task<Dictionary<int, AxisFeedback>> GetAllAxesFeedbackAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var result = new Dictionary<int, AxisFeedback>();

            lock (_lock)
            {
                try
                {
                    // 批量获取位置 - 32轴
                    var positions = new float[32];
                    var speeds = new float[32];

                    var ret1 = zmcaux.ZAux_GetModbusMpos(_handle, 32, positions);
                    var ret2 = zmcaux.ZAux_GetModbusCurSpeed(_handle, 32, speeds);

                    if (ret1 == 0)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            var feedback = new AxisFeedback(
                                AxisNumber: i,
                                Position: positions[i],
                                Velocity: speeds[i],
                                StatusWord: 0,
                                IsEnabled: false,
                                IsHomed: false,
                                HasAlarm: false,
                                AlarmCode: 0
                            );
                            result[i] = feedback;
                        }
                    }
                    else
                    {
                        // 批量读取失败，逐个读取
                        _logger.Warning("Batch read failed, reading axes individually");
                        for (int i = 0; i < 32; i++)
                        {
                            result[i] = GetAxisFeedbackAsync(i).Result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error getting all axes feedback: {ex.Message}", ex);
                }
            }

            return result;
        }, ct);
    }

    public Task<CommandResult> EnableAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // ZMC使用WDOG参数控制轴使能 WDOG=1使能, WDOG=0关闭
                    var ret = zmcaux.ZAux_Direct_SetParam(_handle, "WDOG", axisNumber, 1);
                    if (ret != 0)
                    {
                        _logger.Error($"Failed to enable axis {axisNumber}, error: {ret}");
                        return CommandResult.Fail(ret, $"Enable axis failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} enabled");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception enabling axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> DisableAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var ret = zmcaux.ZAux_Direct_SetParam(_handle, "WDOG", axisNumber, 0);
                    if (ret != 0)
                    {
                        _logger.Error($"Failed to disable axis {axisNumber}, error: {ret}");
                        return CommandResult.Fail(ret, $"Disable axis failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} disabled");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception disabling axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> MoveAbsoluteAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 设置速度
                    var ret1 = zmcaux.ZAux_Direct_SetSpeed(_handle, axisNumber, (float)command.Velocity);
                    if (ret1 != 0)
                    {
                        return CommandResult.Fail(ret1, $"Set speed failed with error {ret1}");
                    }

                    // 设置加速度
                    var ret2 = zmcaux.ZAux_Direct_SetAccel(_handle, axisNumber, (float)command.Acceleration);
                    if (ret2 != 0)
                    {
                        return CommandResult.Fail(ret2, $"Set acceleration failed with error {ret2}");
                    }

                    // 设置减速度
                    var ret3 = zmcaux.ZAux_Direct_SetDecel(_handle, axisNumber, (float)command.Deceleration);
                    if (ret3 != 0)
                    {
                        return CommandResult.Fail(ret3, $"Set deceleration failed with error {ret3}");
                    }

                    // 执行绝对运动
                    var ret4 = zmcaux.ZAux_Direct_MoveAbs(_handle, 1, new[] { axisNumber }, new[] { (float)command.Position });
                    if (ret4 != 0)
                    {
                        _logger.Error($"MoveAbs failed for axis {axisNumber}, error: {ret4}");
                        return CommandResult.Fail(ret4, $"MoveAbs failed with error {ret4}");
                    }

                    _logger.Debug($"Axis {axisNumber} move to {command.Position} at {command.Velocity}");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception moving axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> MoveRelativeAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 设置速度
                    zmcaux.ZAux_Direct_SetSpeed(_handle, axisNumber, (float)command.Velocity);
                    zmcaux.ZAux_Direct_SetAccel(_handle, axisNumber, (float)command.Acceleration);
                    zmcaux.ZAux_Direct_SetDecel(_handle, axisNumber, (float)command.Deceleration);

                    // 执行相对运动
                    var ret = zmcaux.ZAux_Direct_Move(_handle, 1, new[] { axisNumber }, new[] { (float)command.Position });
                    if (ret != 0)
                    {
                        _logger.Error($"MoveRel failed for axis {axisNumber}, error: {ret}");
                        return CommandResult.Fail(ret, $"MoveRel failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} move relative {command.Position} at {command.Velocity}");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception moving axis {axisNumber} relatively: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> JogAsync(int axisNumber, double velocity, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // JOG运动 - 使用MoveSp函数
                    var ret = zmcaux.ZAux_Direct_MoveSp(_handle, 1, new[] { axisNumber }, new[] { (float)velocity });
                    if (ret != 0)
                    {
                        _logger.Error($"Jog failed for axis {axisNumber}, error: {ret}");
                        return CommandResult.Fail(ret, $"Jog failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} jog at {velocity}");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception jogging axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> StopAxisAsync(int axisNumber, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 正常停止 - 使用MOVEPAUSE
                    var ret = zmcaux.ZAux_Direct_MovePause(_handle, axisNumber, 2);
                    if (ret != 0)
                    {
                        // 尝试直接停止
                        ret = zmcaux.ZAux_Direct_Rapidstop(_handle, 2);
                    }

                    _logger.Debug($"Axis {axisNumber} stop requested");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception stopping axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> EmergencyStopAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 急停 - 快速停止所有轴
                    var ret = zmcaux.ZAux_Direct_Rapidstop(_handle, 3);  // mode 3 = 快速停止所有轴
                    if (ret != 0)
                    {
                        _logger.Error($"Emergency stop failed, error: {ret}");
                        return CommandResult.Fail(ret, $"Emergency stop failed with error {ret}");
                    }

                    _logger.Warning("Emergency stop executed");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception during emergency stop: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> HomeAxisAsync(int axisNumber, HomeProfile profile, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 设置回零速度
                    zmcaux.ZAux_Direct_SetSpeed(_handle, axisNumber, (float)profile.SearchSpeed);
                    zmcaux.ZAux_Direct_SetCreep(_handle, axisNumber, (float)profile.LatchSpeed);
                    zmcaux.ZAux_Direct_SetAccel(_handle, axisNumber, (float)profile.Accel);

                    // 执行回零命令 - 使用BASIC的DATUM指令
                    var cmd = $"DATUM({axisNumber},{profile.HomeMode})";
                    var ret = zmcaux.ZAux_DirectCommand(_handle, cmd, null, 0);

                    if (ret != 0)
                    {
                        _logger.Error($"Home failed for axis {axisNumber}, error: {ret}");
                        return CommandResult.Fail(ret, $"Home failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} home started, mode: {profile.HomeMode}");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception homing axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<CommandResult> ResetAxisAlarmAsync(int axisNumber, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 清除报警 - 通过关闭再开启WDOG
                    zmcaux.ZAux_Direct_SetParam(_handle, "WDOG", axisNumber, 0);
                    Thread.Sleep(100);
                    var ret = zmcaux.ZAux_Direct_SetParam(_handle, "WDOG", axisNumber, 1);

                    if (ret != 0)
                    {
                        return CommandResult.Fail(ret, $"Reset alarm failed with error {ret}");
                    }

                    _logger.Debug($"Axis {axisNumber} alarm reset");
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception resetting alarm for axis {axisNumber}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<uint[]> ReadInputsAsync(int startIndex, int count, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var inputs = new uint[count];
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint value = 0;
                        zmcaux.ZAux_Direct_GetIn(_handle, startIndex + i, ref value);
                        inputs[i] = value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error reading inputs: {ex.Message}", ex);
                }
                return inputs;
            }
        }, ct);
    }

    public Task<uint[]> ReadOutputsAsync(int startIndex, int count, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var outputs = new uint[count];
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint value = 0;
                        zmcaux.ZAux_Direct_GetOp(_handle, startIndex + i, ref value);
                        outputs[i] = value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error reading outputs: {ex.Message}", ex);
                }
                return outputs;
            }
        }, ct);
    }

    public Task<CommandResult> WriteOutputAsync(int index, bool value, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var ret = zmcaux.ZAux_Direct_SetOp(_handle, index, value ? 1u : 0u);
                    if (ret != 0)
                    {
                        return CommandResult.Fail(ret, $"Write output failed with error {ret}");
                    }
                    return CommandResult.Ok();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception writing output {index}: {ex.Message}", ex);
                    return CommandResult.Fail(-1, ex.Message);
                }
            }
        }, ct);
    }

    public Task<bool> GetEtherCatStateAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 检查EtherCAT状态 - 通过执行命令获取
                    var response = new StringBuilder(256);
                    var ret = zmcaux.ZAux_DirectCommand(_handle, "?ETHERCAT(0)", response, 256);
                    if (ret == 0)
                    {
                        // 返回值0表示正常
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Could not get EtherCAT state: {ex.Message}");
                    return false;
                }
            }
        }, ct);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                if (_handle != IntPtr.Zero)
                {
                    zmcaux.ZAux_Close(_handle);
                    _handle = IntPtr.Zero;
                }
            }
            _disposed = true;
        }
    }
}
