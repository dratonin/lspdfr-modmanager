using System.Windows.Controls;
using LSPDFRModManager.Helpers;

namespace LSPDFRModManager.Services;

/// <summary>
/// Lightweight navigation service that drives a WPF <see cref="Frame"/>.
/// Pages are navigated programmatically — the built-in WPF navigation
/// chrome is hidden so the app feels like a modern launcher.
/// </summary>
public sealed class NavigationService
{
    private Frame? _frame;

    /// <summary>
    /// Binds this service to a specific Frame control (called once at startup).
    /// </summary>
    public void SetFrame(Frame frame) => _frame = frame;

    /// <summary>
    /// Navigates the frame to the given <paramref name="page"/>.
    /// </summary>
    public void NavigateTo(Page page)
    {
        if (_frame is null)
        {
            Logger.Log("NavigationService: Frame has not been set.");
            return;
        }

        _frame.Navigate(page);
        Logger.Log($"Navigated to {page.GetType().Name}.");
    }

    /// <summary>
    /// Navigates back to the previous page in the frame's journal.
    /// </summary>
    public void GoBack()
    {
        if (_frame is null)
        {
            Logger.Log("NavigationService: Frame has not been set.");
            return;
        }

        if (_frame.CanGoBack)
        {
            _frame.GoBack();
            Logger.Log("Navigated back.");
        }
        else
        {
            Logger.Log("NavigationService: No page to go back to.");
        }
    }
}
