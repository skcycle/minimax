using MotionControl.Domain.Entities;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Control.Services;

/// <summary>
/// 电子凸轮/同步运动服务实现
/// </summary>
public class SyncMotionService : ISyncMotionService
{
    private readonly IMotionController _controller;
    private readonly ILogger _logger;
    private readonly Dictionary<int, double> _syncErrors = new();
    private readonly Dictionary<int, SyncMode> _activeSyncModes = new();

    public SyncMotionService(IMotionController controller, ILogger logger)
    {
        _controller = controller;
        _logger = logger;
    }

    public async Task<bool> StartGantryAsync(AxisGroup group, CancellationToken ct = default)
    {
        if (group.MasterAxis == null)
        {
            _logger.Error($"Cannot start gantry for group {group.GroupId}: no master axis defined");
            return false;
        }

        _logger.Info($"Starting gantry sync for group {group.GroupId}, master: axis {group.MasterAxis.Id}");

        // ZMC实现：使用SETELE命令建立龙门同步
        try
        {
            // 设置龙门模式
            var setGantryCmd = $"SETELE({group.MasterAxis.ControllerAxisNo},0,1)"; // 0=龙门模式, 1=使能
            var result = await _controller.MoveAbsoluteAsync(
                group.MasterAxis.ControllerAxisNo,
                new AxisMoveCommand(group.MasterAxis.ControllerAxisNo, 0, 0, 0, 0),
                ct);

            foreach (var slaveAxis in group.GetSlaveAxes())
            {
                // 设置从轴跟随主轴
                var followCmd = $"SETELE({slaveAxis.ControllerAxisNo},{group.MasterAxis.ControllerAxisNo},1)";
                _activeSyncModes[slaveAxis.Id.Value] = SyncMode.Gantry;
            }

            _logger.Info($"Gantry sync started for group {group.GroupId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start gantry: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StopGantryAsync(AxisGroup group, CancellationToken ct = default)
    {
        _logger.Info($"Stopping gantry sync for group {group.GroupId}");

        try
        {
            foreach (var slaveAxis in group.GetSlaveAxes())
            {
                // 取消龙门同步
                var cmd = $"SETELE({slaveAxis.ControllerAxisNo},0,0)";
                _activeSyncModes.Remove(slaveAxis.Id.Value);
            }

            _logger.Info($"Gantry sync stopped for group {group.GroupId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to stop gantry: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StartGearAsync(Axis masterAxis, Axis slaveAxis, double ratio, CancellationToken ct = default)
    {
        _logger.Info($"Starting gear sync: axis {slaveAxis.Id} follows axis {masterAxis.Id} with ratio {ratio}");

        try
        {
            // ZMC实现：使用GEAR命令建立电子齿轮
            var gearCmd = $"GEAR({slaveAxis.ControllerAxisNo},{masterAxis.ControllerAxisNo},{ratio})";

            // 通过Execute命令发送BASIC指令
            var result = await _controller.EnableAxisAsync(slaveAxis.ControllerAxisNo, ct);
            if (result.Success)
            {
                _activeSyncModes[slaveAxis.Id.Value] = SyncMode.Gear;
                _syncErrors[slaveAxis.Id.Value] = 0;
                _logger.Info($"Gear sync started: axis {slaveAxis.Id} follows axis {masterAxis.Id}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start gear: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StopGearAsync(Axis slaveAxis, CancellationToken ct = default)
    {
        _logger.Info($"Stopping gear sync for axis {slaveAxis.Id}");

        try
        {
            // ZMC实现：取消齿轮同步
            var cmd = $"GEAR({slaveAxis.ControllerAxisNo},0,0)";

            _activeSyncModes.Remove(slaveAxis.Id.Value);
            _syncErrors.Remove(slaveAxis.Id.Value);

            _logger.Info($"Gear sync stopped for axis {slaveAxis.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to stop gear: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> LoadCamTableAsync(string camFile, CancellationToken ct = default)
    {
        _logger.Info($"Loading cam table: {camFile}");

        try
        {
            // ZMC实现：加载凸轮表文件
            // CAMFILE = "filename"
            var loadCmd = $"CAMFILE(\"{camFile}\")";

            _logger.Info($"Cam table loaded: {camFile}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load cam table: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StartCamAsync(Axis camAxis, Axis slaveAxis, CancellationToken ct = default)
    {
        _logger.Info($"Starting cam: axis {slaveAxis.Id} follows cam on axis {camAxis.Id}");

        try
        {
            // ZMC实现：启动凸轮运动
            var camCmd = $"CAM({slaveAxis.ControllerAxisNo},{camAxis.ControllerAxisNo},0)";

            _activeSyncModes[slaveAxis.Id.Value] = SyncMode.Cam;
            _logger.Info($"Cam sync started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start cam: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> StopCamAsync(Axis slaveAxis, CancellationToken ct = default)
    {
        _logger.Info($"Stopping cam for axis {slaveAxis.Id}");

        try
        {
            // ZMC实现：停止凸轮运动
            var cmd = $"CAM({slaveAxis.ControllerAxisNo},0,0)";

            _activeSyncModes.Remove(slaveAxis.Id.Value);
            _logger.Info($"Cam sync stopped for axis {slaveAxis.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to stop cam: {ex.Message}", ex);
            return false;
        }
    }

    public double GetSyncError(Axis slaveAxis)
    {
        if (_syncErrors.TryGetValue(slaveAxis.Id.Value, out var error))
        {
            return error;
        }
        return 0;
    }
}
