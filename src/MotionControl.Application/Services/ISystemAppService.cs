using MotionControl.Domain.Enums;

namespace MotionControl.Application.Services;

/// <summary>
/// 系统应用服务接口
/// </summary>
public interface ISystemAppService
{
    /// <summary>
    /// 获取系统状态
    /// </summary>
    SystemStatusDto GetSystemStatus();

    /// <summary>
    /// 连接控制器
    /// </summary>
    Task<CommandResultDto> ConnectAsync(string ip, int port = 5005, CancellationToken ct = default);

    /// <summary>
    /// 断开控制器
    /// </summary>
    Task<CommandResultDto> DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 初始化系统
    /// </summary>
    Task<CommandResultDto> InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// 进入手动模式
    /// </summary>
    void EnterManualMode();

    /// <summary>
    /// 进入自动模式
    /// </summary>
    void EnterAutoMode();

    /// <summary>
    /// 退出自动模式
    /// </summary>
    void ExitAutoMode();
}

/// <summary>
/// 系统状态DTO
/// </summary>
public record SystemStatusDto(
    MachineState CurrentState,
    bool IsConnected,
    bool EtherCatOnline,
    int TotalAxes,
    int HomedAxes,
    int EnabledAxes,
    int ActiveAlarms,
    bool IsSafeForOperation
);
