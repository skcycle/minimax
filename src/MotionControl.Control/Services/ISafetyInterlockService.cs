using MotionControl.Domain.Entities;

namespace MotionControl.Control.Services;

/// <summary>
/// 安全联锁服务接口
/// </summary>
public interface ISafetyInterlockService
{
    /// <summary>
    /// 检查轴是否可以使能
    /// </summary>
    bool CanEnableAxis(Axis axis);

    /// <summary>
    /// 检查轴是否可以运动
    /// </summary>
    bool CanMoveAxis(Axis axis);

    /// <summary>
    /// 检查轴是否可以回零
    /// </summary>
    bool CanHomeAxis(Axis axis);

    /// <summary>
    /// 检查系统是否可以启动自动运行
    /// </summary>
    bool CanStartAutoRun(Machine machine);

    /// <summary>
    /// 执行急停
    /// </summary>
    Task EmergencyStopAsync();

    /// <summary>
    /// 获取联锁原因
    /// </summary>
    string? GetInterlockReason(Axis axis);
}
