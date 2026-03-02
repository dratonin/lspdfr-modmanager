using System.Collections.ObjectModel;
using System.Diagnostics;
using LSPDFRModManager.Helpers;
using LSPDFRModManager.Models;
using LSPDFRModManager.Services;
using Microsoft.Win32;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// ViewModel for the Mod Library screen.
/// Displays available mods with version info from the LCPDFR API,
/// lets the user open download pages, and locate + install .zip archives.
/// </summary>
public sealed class LibraryViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly FileService _fileService = new();
    private readonly LcpdfrApiService _apiService = new();

    private string _statusMessage = "Ready";

    // ──────────────────────────────────────────────
    //  Bindable properties
    // ──────────────────────────────────────────────

    /// <summary>Path to the GTA V installation (loaded from config).</summary>
    public string GtaFolderPath => _configService.Config.GtaFolderPath;

    /// <summary>Global status message shown in the bottom bar.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Collection of available mods displayed as cards.</summary>
    public ObservableCollection<ModEntry> Mods { get; } = [];

    // ──────────────────────────────────────────────
    //  Commands
    // ──────────────────────────────────────────────

    /// <summary>Opens the mod's LCPDFR download page in the default browser.</summary>
    public RelayCommand DownloadCommand { get; }

    /// <summary>Prompts the user to locate a .zip and installs it.</summary>
    public RelayCommand LocateInstallCommand { get; }

    // ──────────────────────────────────────────────
    //  Constructor
    // ──────────────────────────────────────────────

    public LibraryViewModel(ConfigService configService, NavigationService navigationService)
    {
        _configService = configService;

        DownloadCommand = new RelayCommand(OpenDownloadPage);
        LocateInstallCommand = new RelayCommand(async p => await LocateAndInstallAsync(p));

        // ── Hardcoded mod catalog ──
        Mods.Add(new ModEntry(
            "LSPDFR",
            "Los Santos Police Department First Response — the core police mod for GTA V.",
            7792,
            "https://www.lcpdfr.com/files/file/7792-lspdfr/"));

        Mods.Add(new ModEntry(
            "RagePluginHook",
            "Plugin framework required to load LSPDFR and other script plugins.",
            717,
            "https://www.lcpdfr.com/downloads/gta5mods/misc/717-rage-plugin-hook/"));

        Mods.Add(new ModEntry(
            "Emergency Lighting System",
            "Advanced emergency vehicle lighting with realistic patterns and sirens.",
            13865,
            "https://www.lcpdfr.com/downloads/gta5mods/scripts/13865-emergency-lighting-system/"));

        // Fire-and-forget: fetch version info from the LCPDFR API
        _ = FetchAllVersionsAsync();
    }

    // ──────────────────────────────────────────────
    //  Version fetching
    // ──────────────────────────────────────────────

    /// <summary>
    /// Queries the LCPDFR API for the latest version of every mod in parallel.
    /// Each card updates individually as its version arrives.
    /// </summary>
    private async Task FetchAllVersionsAsync()
    {
        StatusMessage = "Fetching mod versions…";

        try
        {
            var tasks = Mods.Select(async mod =>
            {
                string version = await _apiService.GetVersionAsync(mod.FileId);
                mod.Version = version;
            });

            await Task.WhenAll(tasks);
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            Logger.Log($"Version fetch error: {ex.Message}");
            StatusMessage = "Some version lookups failed.";
        }
    }

    // ──────────────────────────────────────────────
    //  Download page (opens browser)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Opens the mod's LCPDFR download page in the user's default browser.
    /// The mod is passed via CommandParameter.
    /// </summary>
    private void OpenDownloadPage(object? parameter)
    {
        if (parameter is not ModEntry mod)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = mod.DownloadPageUrl,
                UseShellExecute = true
            });
            Logger.Log($"Opened download page for {mod.Name}");
            StatusMessage = $"Opened download page for {mod.Name}";
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to open URL for {mod.Name}: {ex.Message}");
            StatusMessage = $"Error opening browser: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────
    //  Locate & install
    // ──────────────────────────────────────────────

    /// <summary>
    /// Prompts the user to select a downloaded .zip file, then
    /// validates and installs it into the GTA V folder.
    /// Progress is shown on both the mod card and the global status bar.
    /// </summary>
    private async Task LocateAndInstallAsync(object? parameter)
    {
        if (parameter is not ModEntry mod)
            return;

        // Prevent double-click while installing
        if (mod.IsInstalling)
            return;

        // ── Open file dialog ──
        var dialog = new OpenFileDialog
        {
            Title = $"Locate downloaded ZIP for {mod.Name}",
            Filter = "ZIP archives (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true)
            return;

        string zipPath = dialog.FileName;
        Logger.Log($"Installing {mod.Name} from {zipPath}");

        // ── Install with progress ──
        mod.IsInstalling = true;
        mod.StatusMessage = "Preparing…";

        var progress = new Progress<string>(msg =>
        {
            mod.StatusMessage = msg;
            StatusMessage = $"{mod.Name}: {msg}";
        });

        bool success = await _fileService.InstallModAsync(zipPath, GtaFolderPath, progress);

        mod.IsInstalling = false;

        if (success)
        {
            mod.IsInstalled = true;
            mod.StatusMessage = string.Empty;
            StatusMessage = $"{mod.Name} installed successfully ✓";
            Logger.Log($"{mod.Name} installed successfully from {zipPath}");
        }
        else
        {
            // Error message already set by FileService via progress reporter
            StatusMessage = $"{mod.Name}: installation failed";
        }
    }
}
