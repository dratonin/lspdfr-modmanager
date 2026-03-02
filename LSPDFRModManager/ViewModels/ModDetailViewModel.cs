using System.Diagnostics;
using LSPDFRModManager.Helpers;
using LSPDFRModManager.Models;
using LSPDFRModManager.Services;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// ViewModel for the mod detail page.
/// Shows full description, notes, installation guide, and credits.
/// </summary>
public sealed class ModDetailViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    /// <summary>The mod being displayed.</summary>
    public ModEntry Mod { get; }

    /// <summary>Navigates back to the library page.</summary>
    public RelayCommand BackCommand { get; }

    /// <summary>Opens the mod's download page in the browser.</summary>
    public RelayCommand DownloadCommand { get; }

    public ModDetailViewModel(ModEntry mod, NavigationService navigationService)
    {
        Mod = mod;
        _navigationService = navigationService;

        BackCommand = new RelayCommand(_ => _navigationService.GoBack());
        DownloadCommand = new RelayCommand(_ => OpenDownloadPage());
    }

    private void OpenDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Mod.DownloadPageUrl,
                UseShellExecute = true
            });
            Logger.Log($"Opened download page for {Mod.Name}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to open URL for {Mod.Name}: {ex.Message}");
        }
    }
}
