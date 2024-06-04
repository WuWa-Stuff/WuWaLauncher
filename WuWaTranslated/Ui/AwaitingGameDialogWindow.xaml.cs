using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management;
using System.Windows;
using Wpf.Ui.Controls;
using WuWaTranslated.TaskState;
using WuWaTranslated.Ui.Models;

namespace WuWaTranslated.Ui;

public partial class AwaitingGameDialogWindow : IDisposable, IAsyncDisposable
{
    private readonly AwaitingGameDialogWindowContext _context;
    private readonly Timer _timer;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public TaskResult<string> Result { get; private set; } = TaskResult<string>.Cancelled;

    public AwaitingGameDialogWindow()
    {
        _context = new AwaitingGameDialogWindowContext();

        InitializeComponent();
        DataContext = _context;

        var delay = TimeSpan.FromSeconds(1);
        _timer = new Timer(TimerCallbackHandler, default, delay, delay);
    }

    private bool GetGameExePath([NotNullWhen(true)] out string? exePath)
    {
        const string query = "SELECT ExecutablePath, ProcessId FROM Win32_Process";
        var searcher = new ManagementObjectSearcher(query);

        exePath = default;

        foreach (var item in searcher.Get())
        {
            uint? pid = item["ProcessId"] as uint?;
            string? path = item["ExecutablePath"] as string;
            
            if (string.IsNullOrEmpty(path) || !pid.HasValue)
                continue;

            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.Equals("Client-Win64-Shipping", StringComparison.InvariantCultureIgnoreCase))
                continue;

            string procWindowName;
            try
            {
                var procInfo = Process.GetProcessById((int)pid.Value);
                procWindowName = procInfo.MainWindowTitle;
            }
            catch
            {
                continue;
            }
            
            if (!procWindowName.StartsWith("Wuthering Waves", StringComparison.InvariantCultureIgnoreCase))
                continue;

            exePath = path;
            return true;
        }

        return false;
    }

    private void TimerCallbackHandler(object? state)
    {
        _semaphoreSlim.Wait();

        string? gameExePath;
        try
        {
            if (!GetGameExePath(out gameExePath))
                return;
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        try
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        catch { /* do nothing */ }

        var dialogResult = false;
        try
        {
            if (string.IsNullOrEmpty(gameExePath))
            {
                Result = new TaskResult<string>("Не могу узнать путь до игры...");
                return;
            }

            var gameExeDir = Path.GetDirectoryName(gameExePath);
            if (string.IsNullOrEmpty(gameExeDir))
            {
                Result = new TaskResult<string>("Не могу найти путь до папки...");
                return;
            }

            var installationDir = new DirectoryInfo(gameExeDir).Parent?.Parent?.Parent;
            if (installationDir is null)
            {
                Result = new TaskResult<string>("Не могу найти путь до папки установки...");
                return;
            }

            Result = installationDir.FullName;
            dialogResult = true;
        }
        catch (Exception e)
        {
            Result = new TaskResult<string>($"Не могу узнать путь до игры: {e.Message}", e);
        }
        finally
        {
            Dispatcher.Invoke(() => DialogResult = dialogResult);
        }
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
    }
}