using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

/// <summary>
/// 同步运动模式
/// </summary>
public enum SyncMode
{
    /// <summary>独立运动</summary>
    Independent,
    /// <summary>龙门同步</summary>
    Gantry,
    /// <summary>电子凸轮</summary>
    Cam,
    /// <summary>电子齿轮</summary>
    Gear
}

/// <summary>
/// 电子凸轮/同步运动服务接口
/// </summary>
public interface ISyncMotionService
{
    /// <summary>
    /// 启动龙门同步
    /// </summary>
    Task<bool> StartGantryAsync(AxisGroup group, CancellationToken ct = default);

    /// <summary>
    /// 停止龙门同步
    /// </summary>
    Task<bool> StopGantryAsync(AxisGroup group, CancellationToken ct = default);

    /// <summary>
    /// 启动电子齿轮同步
    /// </summary>
    Task<bool> StartGearAsync(Axis masterAxis, Axis slaveAxis, double ratio, CancellationToken ct = default);

    /// <summary>
    /// 停止电子齿轮同步
    /// </summary>
    Task<bool> StopGearAsync(Axis slaveAxis, CancellationToken ct = default);

    /// <summary>
    /// 加载凸轮表
    /// </summary>
    Task<bool> LoadCamTableAsync(string camFile, CancellationToken ct = default);

    /// <summary>
    /// 启动电子凸轮
    /// </summary>
    Task<bool> StartCamAsync(Axis camAxis, Axis slaveAxis, CancellationToken ct = default);

    /// <summary>
    /// 停止电子凸轮
    /// </summary>
    Task<bool> StopCamAsync(Axis slaveAxis, CancellationToken ct = default);

    /// <summary>
    /// 获取同步误差
    /// </summary>
    double GetSyncError(Axis slaveAxis);
}
