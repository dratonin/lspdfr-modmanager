using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LSPDFRModManager.ViewModels;

/// <summary>
/// Base class for all ViewModels.
/// Provides a clean INotifyPropertyChanged implementation with a
/// helper method <see cref="SetProperty{T}"/> that only raises
/// the event when the value actually changes.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises PropertyChanged for the calling property.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// PropertyChanged if the value actually changed.
    /// Returns <c>true</c> when the value was updated.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
