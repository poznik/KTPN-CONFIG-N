using System.IO;
using System.Text.Json;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

/// <summary>Сохранение/загрузка проекта КТПН в JSON (*.ktpn).</summary>
public static class ProjectStorage
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static void Save(ProjectConfig cfg, string path)
    {
        var json = JsonSerializer.Serialize(cfg, Opts);
        File.WriteAllText(path, json);
    }

    public static ProjectConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectConfig>(json, Opts) ?? new ProjectConfig();
    }
}
