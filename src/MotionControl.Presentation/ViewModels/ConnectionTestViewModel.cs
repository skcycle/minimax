using System.Windows.Input;
using MotionControl.Application.Services;
using MotionControl.Domain.Entities;
using MotionControl.Domain.Interfaces;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 连接测试ViewModel
/// </summary>
public class ConnectionTestViewModel : ViewModelBase
{
    private readonly ISystemAppService _systemService;
    private readonly IStatePollingService _pollingService;
    private readonly IMachineRepository _machineRepository;

    private string _ipAddress = "192.168.0.100";
    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    private int _port = 5005;
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private bool _isConnecting;
    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

    private string _connectionStatus = "未连接";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    private string _controllerInfo = "";
    public string ControllerInfo
    {
        get => _controllerInfo;
        set => SetProperty(ref _controllerInfo, value);
    }

    private string _testResults = "";
    public string TestResults
    {
        get => _testResults;
        set => SetProperty(ref _testResults, value);
    }

    private bool _testInProgress;
    public bool TestInProgress
    {
        get => _testInProgress;
        set => SetProperty(ref _testInProgress, value);
    }

    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand TestConnectionCommand { get; }
    public AsyncRelayCommand TestReadAxisCommand { get; }
    public AsyncRelayCommand TestEnableAxisCommand { get; }
    public AsyncRelayCommand TestMoveCommand { get; }

    public ConnectionTestViewModel(
        ISystemAppService systemService,
        IStatePollingService pollingService,
        IMachineRepository machineRepository)
    {
        _systemService = systemService;
        _pollingService = pollingService;
        _machineRepository = machineRepository;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsConnected && !IsConnecting);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        TestReadAxisCommand = new AsyncRelayCommand(TestReadAxisAsync);
        TestEnableAxisCommand = new AsyncRelayCommand(TestEnableAxisAsync);
        TestMoveCommand = new AsyncRelayCommand(TestMoveAsync);
    }

    private async Task ConnectAsync()
    {
        IsConnecting = true;
        ConnectionStatus = $"正在连接 {IpAddress}:{Port}...";

        var result = await _systemService.ConnectAsync(IpAddress, Port);

        if (result.Success)
        {
            IsConnected = true;
            ConnectionStatus = "已连接";
            AppendResult($"✓ 连接成功: {IpAddress}:{Port}");

            // 获取控制器信息
            var info = _systemService.GetSystemStatus();
            ControllerInfo = $"控制器: ZMC432 | 轴数: {info.TotalAxes}";
        }
        else
        {
            IsConnected = false;
            ConnectionStatus = "连接失败";
            AppendResult($"✗ 连接失败: {result.ErrorMessage}");
        }

        IsConnecting = false;
    }

    private async Task DisconnectAsync()
    {
        ConnectionStatus = "正在断开...";
        await _systemService.DisconnectAsync();
        IsConnected = false;
        ConnectionStatus = "已断开";
        AppendResult("✓ 已断开连接");
    }

    private async Task TestConnectionAsync()
    {
        if (!IsConnected)
        {
            AppendResult("✗ 请先连接控制器");
            return;
        }

        TestInProgress = true;
        AppendResult("--- 开始连接测试 ---");

        try
        {
            // 测试1: EtherCAT状态
            var machine = _machineRepository.GetMachine();
            AppendResult($"EtherCAT状态: {(machine.SafetyState.EtherCatOnline ? "在线" : "离线")}");

            // 测试2: 读取轴状态
            var axes = _machineRepository.GetMachine().Axes;
            AppendResult($"检测到轴数: {axes.Count}");

            // 测试3: 轮询服务
            if (_pollingService.IsRunning)
            {
                AppendResult("轮询服务: 运行中");
            }
            else
            {
                AppendResult("轮询服务: 未运行");
            }

            AppendResult("--- 连接测试完成 ---");
        }
        catch (Exception ex)
        {
            AppendResult($"✗ 测试失败: {ex.Message}");
        }

        TestInProgress = false;
    }

    private async Task TestReadAxisAsync()
    {
        if (!IsConnected)
        {
            AppendResult("✗ 请先连接控制器");
            return;
        }

        TestInProgress = true;
        AppendResult("--- 读取轴数据测试 ---");

        try
        {
            var machine = _machineRepository.GetMachine();

            // 读取前4个轴的状态
            for (int i = 0; i < Math.Min(4, machine.Axes.Count); i++)
            {
                var axis = machine.Axes[i];
                AppendResult($"轴{i}: 位置={axis.CurrentPosition:F3}, 状态={axis.State}, 使能={axis.IsEnabled}");
            }

            AppendResult("--- 读取测试完成 ---");
        }
        catch (Exception ex)
        {
            AppendResult($"✗ 读取失败: {ex.Message}");
        }

        TestInProgress = false;
    }

    private async Task TestEnableAxisAsync()
    {
        if (!IsConnected)
        {
            AppendResult("✗ 请先连接控制器");
            return;
        }

        TestInProgress = true;
        AppendResult("--- 轴使能测试 (轴0) ---");

        try
        {
            AppendResult("注意: 此测试需要在实际控制器上执行");

            AppendResult("--- 使能测试完成 ---");
        }
        catch (Exception ex)
        {
            AppendResult($"✗ 测试失败: {ex.Message}");
        }

        TestInProgress = false;
    }

    private async Task TestMoveAsync()
    {
        if (!IsConnected)
        {
            AppendResult("✗ 请先连接控制器");
            return;
        }

        TestInProgress = true;
        AppendResult("--- 运动测试 (轴0, 相对运动10mm) ---");

        try
        {
            var machine = _machineRepository.GetMachine();
            var axis = machine.GetAxis(0);

            if (axis == null)
            {
                AppendResult("✗ 轴0不存在");
                return;
            }

            // 检查轴状态
            AppendResult($"轴0当前位置: {axis.CurrentPosition:F3}");
            AppendResult($"轴0使能状态: {axis.IsEnabled}");
            AppendResult($"轴0回零状态: {axis.IsHomed}");

            if (!axis.IsEnabled)
            {
                AppendResult("✗ 轴0未使能，请先使能");
            }
            else if (!axis.IsHomed)
            {
                AppendResult("✗ 轴0未回零，请先回零");
            }
            else
            {
                AppendResult("注意: 运动测试需要在实际控制器上执行");
                // var result = await _motionService.MoveRelativeAsync(0, 10, 500);
                // AppendResult(result.Success ? "✓ 运动命令已发送" : $"✗ 运动失败: {result.ErrorMessage}");
            }

            AppendResult("--- 运动测试完成 ---");
        }
        catch (Exception ex)
        {
            AppendResult($"✗ 测试失败: {ex.Message}");
        }

        TestInProgress = false;
    }

    private void AppendResult(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        TestResults = $"{TestResults}\n[{timestamp}] {message}";
    }
}
