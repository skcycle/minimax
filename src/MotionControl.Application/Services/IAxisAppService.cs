using MotionControl.Contracts.Constants;
using MotionControl.Domain.Entities;

namespace MotionControl.Application.Services;

/// <summary>
/// 轴应用服务接口
/// </summary>
public interface IAxisAppService
{
    /// <summary>
    /// 获取轴信息
    /// </summary>
    AxisStatusDto? GetAxisStatus(int axisNumber);

    /// <summary>
    /// 获取所有轴状态
    /// </summary>
    IReadOnlyList<AxisStatusDto> GetAllAxesStatus();

    /// <summary>
    /// 使能轴
    /// </summary>
    Task<CommandResultDto> EnableAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 失能轴
    /// </summary>
    Task<CommandResultDto> DisableAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 使能所有轴
    /// </summary>
    Task<CommandResultDto> EnableAllAxesAsync(CancellationToken ct = default);

    /// <summary>
    /// 失能所有轴
    /// </summary>
    Task<CommandResultDto> DisableAllAxesAsync(CancellationToken ct = default);
}

/// <summary>
/// 轴状态DTO
/// </summary>
public record AxisStatusDto(
    int AxisNumber,
    string Name,
    double CurrentPosition,
    double CurrentVelocity,
    double TargetPosition,
    string State,
    string ServoState,
    bool IsHomed,
    bool HasAlarm,
    bool PositiveLimit,
    bool NegativeLimit,
    int AlarmCode
);

/// <summary>
/// 命令结果DTO
/// </summary>
public record CommandResultDto(
    bool Success,
    int ErrorCode,
    string? ErrorMessage
)
{
    public static CommandResultDto Ok() => new(true, 0, null);
    public static CommandResultDto Fail(int errorCode, string? message) => new(false, errorCode, message);
}
