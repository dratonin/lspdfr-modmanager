using System.Windows;
using LSPDFRModManager.Helpers;
using LSPDFRModManager.Services;
using LSPDFRModManager.ViewModels;
using LSPDFRModManager.Views;

namespace LSPDFRModManager;

/// <summary>
/// Application shell. Contains a single Frame that hosts either
/// the OnboardingPage (first launch) or the LibraryPage (returning user).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // ── Bootstrap services ──
        var configService = new ConfigService();
        configService.Load();

        var navigationService = new NavigationService();
        navigationService.SetFrame(MainFrame);

        Logger.Log("Application started.");

        // ── Decide which page to show ──
        if (configService.Config.IsFirstLaunch
            || string.IsNullOrEmpty(configService.Config.GtaFolderPath))
        {
            Logger.Log("First launch detected — showing onboarding.");
            var vm = new OnboardingViewModel(configService, navigationService);
            navigationService.NavigateTo(new OnboardingPage { DataContext = vm });
        }
        else
        {
            Logger.Log("Returning user — showing library.");
            var vm = new LibraryViewModel(configService, navigationService);
            navigationService.NavigateTo(new LibraryPage { DataContext = vm });
        }
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
