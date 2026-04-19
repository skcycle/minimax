using MotionControl.Domain.Entities;
using MotionControl.Domain.Interfaces;

namespace MotionControl.Application.Services;

/// <summary>
/// 机器仓储实现
/// </summary>
public class MachineRepository : IMachineRepository
{
    private readonly Machine _machine;

    public MachineRepository()
    {
        _machine = new Machine();
    }

    public Machine GetMachine() => _machine;

    public void UpdateMachine(Machine machine)
    {
        // 在实际应用中，这里可能需要更深层的同步逻辑
    }
}
