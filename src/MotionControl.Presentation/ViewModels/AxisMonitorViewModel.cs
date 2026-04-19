using System.Collections.ObjectModel;
using MotionControl.Application.Services;
using MotionControl.Presentation.Commands;

namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 轴监控ViewModel
/// </summary>
public class AxisMonitorViewModel : ViewModelBase
{
    private readonly IAxisAppService _axisAppService;
    private readonly IMotionAppService _motionAppService;

    public ObservableCollection<AxisDisplayModel> Axes { get; } = new();

    // 命令
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand EnableAllCommand { get; }
    public AsyncRelayCommand DisableAllCommand { get; }
    public RelayCommand SelectAxisCommand { get; }

    private AxisDisplayModel? _selectedAxis;
    public AxisDisplayModel? SelectedAxis
    {
        get => _selectedAxis;
        set => SetProperty(ref _selectedAxis, value);
    }

    public AxisMonitorViewModel(IAxisAppService axisAppService, IMotionAppService motionAppService)
    {
        _axisAppService = axisAppService;
        _motionAppService = motionAppService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        EnableAllCommand = new AsyncRelayCommand(EnableAllAsync);
        DisableAllCommand = new AsyncRelayCommand(DisableAllAsync);
        SelectAxisCommand = new RelayCommand(SelectAxis);

        // 初始化32个轴
        for (int i = 0; i < 32; i++)
        {
            Axes.Add(new AxisDisplayModel
            {
                AxisNumber = i,
                Name = $"轴{i:D2}"
            });
        }
    }

    public async Task RefreshAsync()
    {
        var allStatus = _axisAppService.GetAllAxesStatus();

        for (int i = 0; i < allStatus.Count && i < Axes.Count; i++)
        {
            var status = allStatus[i];
            var display = Axes[i];

            display.AxisNumber = status.AxisNumber;
            display.Name = status.Name;
            display.Position = status.CurrentPosition;
            display.Velocity = status.CurrentVelocity;
            display.State = status.State;
            display.ServoState = status.ServoState;
            display.IsHomed = status.IsHomed;
            display.HasAlarm = status.HasAlarm;
            display.PositiveLimit = status.PositiveLimit;
            display.NegativeLimit = status.NegativeLimit;
        }
    }

    private async Task EnableAllAsync()
    {
        await _axisAppService.EnableAllAxesAsync();
        await RefreshAsync();
    }

    private async Task DisableAllAsync()
    {
        await _axisAppService.DisableAllAxesAsync();
        await RefreshAsync();
    }

    private void SelectAxis(object? parameter)
    {
        if (parameter is AxisDisplayModel axis)
        {
            SelectedAxis = axis;
        }
    }
}
