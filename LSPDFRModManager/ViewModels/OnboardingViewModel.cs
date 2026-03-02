using System.Diagnostics;
using LSPDFRModManager.Helpers;
using LSPDFRModManager.Services;
using LSPDFRModManager.Views;
using Microsoft.Win32;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// ViewModel for the onboarding flow shown on first launch.
/// Guides the user through selecting a GTA V folder and confirming
/// that LSPDFR has been installed.
/// </summary>
public sealed class OnboardingViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NavigationService _navigationService;

    // ──────────────────────────────────────────────
    //  Backing fields
    // ──────────────────────────────────────────────

    private string _gtaFolderPath = string.Empty;
    private bool _isFolderValid;
    private bool _hasInstalledLspdfr;
    private string _validationMessage = string.Empty;
    private bool _hasValidationMessage;

    // ──────────────────────────────────────────────
    //  Bindable properties
    // ──────────────────────────────────────────────

    /// <summary>Path chosen by the user (may or may not be valid).</summary>
    public string GtaFolderPath
    {
        get => _gtaFolderPath;
        private set
        {
            if (SetProperty(ref _gtaFolderPath, value))
                OnPropertyChanged(nameof(CanContinue));
        }
    }

    /// <summary>True when the selected folder contains GTA5.exe.</summary>
    public bool IsFolderValid
    {
        get => _isFolderValid;
        private set
        {
            if (SetProperty(ref _isFolderValid, value))
                OnPropertyChanged(nameof(CanContinue));
        }
    }

    /// <summary>Checked by the user to confirm LSPDFR is installed.</summary>
    public bool HasInstalledLspdfr
    {
        get => _hasInstalledLspdfr;
        set
        {
            if (SetProperty(ref _hasInstalledLspdfr, value))
                OnPropertyChanged(nameof(CanContinue));
        }
    }

    /// <summary>
    /// True when GTA5.exe is detected in the selected folder.
    /// Controls whether the "Continue" button is enabled.
    /// </summary>
    public bool CanContinue => IsFolderValid;

    /// <summary>Inline validation text shown below the folder input.</summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>True once the user has attempted folder selection (drives visibility).</summary>
    public bool HasValidationMessage
    {
        get => _hasValidationMessage;
        private set => SetProperty(ref _hasValidationMessage, value);
    }

    // ──────────────────────────────────────────────
    //  Commands
    // ──────────────────────────────────────────────

    public RelayCommand SelectFolderCommand { get; }
    public RelayCommand OpenLspdfrWebsiteCommand { get; }
    public RelayCommand ContinueCommand { get; }

    // ──────────────────────────────────────────────
    //  Constructor
    // ──────────────────────────────────────────────

    public OnboardingViewModel(ConfigService configService, NavigationService navigationService)
    {
        _configService = configService;
        _navigationService = navigationService;

        SelectFolderCommand = new RelayCommand(_ => SelectFolder());
        OpenLspdfrWebsiteCommand = new RelayCommand(_ => OpenLspdfrWebsite());
        ContinueCommand = new RelayCommand(_ => Continue(), _ => CanContinue);
    }

    // ──────────────────────────────────────────────
    //  Command implementations
    // ──────────────────────────────────────────────

    private void SelectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select your GTA V installation folder"
        };

        if (dialog.ShowDialog() != true)
            return;

        string path = dialog.FolderName;
        GtaFolderPath = path;

        if (GtaFolderValidator.IsValid(path))
        {
            IsFolderValid = true;
            ValidationMessage = "✓ GTA5.exe gefunden — Ordner ist gültig.";
            HasValidationMessage = true;
            Logger.Log($"Onboarding: valid GTA V folder selected — {path}");
        }
        else
        {
            IsFolderValid = false;
            ValidationMessage = "✗ GTA5.exe wurde nicht gefunden.";
            HasValidationMessage = true;
            Logger.Log($"Onboarding: invalid folder selected — {path}");
        }
    }

    /// <summary>Opens the official LSPDFR website in the default browser.</summary>
    private static void OpenLspdfrWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.lcpdfr.com/lspdfr/",
                UseShellExecute = true
            });
            Logger.Log("Onboarding: opened LSPDFR website.");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to open LSPDFR website: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the config, marks first-launch as done, and navigates to the library.
    /// </summary>
    private void Continue()
    {
        _configService.Config.GtaFolderPath = GtaFolderPath;
        _configService.Config.IsFirstLaunch = false;
        _configService.Save();

        Logger.Log("Onboarding complete — navigating to library.");

        var vm = new LibraryViewModel(_configService, _navigationService);
        var page = new LibraryPage { DataContext = vm };
        _navigationService.NavigateTo(page);
    }
}
