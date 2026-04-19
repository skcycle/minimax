using System.Text.Json;

namespace MotionControl.Infrastructure.Configuration;

/// <summary>
/// JSON配置提供者实现
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly string _configPath;
    private Dictionary<string, object?> _config = new();

    public JsonConfigurationProvider(string? configPath = null)
    {
        _configPath = configPath ?? "config.json";
        Load();
    }

    public T? Get<T>(string key)
    {
        if (_config.TryGetValue(key, out var value))
        {
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            return (T?)value;
        }
        return default;
    }

    public T GetOrDefault<T>(string key, T defaultValue)
    {
        return Get<T>(key) ?? defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _config[key] = value;
    }

    public Task SaveAsync(CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        return File.WriteAllTextAsync(_configPath, json, ct);
    }

    public Task ReloadAsync(CancellationToken ct = default)
    {
        Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
            }
            catch
            {
                _config = new Dictionary<string, object?>();
            }
        }
    }
}
