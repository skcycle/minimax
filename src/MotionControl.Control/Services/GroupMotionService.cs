using MotionControl.Domain.Entities;
using MotionControl.Domain.Specifications;
using MotionControl.Infrastructure.Logging;

namespace MotionControl.Control.Services;

/// <summary>
/// 轴组运动服务实现
/// </summary>
public class GroupMotionService : IGroupMotionService
{
    private readonly IAxisControlService _axisControlService;
    private readonly ILogger _logger;

    public GroupMotionService(IAxisControlService axisControlService, ILogger logger)
    {
        _axisControlService = axisControlService;
        _logger = logger;
    }

    public async Task<bool> EnableGroupAsync(AxisGroup group, CancellationToken ct = default)
    {
        _logger.Info($"Enabling group {group.GroupId} with {group.AxisCount} axes");

        var results = new List<bool>();
        foreach (var axis in group.Axes)
        {
            var result = await _axisControlService.EnableAsync(axis, ct);
            results.Add(result);
        }

        var allSuccess = results.All(r => r);
        if (allSuccess)
        {
            _logger.Info($"Group {group.GroupId} enabled successfully");
        }
        else
        {
            _logger.Error($"Failed to enable some axes in group {group.GroupId}");
        }
        return allSuccess;
    }

    public async Task<bool> DisableGroupAsync(AxisGroup group, CancellationToken ct = default)
    {
        _logger.Info($"Disabling group {group.GroupId}");

        var results = new List<bool>();
        foreach (var axis in group.Axes)
        {
            var result = await _axisControlService.DisableAsync(axis, ct);
            results.Add(result);
        }

        return results.All(r => r);
    }

    public async Task<bool> HomeGroupAsync(AxisGroup group, CancellationToken ct = default)
    {
        if (!group.CanMoveTogether())
        {
            var (canMove, reason) = GroupCanMoveSpecification.Check(group);
            _logger.Warning($"Cannot home group {group.GroupId}: {reason}");
            return false;
        }

        _logger.Info($"Homing group {group.GroupId}");

        var results = new List<bool>();
        foreach (var axis in group.Axes)
        {
            // 使用默认回零参数
            var profile = new Domain.ValueObjects.HomeProfile(
                axis.Parameters.HomeSpeed,
                axis.Parameters.HomeLatchSpeed,
                axis.Parameters.DefaultAccel,
                0,  // 默认回零模式
                0
            );
            var result = await _axisControlService.HomeAsync(axis, profile, ct);
            results.Add(result);
        }

        return results.All(r => r);
    }

    public async Task<bool> StopGroupAsync(AxisGroup group, CancellationToken ct = default)
    {
        _logger.Info($"Stopping group {group.GroupId}");

        var results = new List<bool>();
        foreach (var axis in group.Axes)
        {
            var result = await _axisControlService.StopAsync(axis, ct);
            results.Add(result);
        }

        return results.All(r => r);
    }
}
