using MotionControl.Domain.Entities;

namespace MotionControl.Domain.Interfaces;

/// <summary>
/// 机器仓储接口
/// </summary>
public interface IMachineRepository
{
    Machine GetMachine();
    void UpdateMachine(Machine machine);
}

/// <summary>
/// 轴仓储接口
/// </summary>
public interface IAxisRepository
{
    Axis? GetById(int axisId);
    IEnumerable<Axis> GetAll();
    void Update(Axis axis);
}

/// <summary>
/// 轴组仓储接口
/// </summary>
public interface IAxisGroupRepository
{
    AxisGroup? GetById(string groupId);
    IEnumerable<AxisGroup> GetAll();
    void Add(AxisGroup group);
    void Remove(string groupId);
}

/// <summary>
/// 配方仓储接口
/// </summary>
public interface IRecipeRepository
{
    Recipe? GetById(string id);
    IEnumerable<Recipe> GetAll();
    void Save(Recipe recipe);
    void Delete(string id);
}

/// <summary>
/// 报警仓储接口
/// </summary>
public interface IAlarmRepository
{
    IEnumerable<Alarm> GetActiveAlarms();
    IEnumerable<Alarm> GetAlarmsByDateRange(DateTime start, DateTime end);
    void Save(Alarm alarm);
}
