using System.ComponentModel;
using System.Windows.Threading;
using MotionControl.Application.Services;
using MotionControl.Domain.Interfaces;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// MainWindow ViewModel - 应用主窗口
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly ISystemAppService _systemService;
    private readonly IStatePollingService _pollingService;
    private readonly DispatcherTimer _timer;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private string _connectionStatus = "未连接";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    private string _statusMessage = "就绪";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _currentTime = "";
    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    // 子ViewModels
    public ConnectionTestViewModel ConnectionTestViewModel { get; }
    public AxisDebugViewModel AxisDebugViewModel { get; }
    public AxisMonitorViewModel AxisMonitorViewModel { get; }
    public DashboardViewModel DashboardViewModel { get; }

    public MainWindowViewModel(
        ISystemAppService systemService,
        IStatePollingService pollingService,
        IMachineRepository machineRepository,
        IHomingAppService homingService,
        IMotionAppService motionService,
        IAxisAppService axisAppService,
        ILogger logger)
    {
        _systemService = systemService;
        _pollingService = pollingService;

        // 创建子ViewModels
        ConnectionTestViewModel = new ConnectionTestViewModel(systemService, pollingService, machineRepository);
        AxisDebugViewModel = new AxisDebugViewModel(homingService, motionService, axisAppService);
        AxisMonitorViewModel = new AxisMonitorViewModel(machineRepository, pollingService);
        DashboardViewModel = new DashboardViewModel(machineRepository, systemService);

        // 启动定时器
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _timer.Start();
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 监听轮询服务状态
        _pollingService.PropertyChanged += OnPollingServicePropertyChanged;
    }

    private void OnPollingServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IStatePollingService.IsRunning))
        {
            IsConnected = _pollingService.IsRunning;
            ConnectionStatus = _pollingService.IsRunning ? "已连接" : "未连接";
        }
    }
}
