using System.Windows.Input;
using MotionControl.Application.Services;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Domain.ValueObjects;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 轴调试ViewModel
/// </summary>
public class AxisDebugViewModel : ViewModelBase
{
    private readonly IHomingAppService _homingService;
    private readonly IMotionAppService _motionService;
    private readonly IAxisAppService _axisService;

    private int _selectedAxisNumber;
    public int SelectedAxisNumber
    {
        get => _selectedAxisNumber;
        set
        {
            if (SetProperty(ref _selectedAxisNumber, value))
            {
                RefreshAxisInfo();
            }
        }
    }

    private string _axisName = "";
    public string AxisName
    {
        get => _axisName;
        set => SetProperty(ref _axisName, value);
    }

    private double _currentPosition;
    public double CurrentPosition
    {
        get => _currentPosition;
        set => SetProperty(ref _currentPosition, value);
    }

    private double _targetPosition;
    public double TargetPosition
    {
        get => _targetPosition;
        set => SetProperty(ref _targetPosition, value);
    }

    private double _jogVelocity = 100;
    public double JogVelocity
    {
        get => _jogVelocity;
        set => SetProperty(ref _jogVelocity, value);
    }

    private double _moveVelocity = 500;
    public double MoveVelocity
    {
        get => _moveVelocity;
        set => SetProperty(ref _moveVelocity, value);
    }

    private double _acceleration = 5000;
    public double Acceleration
    {
        get => _acceleration;
        set => SetProperty(ref _acceleration, value);
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    private bool _isHomed;
    public bool IsHomed
    {
        get => _isHomed;
        set => SetProperty(ref _isHomed, value);
    }

    private bool _isMoving;
    public bool IsMoving
    {
        get => _isMoving;
        set => SetProperty(ref _isMoving, value);
    }

    private bool _hasAlarm;
    public bool HasAlarm
    {
        get => _hasAlarm;
        set => SetProperty(ref _hasAlarm, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // 快捷轴选择
    public int[] AxisNumbers { get; } = Enumerable.Range(0, 32).ToArray();

    // 命令
    public AsyncRelayCommand EnableCommand { get; }
    public AsyncRelayCommand DisableCommand { get; }
    public AsyncRelayCommand HomeCommand { get; }
    public AsyncRelayCommand ClearAlarmCommand { get; }
    public AsyncRelayCommand MoveAbsCommand { get; }
    public AsyncRelayCommand MoveRelPlusCommand { get; }
    public AsyncRelayCommand MoveRelMinusCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand JogPlusCommand { get; }
    public AsyncRelayCommand JogMinusCommand { get; }
    public RelayCommand StopJogCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public AxisDebugViewModel(
        IHomingAppService homingService,
        IMotionAppService motionService,
        IAxisAppService axisService)
    {
        _homingService = homingService;
        _motionService = motionService;
        _axisService = axisService;

        EnableCommand = new AsyncRelayCommand(EnableAsync);
        DisableCommand = new AsyncRelayCommand(DisableAsync);
        HomeCommand = new AsyncRelayCommand(HomeAsync);
        ClearAlarmCommand = new AsyncRelayCommand(ClearAlarmAsync);
        MoveAbsCommand = new AsyncRelayCommand(MoveAbsAsync);
        MoveRelPlusCommand = new AsyncRelayCommand(() => MoveRelAsync(10));
        MoveRelMinusCommand = new AsyncRelayCommand(() => MoveRelAsync(-10));
        StopCommand = new AsyncRelayCommand(StopAsync);
        JogPlusCommand = new AsyncRelayCommand(() => JogAsync(1));
        JogMinusCommand = new AsyncRelayCommand(() => JogAsync(-1));
        StopJogCommand = new RelayCommand(StopJog);
        RefreshCommand = new RelayCommand(_ => RefreshAxisInfo());
    }

    private async Task EnableAsync()
    {
        var result = await _axisService.EnableAxisAsync(SelectedAxisNumber);
        if (result.Success)
        {
            IsEnabled = true;
            StatusMessage = $"轴 {SelectedAxisNumber} 已使能";
        }
        else
        {
            StatusMessage = $"使能失败: {result.ErrorMessage}";
        }
    }

    private async Task DisableAsync()
    {
        var result = await _axisService.DisableAxisAsync(SelectedAxisNumber);
        if (result.Success)
        {
            IsEnabled = false;
            StatusMessage = $"轴 {SelectedAxisNumber} 已失能";
        }
        else
        {
            StatusMessage = $"失能失败: {result.ErrorMessage}";
        }
    }

    private async Task HomeAsync()
    {
        var profile = new HomeProfile(
            SearchSpeed: 100,
            LatchSpeed: 10,
            Accel: 5000,
            HomeMode: 0,  // 原点限位回零
            HomeDirection: 0
        );

        var result = await _homingService.HomeAxisAsync(SelectedAxisNumber, profile);
        if (result.Success)
        {
            StatusMessage = $"轴 {SelectedAxisNumber} 回零中...";
        }
        else
        {
            StatusMessage = $"回零失败: {result.ErrorMessage}";
        }
    }

    private async Task ClearAlarmAsync()
    {
        var result = await _axisService.EnableAxisAsync(SelectedAxisNumber);
        if (result.Success)
        {
            HasAlarm = false;
            StatusMessage = $"轴 {SelectedAxisNumber} 报警已清除";
        }
        else
        {
            StatusMessage = $"清除失败: {result.ErrorMessage}";
        }
    }

    private async Task MoveAbsAsync()
    {
        var result = await _motionService.MoveAbsoluteAsync(
            SelectedAxisNumber,
            TargetPosition,
            MoveVelocity,
            Acceleration);

        if (result.Success)
        {
            StatusMessage = $"轴 {SelectedAxisNumber} 运动到 {TargetPosition}";
        }
        else
        {
            StatusMessage = $"运动失败: {result.ErrorMessage}";
        }
    }

    private async Task MoveRelAsync(double distance)
    {
        var result = await _motionService.MoveRelativeAsync(
            SelectedAxisNumber,
            distance,
            MoveVelocity,
            Acceleration);

        if (result.Success)
        {
            StatusMessage = $"轴 {SelectedAxisNumber} 相对运动 {distance}";
        }
        else
        {
            StatusMessage = $"运动失败: {result.ErrorMessage}";
        }
    }

    private async Task StopAsync()
    {
        var result = await _motionService.StopAxisAsync(SelectedAxisNumber);
        if (result.Success)
        {
            StatusMessage = $"轴 {SelectedAxisNumber} 已停止";
        }
    }

    private async Task JogAsync(double direction)
    {
        var velocity = JogVelocity * direction;
        var result = await _motionService.JogAsync(SelectedAxisNumber, velocity);
        if (result.Success)
        {
            IsMoving = true;
            StatusMessage = $"轴 {SelectedAxisNumber} JOG {velocity}";
        }
    }

    private void StopJog(object? parameter)
    {
        _ = StopAsync();
        IsMoving = false;
    }

    private void RefreshAxisInfo()
    {
        var allStatus = _axisService.GetAllAxesStatus();
        if (SelectedAxisNumber >= 0 && SelectedAxisNumber < allStatus.Count)
        {
            var status = allStatus[SelectedAxisNumber];
            AxisName = status.Name;
            CurrentPosition = status.CurrentPosition;
            TargetPosition = status.TargetPosition;
            IsEnabled = status.ServoState == "Enabled";
            IsHomed = status.IsHomed;
            HasAlarm = status.HasAlarm;
            IsMoving = status.State == "Moving";
        }
    }
}
