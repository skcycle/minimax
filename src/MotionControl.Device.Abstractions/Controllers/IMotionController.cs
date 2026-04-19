using MotionControl.Contracts.Constants;

namespace MotionControl.Device.Abstractions.Controllers;

/// <summary>
/// 控制器信息
/// </summary>
public record ControllerInfo(
    string Model,
    string SerialNumber,
    string FirmwareVersion,
    int AxisCount
);

/// <summary>
/// 轴反馈数据
/// </summary>
public record AxisFeedback(
    int AxisNumber,
    double Position,
    double Velocity,
    uint StatusWord,
    bool IsEnabled,
    bool IsHomed,
    bool HasAlarm,
    int AlarmCode
);

/// <summary>
/// IO点反馈
/// </summary>
public record IoFeedback(
    int InputCount,
    int OutputCount,
    uint[] Inputs,
    uint[] Outputs
);

/// <summary>
/// 轴命令
/// </summary>
public record AxisMoveCommand(
    int AxisNumber,
    double Position,
    double Velocity,
    double Acceleration,
    double Deceleration
);

/// <summary>
/// 回零配置
/// </summary>
public record HomeProfile(
    double SearchSpeed,      // 搜索速度
    double LatchSpeed,       // 捕获速度
    double Accel,            // 加速度
    int HomeMode,            // 回零模式 (0=原点限位, 1=索引信号, 2=机械原点)
    int HomeDirection        // 回零方向 (0=正向, 1=负向)
);

/// <summary>
/// 运动控制器接口 - 上层唯一该依赖的控制器接口
/// </summary>
public interface IMotionController : IDisposable
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接状态
    /// </summary>
    ControllerState State { get; }

    /// <summary>
    /// 连接控制器
    /// </summary>
    Task<bool> ConnectAsync(string ip, int port = 5005, CancellationToken ct = default);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 获取控制器信息
    /// </summary>
    Task<ControllerInfo> GetControllerInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取单个轴反馈
    /// </summary>
    Task<AxisFeedback> GetAxisFeedbackAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 批量获取所有轴反馈 (性能优化)
    /// </summary>
    Task<Dictionary<int, AxisFeedback>> GetAllAxesFeedbackAsync(CancellationToken ct = default);

    /// <summary>
    /// 轴使能
    /// </summary>
    Task<CommandResult> EnableAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 轴失能
    /// </summary>
    Task<CommandResult> DisableAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 绝对运动
    /// </summary>
    Task<CommandResult> MoveAbsoluteAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default);

    /// <summary>
    /// 相对运动
    /// </summary>
    Task<CommandResult> MoveRelativeAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default);

    /// <summary>
    /// 点动
    /// </summary>
    Task<CommandResult> JogAsync(int axisNumber, double velocity, CancellationToken ct = default);

    /// <summary>
    /// 停止轴
    /// </summary>
    Task<CommandResult> StopAxisAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 紧急停止
    /// </summary>
    Task<CommandResult> EmergencyStopAsync(CancellationToken ct = default);

    /// <summary>
    /// 回零
    /// </summary>
    Task<CommandResult> HomeAxisAsync(int axisNumber, HomeProfile profile, CancellationToken ct = default);

    /// <summary>
    /// 清除轴报警
    /// </summary>
    Task<CommandResult> ResetAxisAlarmAsync(int axisNumber, CancellationToken ct = default);

    /// <summary>
    /// 读取输入IO
    /// </summary>
    Task<uint[]> ReadInputsAsync(int startIndex, int count, CancellationToken ct = default);

    /// <summary>
    /// 读取输出IO
    /// </summary>
    Task<uint[]> ReadOutputsAsync(int startIndex, int count, CancellationToken ct = default);

    /// <summary>
    /// 写入输出IO
    /// </summary>
    Task<CommandResult> WriteOutputAsync(int index, bool value, CancellationToken ct = default);

    /// <summary>
    /// 获取EtherCAT状态
    /// </summary>
    Task<bool> GetEtherCatStateAsync(CancellationToken ct = default);

    /// <summary>
    /// 事件：连接状态变更
    /// </summary>
    event EventHandler<ControllerState>? ConnectionStateChanged;
}

/// <summary>
/// 命令结果
/// </summary>
public record CommandResult(
    bool Success,
    int ErrorCode = 0,
    string? ErrorMessage = null
)
{
    public static CommandResult Ok() => new(true);
    public static CommandResult Fail(int errorCode, string? message = null) => new(false, errorCode, message);
}

/// <summary>
/// 回零结果
/// </summary>
public record HomeResult(
    bool Success,
    double HomePosition,
    int ErrorCode = 0,
    string? ErrorMessage = null
);
