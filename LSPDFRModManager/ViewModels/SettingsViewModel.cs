using LSPDFRModManager.Helpers;
using LSPDFRModManager.Services;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Lets the user view and change the GTA V directory path.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NavigationService _navigationService;

    private string _gtaFolderPath;
    private bool _isFolderValid;
    private string _validationMessage = string.Empty;

    /// <summary>Current GTA V folder path (editable).</summary>
    public string GtaFolderPath
    {
        get => _gtaFolderPath;
        set
        {
            if (SetProperty(ref _gtaFolderPath, value))
                Validate();
        }
    }

    /// <summary>True if the current path contains GTA5.exe.</summary>
    public bool IsFolderValid
    {
        get => _isFolderValid;
        private set => SetProperty(ref _isFolderValid, value);
    }

    /// <summary>Validation feedback message.</summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand BrowseCommand { get; }

    public SettingsViewModel(ConfigService configService, NavigationService navigationService)
    {
        _configService = configService;
        _navigationService = navigationService;
        _gtaFolderPath = configService.Config.GtaFolderPath;

        SaveCommand = new RelayCommand(_ => Save(), _ => IsFolderValid);
        BackCommand = new RelayCommand(_ => _navigationService.GoBack());
        BrowseCommand = new RelayCommand(_ => Browse());

        Validate();
    }

    private void Validate()
    {
        IsFolderValid = GtaFolderValidator.IsValid(GtaFolderPath);
        ValidationMessage = string.IsNullOrWhiteSpace(GtaFolderPath)
            ? string.Empty
            : IsFolderValid
                ? "\u2713 GTA5.exe found"
                : "\u2717 GTA5.exe not found in this directory";
    }

    private void Save()
    {
        _configService.Config.GtaFolderPath = GtaFolderPath;
        _configService.Save();
        Logger.Log($"GTA folder updated to: {GtaFolderPath}");
        _navigationService.GoBack();
    }

    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Locate GTA5.exe",
            Filter = "GTA5.exe|GTA5.exe",
            FileName = "GTA5.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            GtaFolderPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }
}
