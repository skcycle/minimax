using MotionControl.Contracts.Constants;
using MotionControl.Contracts.Events;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Interfaces;
using MotionControl.Infrastructure.Logging;
using MotionControl.Infrastructure.Messaging;

namespace MotionControl.Application.Services;

/// <summary>
/// 控制器状态轮询服务
/// 周期性读取32轴状态，更新领域对象，发布事件
/// </summary>
public interface IStatePollingService : IDisposable
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 轮询周期(毫秒)
    /// </summary>
    int PollingIntervalMs { get; set; }

    /// <summary>
    /// 开始轮询
    /// </summary>
    void Start();

    /// <summary>
    /// 停止轮询
    /// </summary>
    void Stop();
}

/// <summary>
/// 控制器状态轮询服务实现
/// </summary>
public class ControllerPollingService : IStatePollingService
{
    private readonly IMotionController _controller;
    private readonly IMachineRepository _machineRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private bool _disposed;

    public bool IsRunning => _pollingTask != null && !_pollingTask.IsCompleted;
    public int PollingIntervalMs { get; set; } = SystemConstants.DefaultPollingIntervalMs;

    public ControllerPollingService(
        IMotionController controller,
        IMachineRepository machineRepository,
        IEventBus eventBus,
        ILogger logger)
    {
        _controller = controller;
        _machineRepository = machineRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _pollingTask = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
        _logger.Info($"State polling started, interval: {PollingIntervalMs}ms");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _logger.Info("State polling stopped");
    }

    private async Task PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PollAxisStatesAsync(ct);
                await Task.Delay(PollingIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Polling error: {ex.Message}", ex);
                await Task.Delay(1000, ct); // 错误时延迟1秒
            }
        }
    }

    /// <summary>
    /// 轮询所有轴状态
    /// </summary>
    private async Task PollAxisStatesAsync(CancellationToken ct)
    {
        if (!_controller.IsConnected) return;

        try
        {
            var feedbacks = await _controller.GetAllAxesFeedbackAsync(ct);
            var machine = _machineRepository.GetMachine();

            foreach (var (axisNumber, feedback) in feedbacks)
            {
                var axis = machine.GetAxis(axisNumber);
                if (axis == null) continue;

                // 更新位置
                var oldPosition = axis.CurrentPosition;
                axis.CurrentPosition = feedback.Position;
                axis.CurrentVelocity = feedback.Velocity;

                // 更新状态
                axis.UpdateFeedback(
                    feedback.Position,
                    feedback.Velocity,
                    feedback.StatusWord
                );

                // 发布位置变更事件
                if (Math.Abs(oldPosition - feedback.Position) > 0.0001)
                {
                    _eventBus.Publish(new AxisPositionChangedEvent(axisNumber, oldPosition, feedback.Position));
                }

                // 发布状态变更事件
                if (axis.State != (Domain.Enums.AxisState)(feedback.StatusWord & 0x0F))
                {
                    _eventBus.Publish(new AxisStateChangedEvent(
                        axisNumber,
                        axis.State.ToString(),
                        ((Domain.Enums.AxisState)(feedback.StatusWord & 0x0F)).ToString()
                    ));
                }

                // 处理报警
                if (feedback.HasAlarm && !axis.HasAlarm)
                {
                    axis.SetAlarm(feedback.AlarmCode, "驱动器报警");
                    _eventBus.Publish(new AxisAlarmRaisedEvent(axisNumber, feedback.AlarmCode, "驱动器报警"));
                    machine.RaiseAlarm(
                        axisNumber,
                        new Domain.ValueObjects.AlarmCode(feedback.AlarmCode, "驱动器报警"),
                        Contracts.Events.AlarmLevel.Fault,
                        "驱动器报警"
                    );
                }
                else if (!feedback.HasAlarm && axis.HasAlarm)
                {
                    axis.ClearAlarm();
                    _eventBus.Publish(new AxisAlarmClearedEvent(axisNumber));
                }
            }

            // 检查EtherCAT状态
            var etherCatOnline = await _controller.GetEtherCatStateAsync(ct);
            machine.SafetyState.EtherCatOnline = etherCatOnline;

            if (!etherCatOnline)
            {
                _logger.Warning("EtherCAT offline detected");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error polling axis states: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
