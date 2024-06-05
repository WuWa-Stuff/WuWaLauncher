using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WuWaTranslated.Ui.Models;

public class AwaitingGameDialogWindowContext : INotifyPropertyChanged
{
    public string Title { get; } = "Ожидаю запуска игры...";

    public string Message { get; } = "Мне необходимо знать, куда установлена Wuthering Waves.\n" +
                                     "Для этого тебе надо запустить игру и дождаться, когда пропадёт это сообщение.\n" +
                                     "Причём надо запустить ИМЕННО игру, а не только её лаунчер!\n" +
                                     "Это нужно для того, чтобы установить файл русификатора и далее запустить игру, чтобы русификатор сработал.\n" +
                                     "\n" +
                                     "Ответственность за твой аккаунт только на тебе!\n" +
                                     "Запуская игру и/или используя это ПО, ты соглашаешься с вышенаписанным.\n" +
                                     "\n" +
                                     "Ожидаю запуск игры...";

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