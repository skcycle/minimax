using System.Windows;
using System.Windows.Threading;

namespace MotionControl.App.Views;

/// <summary>
/// Main window
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        // 启动时钟更新
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => TimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _timer.Start();
        TimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
