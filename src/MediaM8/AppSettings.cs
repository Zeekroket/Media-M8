using System.Text.Json;
using System.Text.Json.Nodes;

namespace MediaM8;

internal static class AppSettings
{
    private static readonly SettingsStore Store = new(AppPaths.SettingsPath);

    public static string? Read(string key) => Store.Read(key);

    public static void Write(params (string Key, string Value)[] values) => Store.Write(values);
}

internal sealed class SettingsStore
{
    private readonly object Sync = new();
    private readonly string _settingsPath;

    public SettingsStore(string settingsPath)
    {
        _settingsPath = Path.GetFullPath(settingsPath);
    }

    public string? Read(string key)
    {
        lock (Sync)
        {
            try
            {
                var settings = Load();
                return settings[key] is JsonValue value &&
                    value.TryGetValue<string>(out var result)
                    ? result
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public void Write(params (string Key, string Value)[] values)
    {
        lock (Sync)
        {
            string? tempPath = null;
            try
            {
                JsonObject settings;
                try
                {
                    settings = Load();
                }
                catch (JsonException)
                {
                    settings = new JsonObject();
                }

                foreach (var (key, value) in values)
                {
                    settings[key] = value;
                }

                var directory = Path.GetDirectoryName(_settingsPath)!;
                Directory.CreateDirectory(directory);
                tempPath = Path.Combine(directory,
                    "settings-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(tempPath, settings.ToJsonString(
                    new JsonSerializerOptions { WriteIndented = true }));
                File.Move(tempPath, _settingsPath, overwrite: true);
            }
            catch
            {
                // Invalid or unwritable settings must not prevent normal use.
            }
            finally
            {
                if (tempPath is not null)
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }

    private JsonObject Load() =>
        File.Exists(_settingsPath)
            ? JsonNode.Parse(File.ReadAllText(_settingsPath)) as JsonObject
                ?? new JsonObject()
            : new JsonObject();
}
