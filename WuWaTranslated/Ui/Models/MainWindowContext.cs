using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WuWaTranslated.Ui.Models;

public class MainWindowContext : INotifyPropertyChanged
{
    internal const string DefaultInstallButtonText = "ДА! ( У С Т А Н О В И Т Ь ! )";

    public string Title { get; } = "WutheringWaves translated";

    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    public bool IsNeedToInstall => !IsAppInstalled;

    private bool _isAppInstalled = false;
    public bool IsAppInstalled
    {
        get => _isAppInstalled;
        set
        {
            SetField(ref _isAppInstalled, value);
            OnPropertyChanged(nameof(IsNeedToInstall));
        }
    }

    private bool _isAppUpdatesAvailable = false;
    public bool IsAppUpdatesAvailable
    {
        get => _isAppUpdatesAvailable;
        set => SetField(ref _isAppUpdatesAvailable, value);
    }

    private string _appUpdateSize = "...";
    public string AppUpdateSize
    {
        get => _appUpdateSize;
        set => SetField(ref _appUpdateSize, value);
    }

    private string _appUpdatedAt = "...";
    public string AppUpdatedAt
    {
        get => _appUpdatedAt;
        set => SetField(ref _appUpdatedAt, value);
    }

    private bool _isLangFileInstalled = false;
    public bool IsLangFileInstalled
    {
        get => _isLangFileInstalled;
        set => SetField(ref _isLangFileInstalled, value);
    }

    public string InstallerMessage { get; } = "Установщик русификатора не установлен.\n" +
                                              "Чтобы его установить, нажми кнопку ниже и следуй инструкции.\n" +
                                              "Всё просто, да?";

    private string _installButtonText = DefaultInstallButtonText;
    public string InstallButtonText
    {
        get => _installButtonText;
        set => SetField(ref _installButtonText, value);
    }

    private bool _installButtonEnabled = true;
    public bool InstallButtonEnabled
    {
        get => _installButtonEnabled;
        set => SetField(ref _installButtonEnabled, value);
    }

    private bool _installButtonVisible = true;
    public bool InstallButtonVisible
    {
        get => _installButtonVisible;
        set => SetField(ref _installButtonVisible, value);
    }

    private bool _updateAvailable = false;
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        set
        {
            SetField(ref _updateAvailable, value);
            if (value) IsLangFileInstalled = !value;
        }
    }

    private string _pakInstallerMessage = "Игра не выглядит русифицированной.\n" +
                                          "Тебе руками особо ничего делать не надо (надеюсь).\n" +
                                          "Просто нажми кнопку ниже\n" +
                                          "Всё просто, да?";
    public string PakInstallerMessage
    {
        get => _pakInstallerMessage;
        set => SetField(ref _pakInstallerMessage, value);
    }

    private string _pakFileReleasedAt = "...";
    public string PakFileReleasedAt
    {
        get => _pakFileReleasedAt;
        set => SetField(ref _pakFileReleasedAt, value);
    }

    private string _pakFileSize = "...";
    public string PakFileSize { get => _pakFileSize;
        set => SetField(ref _pakFileSize, value);
    }

    private bool _cancelButtonEnabled = true;
    public bool CancelButtonEnabled
    {
        get => _cancelButtonEnabled;
        set => SetField(ref _cancelButtonEnabled, value);
    }

    private string _progressText = string.Empty;
    public string ProgressText
    {
        get => _progressText;
        set => SetField(ref _progressText, value);
    }

    private double _progress = 0.0;
    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    private bool _progressIndeterminate = true;
    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        set => SetField(ref _progressIndeterminate, value);
    }

    private bool _progressVsible = false;
    public bool ProgressVisible
    {
        get => _progressVsible;
        set => SetField(ref _progressVsible, value);
    }

    private bool _allowLaunch = false;
    public bool AllowLaunch
    {
        get => _allowLaunch;
        set => SetField(ref _allowLaunch, value);
    }

    private bool _launchButtonEnabled = true;
    public bool LaunchButtonEnabled
    {
        get => _launchButtonEnabled;
        set => SetField(ref _launchButtonEnabled, value);
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