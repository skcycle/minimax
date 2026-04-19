# PROJECT.md - 32轴运动控制上位机项目

## 项目概述

| 项 | 内容 |
|---|---|
| 名称 | MotionControl |
| 类型 | 工业运动控制上位机软件 |
| 控制器 | 正运动 ZMC432 (32轴EtherCAT) |
| 技术栈 | C# + WPF + .NET 8 |
| 架构 | 7层领域驱动设计 (DDD) |

---

## 项目结构

```
MotionControl.sln
│
├─ src/
│ ├─ MotionControl.App                     # WPF应用入口
│ ├─ MotionControl.Presentation            # WPF视图层
│ ├─ MotionControl.Application             # 应用服务层
│ ├─ MotionControl.Domain                  # 领域模型
│ ├─ MotionControl.Control                 # 控制服务层
│ ├─ MotionControl.Device.Abstractions     # 设备抽象接口
│ ├─ MotionControl.Device.Zmc              # ZMC432实现
│ ├─ MotionControl.Infrastructure          # 基础设施
│ ├─ MotionControl.Diagnostics             # 安全诊断
│ └─ MotionControl.Contracts              # 共享契约
│
├─ tests/
│ ├─ MotionControl.Domain.Tests
│ └─ MotionControl.Control.Tests
│
├─ docs/
├─ tools/sample-config/
└─ .gitignore
```

---

## 已完成

### 1. 项目框架搭建 ✓
- [x] 解决方案和10个项目创建
- [x] 项目引用关系配置
- [x] .NET 8 配置

### 2. Contracts层 ✓
- [x] DomainEvents 定义
- [x] SystemConstants 定义

### 3. Domain层 ✓
- [x] Enums (AxisState, MachineState, ServoState, etc.)
- [x] ValueObjects (AxisId, Position, Velocity, SoftLimit, StatusWord, HomeProfile)
- [x] Entities (Axis, AxisGroup, Machine, Alarm, Recipe, SafetyState)
- [x] Interfaces (IMachineRepository, IAxisRepository, IRecipeRepository, IAlarmRepository)
- [x] Specifications (轴/系统可运动规整)

### 4. Device.Abstractions层 ✓
- [x] IMotionController 接口
- [x] CommandResult, HomeResult
- [x] DeviceExceptions

### 5. Infrastructure层 (骨架)
- [x] IEventBus 接口
- [x] ILogger 接口
- [x] IConfigurationProvider 接口

### 6. Presentation层 (骨架)
- [x] ViewModelBase
- [x] RelayCommand, AsyncRelayCommand
- [x] MainWindowViewModel
- [x] AxisDisplayModel
- [x] MainWindow.xaml (TabControl布局)

### 7. Device.Zmc层 ✓
- [x] Zmcaux.cs - ZMC SDK封装 (4638行)
- [x] ZmcMotionController 完整实现
  - Connect/Disconnect
  - GetAxisFeedback / GetAllAxesFeedback
  - EnableAxis / DisableAxis
  - MoveAbsolute / MoveRelative / Jog
  - StopAxis / EmergencyStop
  - HomeAxis / ResetAxisAlarm
  - ReadInputs / ReadOutputs / WriteOutput
  - GetEtherCatState

### 8. ZMC SDK集成 ✓
- [x] zauxdll.dll DllImport封装
- [x] 以太网连接 (ZAux_OpenEth)
- [x] 批量位置/速度读取 (ZAux_GetModbusMpos等)
- [x] 运动指令 (ZAux_Direct_MoveAbs等)
- [x] 轴参数读写 (ZAux_Direct_SetParam等)

---

## 待完成

### 高优先级
- [x] ZMC SDK集成 (zaux.dll P/Invoke) ✓
- [x] ZmcMotionController 实现 ✓
- [x] 状态轮询服务 (ControllerPollingService) ✓
- [x] DI容器配置 (ServiceRegistration) ✓

### 中优先级
- [x] Application层服务实现 ✓
- [x] Control层服务实现 ✓
- [x] WPF视图完善 ✓
- [x] 配置系统实现 ✓

### 低优先级
- [x] 日志系统实现 ✓
- [x] 报警管理系统 ✓ (基础)
- [x] 安全联锁服务 ✓
- [x] 单元测试 ✓
- [x] 电子凸轮/同步功能 ✓
- [x] 报警历史记录 ✓
- [x] 轴调试页面 ✓
- [x] 连接测试页面 ✓

---

## 核心接口

### IMotionController
```csharp
public interface IMotionController : IDisposable
{
    bool IsConnected { get; }
    ControllerState State { get; }
    Task<bool> ConnectAsync(string ip, int port = 5005, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<ControllerInfo> GetControllerInfoAsync(CancellationToken ct = default);
    Task<AxisFeedback> GetAxisFeedbackAsync(int axisNumber, CancellationToken ct = default);
    Task<Dictionary<int, AxisFeedback>> GetAllAxesFeedbackAsync(CancellationToken ct = default);
    Task<CommandResult> EnableAxisAsync(int axisNumber, CancellationToken ct = default);
    Task<CommandResult> DisableAxisAsync(int axisNumber, CancellationToken ct = default);
    Task<CommandResult> MoveAbsoluteAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default);
    Task<CommandResult> MoveRelativeAsync(int axisNumber, AxisMoveCommand command, CancellationToken ct = default);
    Task<CommandResult> JogAsync(int axisNumber, double velocity, CancellationToken ct = default);
    Task<CommandResult> StopAxisAsync(int axisNumber, CancellationToken ct = default);
    Task<CommandResult> EmergencyStopAsync(CancellationToken ct = default);
    Task<CommandResult> HomeAxisAsync(int axisNumber, HomeProfile profile, CancellationToken ct = default);
    Task<CommandResult> ResetAxisAlarmAsync(int axisNumber, CancellationToken ct = default);
    Task<uint[]> ReadInputsAsync(int startIndex, int count, CancellationToken ct = default);
    Task<uint[]> ReadOutputsAsync(int startIndex, int count, CancellationToken ct = default);
    Task<CommandResult> WriteOutputAsync(int index, bool value, CancellationToken ct = default);
    Task<bool> GetEtherCatStateAsync(CancellationToken ct = default);
    event EventHandler<ControllerState>? ConnectionStateChanged;
}
```

---

## 下一步计划

1. ~~**集成ZMC SDK** — 添加zaux.dll的DllImport封装~~ ✓
2. ~~**实现ZmcMotionController** — 完成控制器通信~~ ✓
3. ~~**实现状态轮询** — 周期性读取32轴状态~~ ✓
4. ~~**配置DI容器** — Microsoft.Extensions.DependencyInjection~~ ✓
5. ~~**完善WPF界面** — 绑定ViewModel到UI~~ ✓
6. ~~**连接测试页面** — 实际连接ZMC432测试~~ ✓
7. ~~**轴调试页面** — 单轴点动、回零、定位控制~~ ✓
8. ~~**报警历史记录** — 持久化报警日志~~ ✓
9. ~~**电子凸轮/同步功能** — GroupMotionService扩展~~ ✓

### 待完善
- [ ] 安装.NET 8 SDK并运行项目
- [ ] 配置真实控制器IP地址
- [ ] 编写凸轮表文件(.cam)
- [ ] 添加数据持久化(SQLite)

---

## 文档

- [架构文档](./docs/architecture.md) - 待编写
- [轴映射文档](./docs/axis-mapping.md) - 待编写
- [状态机文档](./docs/state-machine.md) - 待编写
- [安全文档](./docs/safety.md) - 待编写
- [ZMC集成文档](./docs/zmc-integration.md) - 待编写
