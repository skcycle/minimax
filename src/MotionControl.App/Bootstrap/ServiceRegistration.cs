using Microsoft.Extensions.DependencyInjection;
using MotionControl.Application.Services;
using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Zmc.Controllers;
using MotionControl.Diagnostics.Services;
using MotionControl.Domain.Interfaces;
using MotionControl.Infrastructure.Configuration;
using MotionControl.Infrastructure.Logging;
using MotionControl.Infrastructure.Messaging;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

/// <summary>
/// 服务注册 - DI容器配置
/// </summary>
public static class ServiceRegistration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // ========== 基础设施层 ==========

        // 日志
        services.AddSingleton<ILogger, ConsoleLogger>();

        // 事件总线
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // 配置提供者
        services.AddSingleton<IConfigurationProvider, JsonConfigurationProvider>();

        // ========== 诊断服务 ==========

        // 报警记录器
        services.AddSingleton<IAlarmRecorder, AlarmRecorder>();

        // 诊断服务
        services.AddSingleton<IDiagnosticService, DiagnosticService>();

        // ========== 设备层 ==========

        // 控制器 - 单例（共享连接）
        services.AddSingleton<IMotionController, ZmcMotionController>();

        // ========== 领域层 ==========

        // 机器仓储
        services.AddSingleton<IMachineRepository, MachineRepository>();

        // ========== 应用层 ==========

        // 状态轮询服务
        services.AddSingleton<IStatePollingService, ControllerPollingService>();

        // 应用服务
        services.AddTransient<ISystemAppService, SystemAppService>();
        services.AddTransient<IMotionAppService, MotionAppService>();
        services.AddTransient<IAxisAppService, AxisAppService>();
        services.AddTransient<IHomingAppService, HomingAppService>();
        services.AddTransient<IAlarmAppService, AlarmAppService>();

        // ========== 控制层 ==========

        // 轴控制服务
        services.AddTransient<IAxisControlService, AxisControlService>();

        // 安全联锁服务
        services.AddTransient<ISafetyInterlockService, SafetyInterlockService>();

        // 组运动服务
        services.AddTransient<IGroupMotionService, GroupMotionService>();

        // 同步运动服务
        services.AddSingleton<ISyncMotionService, SyncMotionService>();

        // ========== 展示层 ==========

        // ViewModels - 每个请求创建新实例
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ConnectionTestViewModel>();
        services.AddTransient<AxisDebugViewModel>();
        services.AddTransient<AxisMonitorViewModel>();
        services.AddTransient<DashboardViewModel>();

        // ========== WPF特定 ==========

        // 主窗口
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
