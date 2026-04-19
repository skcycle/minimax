using System.Collections.ObjectModel;
using MotionControl.Application.Services;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 仪表盘ViewModel
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly ISystemAppService _systemAppService;
    private readonly IAxisAppService _axisAppService;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private string _systemState = "Unknown";
    public string SystemState
    {
        get => _systemState;
        set => SetProperty(ref _systemState, value);
    }

    private int _totalAxes = 32;
    public int TotalAxes
    {
        get => _totalAxes;
        set => SetProperty(ref _totalAxes, value);
    }

    private int _homedAxes;
    public int HomedAxes
    {
        get => _homedAxes;
        set => SetProperty(ref _homedAxes, value);
    }

    private int _enabledAxes;
    public int EnabledAxes
    {
        get => _enabledAxes;
        set => SetProperty(ref _enabledAxes, value);
    }

    private int _activeAlarms;
    public int ActiveAlarms
    {
        get => _activeAlarms;
        set => SetProperty(ref _activeAlarms, value);
    }

    private bool _etherCatOnline;
    public bool EtherCatOnline
    {
        get => _etherCatOnline;
        set => SetProperty(ref _etherCatOnline, value);
    }

    private string _controllerIp = "192.168.0.100";
    public string ControllerIp
    {
        get => _controllerIp;
        set => SetProperty(ref _controllerIp, value);
    }

    // 命令
    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand EnableAllCommand { get; }
    public AsyncRelayCommand DisableAllCommand { get; }
    public AsyncRelayCommand HomeAllCommand { get; }
    public AsyncRelayCommand ClearAlarmsCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public DashboardViewModel(ISystemAppService systemAppService, IAxisAppService axisAppService)
    {
        _systemAppService = systemAppService;
        _axisAppService = axisAppService;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        EnableAllCommand = new AsyncRelayCommand(EnableAllAsync);
        DisableAllCommand = new AsyncRelayCommand(DisableAllAsync);
        HomeAllCommand = new AsyncRelayCommand(HomeAllAsync);
        ClearAlarmsCommand = new AsyncRelayCommand(ClearAlarmsAsync);
        RefreshCommand = new RelayCommand(Refresh);

        Refresh();
    }

    private async Task ConnectAsync()
    {
        await _systemAppService.ConnectAsync(ControllerIp);
        Refresh();
    }

    private async Task DisconnectAsync()
    {
        await _systemAppService.DisconnectAsync();
        Refresh();
    }

    private async Task EnableAllAsync()
    {
        await _axisAppService.EnableAllAxesAsync();
        Refresh();
    }

    private async Task DisableAllAsync()
    {
        await _axisAppService.DisableAllAxesAsync();
        Refresh();
    }

    private async Task HomeAllAsync()
    {
        // TODO: 通过HomingAppService
        await Task.Delay(100);
        Refresh();
    }

    private async Task ClearAlarmsAsync()
    {
        // TODO: 通过AlarmAppService
        await Task.Delay(100);
        Refresh();
    }

    private void Refresh()
    {
        var status = _systemAppService.GetSystemStatus();

        IsConnected = status.IsConnected;
        SystemState = status.CurrentState.ToString();
        TotalAxes = status.TotalAxes;
        HomedAxes = status.HomedAxes;
        EnabledAxes = status.EnabledAxes;
        ActiveAlarms = status.ActiveAlarms;
        EtherCatOnline = status.EtherCatOnline;
    }
}
