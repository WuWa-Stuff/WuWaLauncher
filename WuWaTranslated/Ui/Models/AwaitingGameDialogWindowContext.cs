using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WuWaTranslated.Ui.Models;

public class AwaitingGameDialogWindowContext : INotifyPropertyChanged
{
    public string Title { get; } = "Слыш, игру запусти";

    public string Message { get; } = "Ожидаю запуска игры...";

    public string CancelButtonText { get; } = "Отмена";

    private bool _cancelButtonEnabled = true;
    public bool CancelButtonEnabled
    {
        get => _cancelButtonEnabled;
        set => SetField(ref _cancelButtonEnabled, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}