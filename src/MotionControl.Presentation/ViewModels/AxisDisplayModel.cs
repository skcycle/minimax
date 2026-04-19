namespace MotionControl.Presentation.ViewModels;

/// <summary>
/// 轴显示模型
/// </summary>
public class AxisDisplayModel : ViewModelBase
{
    private int _axisNumber;
    public int AxisNumber
    {
        get => _axisNumber;
        set => SetProperty(ref _axisNumber, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private double _position;
    public double Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    private double _velocity;
    public double Velocity
    {
        get => _velocity;
        set => SetProperty(ref _velocity, value);
    }

    private string _state = "Disabled";
    public string State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    private string _servoState = "Disabled";
    public string ServoState
    {
        get => _servoState;
        set => SetProperty(ref _servoState, value);
    }

    private bool _isHomed;
    public bool IsHomed
    {
        get => _isHomed;
        set => SetProperty(ref _isHomed, value);
    }

    private bool _hasAlarm;
    public bool HasAlarm
    {
        get => _hasAlarm;
        set => SetProperty(ref _hasAlarm, value);
    }

    private bool _positiveLimit;
    public bool PositiveLimit
    {
        get => _positiveLimit;
        set => SetProperty(ref _positiveLimit, value);
    }

    private bool _negativeLimit;
    public bool NegativeLimit
    {
        get => _negativeLimit;
        set => SetProperty(ref _negativeLimit, value);
    }
}
