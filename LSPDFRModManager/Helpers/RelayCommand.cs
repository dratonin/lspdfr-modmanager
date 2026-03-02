using System.Windows.Input;

namespace LSPDFRModManager.Helpers;

/// <summary>
/// A lightweight ICommand implementation that delegates Execute and CanExecute
/// to lambda expressions. This is the standard way to bind buttons to
/// ViewModel methods in WPF MVVM.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <param name="execute">Action to run when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate that controls whether the command is enabled.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Raised when the command's executability may have changed.
    /// Hooks into the WPF CommandManager so the UI re-queries automatically.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
