using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MotionControl.App.Bootstrap;
using MotionControl.App.Views;
using MotionControl.Application.Services;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置DI容器
        _serviceProvider = ServiceRegistration.ConfigureServices();

        // 创建并显示主窗口
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

        // 获取MainWindowViewModel并设置DataContext
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = viewModel;

        // 启动状态轮询服务
        var pollingService = _serviceProvider.GetRequiredService<IStatePollingService>();
        pollingService.Start();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 停止轮询服务
        if (_serviceProvider != null)
        {
            var pollingService = _serviceProvider.GetService<IStatePollingService>();
            pollingService?.Stop();

            // 断开控制器连接
            var systemService = _serviceProvider.GetService<ISystemAppService>();
            systemService?.DisconnectAsync().Wait();

            // 释放控制器
            var controller = _serviceProvider.GetService<IMotionController>();
            controller?.Dispose();
        }

        base.OnExit(e);
    }
}
