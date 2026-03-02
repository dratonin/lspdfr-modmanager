using System.IO;
using System.Text.Json;
using LSPDFRModManager.Helpers;

namespace LSPDFRModManager.Services;

// ──────────────────────────────────────────────
//  Data model for the JSON config file
// ──────────────────────────────────────────────

/// <summary>
/// Represents the persisted application configuration.
/// Stored as JSON in %AppData%/LSPDFRModManager/config.json.
/// </summary>
public sealed class AppConfig
{
    public string GtaFolderPath { get; set; } = string.Empty;
    public bool IsFirstLaunch { get; set; } = true;
}

// ──────────────────────────────────────────────
//  Service that reads / writes the config file
// ──────────────────────────────────────────────

/// <summary>
/// Manages loading and saving the application configuration to disk.
/// Config is stored in the user's AppData folder so it persists between sessions.
/// </summary>
public sealed class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LSPDFRModManager");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>The current in-memory configuration.</summary>
    public AppConfig Config { get; private set; } = new();

    /// <summary>
    /// Loads config.json from disk. If the file doesn't exist or is corrupt,
    /// a fresh default config is used instead.
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                Logger.Log("Config loaded successfully.");
            }
            else
            {
                Logger.Log("No config file found — using defaults.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to load config: {ex.Message}");
            Config = new AppConfig();
        }
    }

    /// <summary>
    /// Persists the current configuration to config.json.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
            Logger.Log("Config saved successfully.");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to save config: {ex.Message}");
        }
    }
}
