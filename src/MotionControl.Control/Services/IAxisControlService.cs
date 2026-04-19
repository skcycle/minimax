using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Device.Abstractions.Controllers;

namespace MotionControl.Control.Services;

/// <summary>
/// 轴控制服务接口
/// </summary>
public interface IAxisControlService
{
    /// <summary>
    /// 使能轴
    /// </summary>
    Task<bool> EnableAsync(Axis axis, CancellationToken ct = default);

    /// <summary>
    /// 失能轴
    /// </summary>
    Task<bool> DisableAsync(Axis axis, CancellationToken ct = default);

    /// <summary>
    /// 绝对运动
    /// </summary>
    Task<bool> MoveAbsAsync(Axis axis, Position target, Velocity velocity, CancellationToken ct = default);

    /// <summary>
    /// 相对运动
    /// </summary>
    Task<bool> MoveRelAsync(Axis axis, double distance, Velocity velocity, CancellationToken ct = default);

    /// <summary>
    /// JOG
    /// </summary>
    Task<bool> JogAsync(Axis axis, Velocity velocity, CancellationToken ct = default);

    /// <summary>
    /// 停止
    /// </summary>
    Task<bool> StopAsync(Axis axis, CancellationToken ct = default);

    /// <summary>
    /// 回零
    /// </summary>
    Task<bool> HomeAsync(Axis axis, HomeProfile profile, CancellationToken ct = default);

    /// <summary>
    /// 复位报警
    /// </summary>
    Task<bool> ResetAlarmAsync(Axis axis, CancellationToken ct = default);
}
