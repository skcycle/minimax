using MotionControl.Domain.ValueObjects;
using MotionControl.Contracts.Events;

namespace MotionControl.Application.Services;

/// <summary>
/// 报警信息DTO
/// </summary>
public class AlarmInfo
{
    public int AlarmId { get; set; }
    public int AxisId { get; set; }
    public int Code { get; set; }
    public AlarmLevel Level { get; set; }
    public string Message { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public TimeSpan Duration => DateTime.UtcNow - OccurredAt;
}
