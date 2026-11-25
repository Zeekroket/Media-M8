using System.Text.Json;
using System.Text.Json.Nodes;

namespace MediaM8;

internal static class AppSettings
{
    private static readonly object Sync = new();

    public static string? Read(string key)
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

    public static void Write(params (string Key, string Value)[] values)
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

                Directory.CreateDirectory(AppPaths.DataDirectory);
                tempPath = Path.Combine(AppPaths.DataDirectory,
                    "settings-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(tempPath, settings.ToJsonString(
                    new JsonSerializerOptions { WriteIndented = true }));
                File.Move(tempPath, AppPaths.SettingsPath, overwrite: true);
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

    private static JsonObject Load() =>
        File.Exists(AppPaths.SettingsPath)
            ? JsonNode.Parse(File.ReadAllText(AppPaths.SettingsPath)) as JsonObject
                ?? new JsonObject()
            : new JsonObject();
}
