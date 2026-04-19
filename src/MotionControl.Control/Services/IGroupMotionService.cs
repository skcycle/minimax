using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

/// <summary>
/// 轴组运动服务接口
/// </summary>
public interface IGroupMotionService
{
    /// <summary>
    /// 组使能
    /// </summary>
    Task<bool> EnableGroupAsync(AxisGroup group, CancellationToken ct = default);

    /// <summary>
    /// 组失能
    /// </summary>
    Task<bool> DisableGroupAsync(AxisGroup group, CancellationToken ct = default);

    /// <summary>
    /// 组回零
    /// </summary>
    Task<bool> HomeGroupAsync(AxisGroup group, CancellationToken ct = default);

    /// <summary>
    /// 组停止
    /// </summary>
    Task<bool> StopGroupAsync(AxisGroup group, CancellationToken ct = default);
}
