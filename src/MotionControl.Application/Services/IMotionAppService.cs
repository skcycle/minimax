using MotionControl.Device.Abstractions.Controllers;

namespace MotionControl.Application.Services;

/// <summary>
/// 运动应用服务接口
/// </summary>
public interface IMotionAppService
{
    /// <summary>
    /// 绝对运动
    /// </summary>
    Task<CommandResultDto> MoveAbsoluteAsync(int axisNumber, double position, double velocity, double? accel = null, double? decel = null, CancellationToken ct = default);

    /// <summary>
    /// 相对运动
    /// </summary>
    Task<CommandResultDto> MoveRelativeAsync(int axisNumber, double distance, double velocity, double? accel = null, double? decel = null, CancellationToken ct = default);

    /// <summary>
    /// JOG运动
    /// </summary>
    Task<CommandResultDto> JogAsync(int axisNumber, double velocity, CancellationToken ct = default);

    /// <summary>
    /// 停止轴
    /// </summary>
    Task<CommandResultDto> StopAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 急停
    /// </summary>
    Task<CommandResultDto> EmergencyStopAsync(CancellationToken ct = default);

    /// <summary>
    /// 停止所有轴
    /// </summary>
    Task<CommandResultDto> StopAllAsync(CancellationToken ct = default);
}

/// <summary>
/// 回零应用服务接口
/// </summary>
public interface IHomingAppService
{
    /// <summary>
    /// 回零
    /// </summary>
    Task<CommandResultDto> HomeAxisAsync(int axisNumber, HomeProfile profile, CancellationToken ct = default);

    /// <summary>
    /// 回零所有轴
    /// </summary>
    Task<CommandResultDto> HomeAllAxesAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取回零状态
    /// </summary>
    HomeStatusDto? GetHomeStatus(int axisNumber);
}

/// <summary>
/// 回零状态DTO
/// </summary>
public record HomeStatusDto(
    int AxisNumber,
    bool IsHomed,
    string HomeState,
    double HomePosition
);
