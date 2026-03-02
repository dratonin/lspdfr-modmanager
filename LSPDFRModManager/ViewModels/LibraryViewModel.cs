using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using LSPDFRModManager.Helpers;
using LSPDFRModManager.Models;
using LSPDFRModManager.Services;
using LSPDFRModManager.Views;
using Microsoft.Win32;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// ViewModel for the Mod Library screen.
/// Displays available mods grouped by category with version info from the LCPDFR API,
/// lets the user open download pages, navigate to detail pages, and locate + install archives.
/// </summary>
public sealed class LibraryViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NavigationService _navigationService;
    private readonly FileService _fileService = new();
    private readonly LcpdfrApiService _apiService = new();

    private string _statusMessage = "Ready";
    private string _searchText = string.Empty;

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

    /// <summary>Search text used to filter the mod list.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ModsView.Refresh();
        }
    }

    /// <summary>Full collection of available mods.</summary>
    public ObservableCollection<ModEntry> Mods { get; } = [];

    /// <summary>Grouped and filtered view bound by the UI.</summary>
    public ICollectionView ModsView { get; }

    // ──────────────────────────────────────────────
    //  Commands
    // ──────────────────────────────────────────────

    /// <summary>Opens the mod's LCPDFR download page in the default browser.</summary>
    public RelayCommand DownloadCommand { get; }

    /// <summary>Prompts the user to locate an archive and installs it.</summary>
    public RelayCommand LocateInstallCommand { get; }

    /// <summary>Navigates to the detail page for a specific mod.</summary>
    public RelayCommand ViewDetailCommand { get; }

    /// <summary>Navigates to the settings page.</summary>
    public RelayCommand OpenSettingsCommand { get; }

    // ──────────────────────────────────────────────
    //  Constructor
    // ──────────────────────────────────────────────

    public LibraryViewModel(ConfigService configService, NavigationService navigationService)
    {
        _configService = configService;
        _navigationService = navigationService;

        DownloadCommand = new RelayCommand(OpenDownloadPage);
        LocateInstallCommand = new RelayCommand(async p => await LocateAndInstallAsync(p));
        ViewDetailCommand = new RelayCommand(NavigateToDetail);
        OpenSettingsCommand = new RelayCommand(_ => NavigateToSettings());

        // ── Mod catalog grouped by category ──
        PopulateModCatalog();

        // ── Set up grouped + filtered collection view ──
        ModsView = CollectionViewSource.GetDefaultView(Mods);
        ModsView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        ModsView.Filter = FilterMods;

        // Fire-and-forget: fetch version info from the LCPDFR API
        _ = FetchAllVersionsAsync();
    }

    private bool FilterMods(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        if (item is not ModEntry mod)
            return false;

        return mod.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || mod.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || mod.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────
    //  Mod catalog
    // ──────────────────────────────────────────────

    private void PopulateModCatalog()
    {
        // ── Essential Mods ──
        Mods.Add(new ModEntry(
            "LSPDFR",
            "Los Santos Police Department First Response \u2014 the core police mod for GTA V.",
            "Essential Mods",
            7792,
            "https://www.lcpdfr.com/files/file/7792-lspdfr/",
            detailDescription:
                "LSPDFR (LSPD First Response) is the premier police modification for Grand Theft Auto V.\n" +
                "It transforms the game into a law enforcement simulator, allowing players to patrol\n" +
                "the streets of Los Santos as a police officer, conduct traffic stops, pursue suspects,\n" +
                "and respond to emergency calls.",
            notes:
                "Requires a legal copy of GTA V.\n" +
                "Not compatible with GTA Online \u2014 use only in Story Mode.\n" +
                "Requires RagePluginHook to load.",
            installationGuide:
                "1. Install RagePluginHook first.\n" +
                "2. Extract the LSPDFR archive into your GTA V root directory.\n" +
                "3. Launch the game via RagePluginHook.exe.",
            credits: "Developed by the LCPDFR.com team."));

        Mods.Add(new ModEntry(
            "RagePluginHook",
            "Plugin framework required to load LSPDFR and other script plugins.",
            "Essential Mods",
            717,
            "https://www.lcpdfr.com/downloads/gta5mods/misc/717-rage-plugin-hook/",
            detailDescription:
                "RagePluginHook (RPH) is the foundational plugin loader for GTA V modding.\n" +
                "It provides a managed .NET API that allows developers to create plugins\n" +
                "that interact with the game engine. LSPDFR and most police plugins require RPH.",
            notes:
                "Must be updated when GTA V receives a game update.\n" +
                "Run RagePluginHook.exe instead of GTA5.exe to load plugins.",
            installationGuide:
                "1. Extract all files into your GTA V root directory.\n" +
                "2. Ensure RagePluginHook.exe, RagePluginHook.dll, and RAGENativeUI.dll are present.\n" +
                "3. Launch the game via RagePluginHook.exe.",
            credits: "Developed by MulleDK19."));

        // ── Scripts & Plugins ──
        Mods.Add(new ModEntry(
            "Emergency Lighting System",
            "Advanced emergency vehicle lighting with realistic patterns and sirens.",
            "Scripts & Plugins",
            13865,
            "https://www.lcpdfr.com/downloads/gta5mods/scripts/13865-emergency-lighting-system/",
            detailDescription:
                "Emergency Lighting System V brings one of the most popular modifications ever created\n" +
                "for a GTA title (ELS-IV) to Grand Theft Auto V. ELS-V will add a whole new dimension to\n" +
                "your patrols. With a fresh look and many more features than its predecessor, it's also sure\n" +
                "to brighten up your game and satisfy the emergency lighting enthusiast in anyone.\n\n" +
                "\u2022 ELS provides an alternate and incredibly in-depth way of controlling emergency vehicle lights and sounds.\n" +
                "\u2022 ELS requires vehicle models specifically designed to make use of its features.\n" +
                "\u2022 ELS includes over 200 unique lighting patterns scattered across 4 light groups and lighting formats.",
            notes:
                "Please consult the ELS USER GUIDE and other provided documentation prior to using ELS.\n" +
                "As of V1.00 ELS is not compatible with any form of multiplayer.\n" +
                "A visual enhancement mod is recommended to improve the appearance of ELS lights in game.",
            installationGuide:
                "A GTA V ScriptHook must be installed for any .asi modification to work.\n\n" +
                "1. Place ELS.asi, ELS.ini, the ELS folder, and AdvancedHookV.dll inside your main GTA V game directory.\n" +
                "2. Download and install pro-ELS vehicle model(s).\n" +
                "3. Start your game.\n\n" +
                "WARNING: Using script modifications in GTA Online can result in a temporary or permanent ban.",
            credits:
                "ELS is coded by Lt.Caine.\n" +
                "AdvancedHookV and vehicle damage feature coded by LMS.\n" +
                "ELS-V testing team: Albo1125, BxBugs123, GravelRoadCop, PoliceWag, and Prophet."));

        Mods.Add(new ModEntry(
            "StopThePed",
            "Enhanced traffic stop and pedestrian interaction plugin for LSPDFR.",
            "Scripts & Plugins",
            28647,
            "https://www.lcpdfr.com/downloads/gta5mods/scripts/28647-stoptheped/",
            detailDescription:
                "StopThePed enhances LSPDFR's traffic stop and pedestrian interaction systems.\n" +
                "It adds realistic options like asking for ID, checking warrants, searching vehicles,\n" +
                "issuing citations, performing DUI tests, and much more.",
            notes:
                "Requires LSPDFR and RagePluginHook.\n" +
                "Check for updates after each LSPDFR version change.",
            installationGuide:
                "1. Ensure LSPDFR and RagePluginHook are installed.\n" +
                "2. Extract StopThePed files into your GTA V Plugins/LSPDFR folder.\n" +
                "3. Launch via RagePluginHook.exe.",
            credits: "Developed by BejoIjo."));

        Mods.Add(new ModEntry(
            "Ultimate Backup",
            "Realistic AI backup dispatch system for LSPDFR patrols.",
            "Scripts & Plugins",
            28583,
            "https://www.lcpdfr.com/downloads/gta5mods/scripts/28583-ultimate-backup/",
            detailDescription:
                "Ultimate Backup provides a complete overhaul of the LSPDFR backup system.\n" +
                "Call for specific unit types including K-9, SWAT, helicopters, and more.\n" +
                "Customize response vehicles and officer loadouts.",
            notes:
                "Requires LSPDFR 0.4.9+ and RagePluginHook.\n" +
                "Configure unit types in the provided XML files.",
            installationGuide:
                "1. Extract into the GTA V Plugins/LSPDFR folder.\n" +
                "2. Edit the XML configuration files to customize units.\n" +
                "3. Launch the game via RagePluginHook.exe.",
            credits: "Developed by BejoIjo."));

        // ── Vehicle Mods ──
        Mods.Add(new ModEntry(
            "LSPD Vehicle Pack",
            "High-quality ELS-ready police vehicle models for Los Santos PD.",
            "Vehicle Mods",
            15248,
            "https://www.lcpdfr.com/downloads/gta5mods/vehiclemodels/15248-lspd-vehicle-pack/",
            detailDescription:
                "A comprehensive pack of custom police vehicle models designed for LSPD patrols.\n" +
                "All vehicles are ELS-compatible with detailed liveries and accurate light bars.\n" +
                "Includes patrol cars, SUVs, and unmarked vehicles.",
            notes:
                "Requires OpenIV for vehicle model installation.\n" +
                "ELS-compatible vehicles require ELS to be installed.",
            installationGuide:
                "1. Open OpenIV and enable Edit Mode.\n" +
                "2. Navigate to the appropriate vehicle folder.\n" +
                "3. Replace or add the vehicle model files.\n" +
                "4. Install the ELS configuration files in the ELS folder.",
            credits: "Various community vehicle modelers."));

        // ── Character ──
        Mods.Add(new ModEntry(
            "EUP Law Enforcement Pack",
            "Realistic law enforcement uniforms and equipment for player characters.",
            "Character",
            23080,
            "https://www.lcpdfr.com/downloads/gta5mods/character/23080-eup-law-enforcement-pack/",
            detailDescription:
                "EUP (Emergency Uniforms Pack) provides a vast collection of realistic law enforcement\n" +
                "uniforms, equipment, and accessories. Dress your character in authentic police,\n" +
                "sheriff, state trooper, and federal agent outfits.",
            notes:
                "Requires EUP Menu or a compatible outfit manager.\n" +
                "OpenIV is required for installation of uniform models.",
            installationGuide:
                "1. Install the base EUP files using OpenIV.\n" +
                "2. Install the EUP Menu plugin into the LSPDFR Plugins folder.\n" +
                "3. Configure outfit presets in the EUP menu in-game.",
            credits: "Developed by Alex_Ashfold."));

        // ── Audio and Visual ──
        Mods.Add(new ModEntry(
            "Realistic Sirens & Lighting",
            "Authentic emergency siren sounds and improved lighting effects.",
            "Audio and Visual",
            19657,
            "https://www.lcpdfr.com/downloads/gta5mods/audio/19657-realistic-sirens/",
            detailDescription:
                "Replaces the default GTA V emergency sirens with high-quality recordings of real\n" +
                "emergency vehicle sirens used by US law enforcement agencies. Includes yelp, wail,\n" +
                "hi-lo, and air horn variants.",
            notes:
                "Audio mods require OpenIV for installation.\n" +
                "Back up your original audio files before replacing.",
            installationGuide:
                "1. Open OpenIV and enable Edit Mode.\n" +
                "2. Navigate to the audio folder (x64/audio/sfx).\n" +
                "3. Replace the siren .awc files with the modded versions.\n" +
                "4. Restart the game.",
            credits: "Various community audio modders."));

        Mods.Add(new ModEntry(
            "VisualV",
            "Comprehensive visual enhancement overhaul for GTA V graphics.",
            "Audio and Visual",
            11736,
            "https://www.lcpdfr.com/downloads/gta5mods/misc/11736-visualv/",
            detailDescription:
                "VisualV is a major graphics overhaul that improves weather, lighting, tonemapping,\n" +
                "and overall visual fidelity of GTA V. It enhances the atmosphere and makes the\n" +
                "game look significantly more realistic and cinematic.",
            notes:
                "May impact performance on lower-end systems.\n" +
                "Compatible with most other mods but may conflict with other visual mods.",
            installationGuide:
                "1. Extract VisualV files into your GTA V root directory.\n" +
                "2. Use the included installer or manually place the files.\n" +
                "3. Adjust settings via the provided configuration files.",
            credits: "Developed by _CP_."));
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
        StatusMessage = "Fetching mod versions\u2026";

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
    //  View detail page
    // ──────────────────────────────────────────────

    private void NavigateToDetail(object? parameter)
    {
        if (parameter is not ModEntry mod)
            return;

        var vm = new ModDetailViewModel(mod, _navigationService);
        _navigationService.NavigateTo(new ModDetailPage { DataContext = vm });
    }

    // ──────────────────────────────────────────────
    //  Settings page
    // ──────────────────────────────────────────────

    private void NavigateToSettings()
    {
        var vm = new SettingsViewModel(_configService, _navigationService);
        _navigationService.NavigateTo(new SettingsPage { DataContext = vm });
    }

    // ──────────────────────────────────────────────
    //  Locate & install
    // ──────────────────────────────────────────────

    /// <summary>
    /// Prompts the user to select a downloaded archive file, then
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
            Title = $"Locate downloaded archive for {mod.Name}",
            Filter = "Archives (*.zip;*.rar)|*.zip;*.rar|ZIP archives (*.zip)|*.zip|RAR archives (*.rar)|*.rar"
        };

        if (dialog.ShowDialog() != true)
            return;

        string archivePath = dialog.FileName;
        Logger.Log($"Installing {mod.Name} from {archivePath}");

        // ── Install with progress ──
        mod.IsInstalling = true;
        mod.StatusMessage = "Preparing\u2026";

        var progress = new Progress<string>(msg =>
        {
            mod.StatusMessage = msg;
            StatusMessage = $"{mod.Name}: {msg}";
        });

        bool success = await _fileService.InstallModAsync(archivePath, GtaFolderPath, progress);

        mod.IsInstalling = false;

        if (success)
        {
            mod.IsInstalled = true;
            mod.StatusMessage = string.Empty;
            StatusMessage = $"{mod.Name} installed successfully \u2713";
            Logger.Log($"{mod.Name} installed successfully from {archivePath}");
        }
        else
        {
            StatusMessage = $"{mod.Name}: installation failed";
        }
    }
}
