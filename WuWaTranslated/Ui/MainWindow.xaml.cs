using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using Wpf.Ui.Appearance;
using WuWaTranslated.Attributes;
using WuWaTranslated.GithubApi;
using WuWaTranslated.GithubApi.Repos.Models;
using WuWaTranslated.Models.Config;
using WuWaTranslated.TaskState;
using WuWaTranslated.Ui.Models;
using File = System.IO.File;

namespace WuWaTranslated.Ui;

// обосрусь от смеха, если overlink с его командой будут воровать отсюда код.
// но я не жадный, пусть воруют...

// todo: напихать ратников и ссылок на донат
// ^ (для валенков: строка выше - сарказм и шутка)

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private const string TranslationFileName = "pakchunk0-1.0.0-1.1.0-WindowsNoEditor_10000_P.pak";
    private const string GameExeFileName = "Client-Win64-Shipping.exe";

    private const string ReposOwner = "WuWa-Stuff";
    private const string PakFileRepo = "WuWaTexts";
    private const string InstallerRepo = "WuWaLauncher";

    internal const string AppLaunchModeUpdate = "UPDATE";
    internal const string AppLaunchModePostUpdate = "POST-UPDATE";

    private readonly MainWindowContext _context;
    private readonly GithubApiClient _githubApiClient;
    private readonly ConfigHolder _config;

    private readonly string _appDirectory;
    private readonly string _appDataDirectory;
    private readonly ReadOnlyCollection<string> _gameLaunchArgs;

    private string _gameDirectory = string.Empty;
    private string _gamePaksDirectory = string.Empty;
    private string _gameExeFile = string.Empty;
    private string _translationFile = string.Empty;

    private AssetItem? _pakFileOnServer;
    private string _pakShaServer = string.Empty;
    private string _pakShaLocal = string.Empty;

    private AssetItem? _latestVersion;

    private CancellationTokenSource? _installCancellationTokenSource;

    public MainWindow()
    {
        _context = new MainWindowContext();

        _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wuwa_translated");
        var configFilePath = Path.Combine(_appDataDirectory, "config.json");
        _config = new ConfigHolder(configFilePath);

        var gameDirectory = _appDirectory = GetAppDirectory();
#if DEBUG
        var dbgGamePath = Environment.GetEnvironmentVariable("DBG_GAME_PATH");
        if (!string.IsNullOrEmpty(dbgGamePath))
            gameDirectory = Path.Combine(dbgGamePath, ".fake_folder"); // this folder will not exist, it's only for debugging
#endif

        if (!Directory.Exists(_appDataDirectory))
            Directory.CreateDirectory(_appDataDirectory);

#if !SINGLE_FILE_BUILD
        gameDirectory = Directory.GetParent(gameDirectory)?.FullName
                        ?? Path.GetPathRoot(gameDirectory)
                        ?? gameDirectory;
#endif
        ChangeGameDirectory(gameDirectory);

        var launchArgs = new List<string> { "Client", "-fileopenlog" }; // о нэээт... они своровали параметр запуска... (с)
        if (Utilities.ShouldDisableDlss())
            launchArgs.Add("-DisableModule=dlss");
        _gameLaunchArgs = launchArgs.AsReadOnly();

        _githubApiClient = new GithubApiClient();

        InitializeComponent();
        DataContext = _context;

        ApplicationThemeManager.Apply(this);
    }

    private static void HandleError(TaskResult taskResult)
    {
        if (taskResult.State is not TaskState.Enums.TaskState.Failed)
            return;

        MessageBox.Show(taskResult.ErrorMessage ?? "Что-то пошло не так...",
            "ОШИБКА!",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ChangeGameDirectory(string newPath)
    {
        _gameDirectory = newPath;
        _gamePaksDirectory = Path.Combine(_gameDirectory, "Client", "Content", "Paks");
        _gameExeFile = Path.Combine(_gameDirectory, "Client", "Binaries", "Win64", GameExeFileName);
        _translationFile = Path.Combine(_gamePaksDirectory, TranslationFileName);
    }

    private record UpdateInfo(AssetItem PakFile, string Sha256, DateTime ReleaseDate);
    private async Task<TaskResult<UpdateInfo>> FetchUpdateInfo(CancellationToken cancellationToken)
    {
        var releases = await _githubApiClient.Repos.FetchLatestRelease(
            ReposOwner,
            PakFileRepo,
            cancellationToken);

        if (releases.State is not TaskState.Enums.TaskState.Success)
            return releases.To<UpdateInfo>();

        var assets = releases.Content!.Assets;
        var pakFileAsset = assets.FirstOrDefault(x => x.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase));
        if (pakFileAsset is null)
            return new TaskResult<UpdateInfo>("Не могу найти файл языка на сервере...", default);

        var pakFileName = pakFileAsset.Name + ".sha256";
        var hashPakFileAsset = assets.FirstOrDefault(x => x.Name.Equals(pakFileName, StringComparison.OrdinalIgnoreCase));
        if (hashPakFileAsset is null)
            return new TaskResult<UpdateInfo>("Не могу проверить обновление...", default);

        var hashSha = await _githubApiClient.FetchAssetAsString(hashPakFileAsset, cancellationToken);
        if (hashSha.State is not TaskState.Enums.TaskState.Success)
            return hashSha.To<UpdateInfo>();

        return new UpdateInfo(pakFileAsset, hashSha.Content?.Trim() ?? string.Empty, releases.Content!.PublishedAt);
    }

    private async Task<TaskResult> CheckForPakFileUpdates(CancellationToken cancellationToken)
    {
        var updateInfo = await FetchUpdateInfo(cancellationToken);
        if (updateInfo.State is not TaskState.Enums.TaskState.Success)
            return updateInfo;

        _pakShaServer = updateInfo.Content!.Sha256;
        _pakFileOnServer = updateInfo.Content!.PakFile;

        Dispatcher.Invoke(
            () => _context.PakFileReleasedAt = updateInfo.Content!.ReleaseDate.ToString("dd.MM.yyyy HH:mm"));
        Dispatcher.Invoke(
            () => _context.PakFileSize = Utilities.ByteSizeToHumanReadable(_pakFileOnServer.Size, default));

        return TaskState.Enums.TaskState.Success;
    }

    private static string GetAppDirectory()
        => Path.GetDirectoryName(Utilities.GetCurrentExecutable()) ?? Environment.CurrentDirectory;

    private async Task<TaskResult<string>> FetchGameDirectory()
    {
        await using var awaitingDialog = new AwaitingGameDialogWindow();
        awaitingDialog.Owner = this;
        awaitingDialog.ShowDialog();
        return awaitingDialog.Result;
    }

    private async Task<TaskResult> Install()
    {
        var gameDirectoryResult = await FetchGameDirectory();
        if (gameDirectoryResult.State is not TaskState.Enums.TaskState.Success)
            return gameDirectoryResult;

        var gameDirectory = gameDirectoryResult.Content!;
        return await _config.EditConfig(c => c.GameDirectory = gameDirectory);
    }

    private bool FindLauncherReleaseFile(AssetItem item)
    {
#if SINGLE_FILE_BUILD
        const string ext = ".exe";
#else
        const string ext = ".zip";
#endif

        var fileExt = Path.GetExtension(item.Name);
        return fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TaskResult<bool>> CheckForLauncherUpdates(CancellationToken cancellationToken)
    {
        var latestRelease = await _githubApiClient.Repos.FetchLatestRelease(
            ReposOwner,
            InstallerRepo,
            cancellationToken).ConfigureAwait(false);

        if (latestRelease.State is not TaskState.Enums.TaskState.Success)
            return latestRelease.To<bool>();

        if (latestRelease.Content!.PublishedAt < BuildInfoAttribute.Instance.BuildTime.Add(TimeSpan.FromMinutes(10)))
            return false; // means that there is no any updates

        _latestVersion = latestRelease.Content!.Assets.FirstOrDefault(FindLauncherReleaseFile);
        if (_latestVersion is null)
            return new TaskResult<bool>("Обновление есть, но не могу найти файл обновления.", default);

        _context.AppUpdatedAt = latestRelease.Content!.PublishedAt.ToString("dd.MM.yyyy HH:mm");
        _context.AppUpdateSize = Utilities.ByteSizeToHumanReadable(_latestVersion.Size, default);
        return true; // means that we have an update
    }

    private async Task<TaskResult> InstallAppUpdate(CancellationToken cancellationToken)
    {
        if (_latestVersion is null)
            return new TaskResult("Что-то пошло не так, перезапусти лаунчер.");

        var tempFile = await DownloadAssetFile(_latestVersion,
            _latestVersion.Name,
            cancellationToken);

        if (tempFile.State is not TaskState.Enums.TaskState.Success)
            return tempFile;

        var currentProcPath = Utilities.GetCurrentExecutable();

        // ReSharper disable once RedundantAssignment
        var tempExeToLaunch = tempFile.Content!;
#if !SINGLE_FILE_BUILD
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "wuwalauncher");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);

            using (var zip = ZipFile.OpenRead(tempFile.Content!))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var targetName = Path.GetFullPath(entry.FullName, tempDir);
                    var targetPath = Path.GetDirectoryName(targetName)!;
                    if (!Directory.Exists(targetPath))
                        Directory.CreateDirectory(targetPath);

                    await using var tmpFile = File.Open(targetName, FileMode.Create, FileAccess.Write, FileShare.None);
                    await using var entFile = entry.Open();
                    await entFile.CopyToAsync(tmpFile, cancellationToken);
                }
            }

            File.Delete(tempFile.Content!);

            var currentExeName = Path.GetFileName(currentProcPath);
            tempExeToLaunch = Path.Combine(tempDir, currentExeName);
            if (!File.Exists(tempExeToLaunch))
                return new TaskResult("Не удалось распаковать обновление лаунчера: Не найден исполняемый файл лаунчера.");
        }
        catch (OperationCanceledException)
        {
            return TaskState.Enums.TaskState.Cancelled;
        }
        catch (Exception e)
        {
            return new TaskResult($"Не удалось распаковать обновление лаунчера: {e.Message}", e);
        }
#endif

        try
        {
            var currentProcId = Environment.ProcessId.ToString();
            var procStartInfo = new ProcessStartInfo(tempExeToLaunch,
                [
                    AppLaunchModeUpdate,
                    currentProcId,
                    currentProcPath
                ]);
            var proc = Process.Start(procStartInfo);
            if (proc is null)
                return new TaskResult("Не удалось запустить установку обновления.");

            Environment.Exit(0);
            return TaskState.Enums.TaskState.Success;
        }
        catch (Exception e)
        {
            return new TaskResult($"Не удалось установить обновление лаунчера: {e.Message}", e);
        }
    }

    private async void ButtonInstallAppUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        _installCancellationTokenSource ??= new CancellationTokenSource();
        if (_installCancellationTokenSource.IsCancellationRequested)
            return;

        _context.InstallButtonVisible = false;

        _context.CancelButtonEnabled = true;
        _context.ProgressText = string.Empty;
        _context.Progress = 0.0d;
        _context.ProgressIndeterminate = true;
        _context.ProgressVisible = true;

        var result = await InstallAppUpdate(_installCancellationTokenSource.Token).ConfigureAwait(false);
        HandleError(result);

        _context.ProgressVisible = false;
        _context.InstallButtonVisible = true;
        _context.InstallButtonEnabled = true;

        if (result.State is TaskState.Enums.TaskState.Success)
            _context.IsAppUpdatesAvailable = false;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _config.Load();
            HandleError(result);

            var config = await _config.Get();

            _context.IsAppInstalled = Directory.Exists(_gamePaksDirectory);
            if (_context.IsAppInstalled)
            {
                await _config.EditConfig(c => c.GameDirectory = _gameDirectory);
            }
            else if (!string.IsNullOrEmpty(config.GameDirectory))
            {
                ChangeGameDirectory(config.GameDirectory);
            }

            _context.IsAppInstalled = Directory.Exists(_gamePaksDirectory);
            if (!_context.IsAppInstalled)
                return;

            var bResult = await CheckForLauncherUpdates(default).ConfigureAwait(false);
            if (bResult.State is TaskState.Enums.TaskState.Success && bResult.Content)
            {
                _context.IsAppUpdatesAvailable = true;
                return;
            }
            else
            {
                HandleError(bResult);
            }

            result = await CheckForPakFileUpdates(default).ConfigureAwait(false);
            if (result.State is TaskState.Enums.TaskState.Cancelled)
                return;

            if (result.State is TaskState.Enums.TaskState.Failed)
            {
                _context.PakInstallerMessage = result.ErrorMessage ?? "Что-то пошло не так во время проверки обновления.";
                _context.InstallButtonEnabled = false;
                _context.InstallButtonVisible = false;
                _context.UpdateAvailable = true;
                return;
            }

            _context.IsLangFileInstalled = File.Exists(_translationFile);
            if (!_context.IsLangFileInstalled)
            {
                _context.UpdateAvailable = true;
            }
            else
            {
                var localSha = await Utilities.CalculateSha256OfFile(_translationFile, default)
                    .ConfigureAwait(false);

                if (localSha.State is not TaskState.Enums.TaskState.Success)
                {
                    HandleError(result);
                    Environment.Exit(1);
                    return;
                }

                _pakShaLocal = localSha.Content!;
                _context.UpdateAvailable = !_pakShaServer.Equals(_pakShaLocal, StringComparison.Ordinal);
                if (_context.UpdateAvailable)
                {
                    _context.PakInstallerMessage = "Доступно обновление русского языка в игре.\n" +
                                                   "Тебе руками особо ничего делать не надо (надеюсь).\n" +
                                                   "Просто нажми кнопку ниже\n" +
                                                   "Всё просто, да?";
                }
                else
                {
                    _context.AllowLaunch = true;
                }
            }
        }
        finally
        {
            _context.IsLoading = false;
        }
    }

    private async void ButtonInstall_OnClick(object sender, RoutedEventArgs e)
    {
        _context.InstallButtonEnabled = false;
        _context.InstallButtonText = "Устанавливаем...";

        try
        {
            var installResult = await Install().ConfigureAwait(false);
            HandleError(installResult);
        }
        finally
        {
            _context.InstallButtonText = MainWindowContext.DefaultInstallButtonText;
            _context.InstallButtonEnabled = true;
        }
    }

    private void DownloadProgressReport(long current, long? total)
    {
        if (total > 0.0d)
        {
            var progressPercent = Math.Clamp(Math.Ceiling((current / (decimal)total) * 100), 0, 100);
            _context.ProgressIndeterminate = false;
            _context.Progress = (double)progressPercent;
            _context.ProgressText = $"{(int)progressPercent}%";
        }
        else
        {
            _context.ProgressIndeterminate = true;
            _context.ProgressText = Utilities.ByteSizeToHumanReadable(current, default);
        }
    }

    private async Task<TaskResult<string>> DownloadAssetFile(
        AssetItem asset,
        string tempFileName,
        CancellationToken cancellationToken)
    {
        var tempDir = Path.GetTempPath();
        var tempFilePath = Path.Combine(tempDir, tempFileName);
        await using var tempFile = File.Open(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

        var result = await _githubApiClient.FetchAsset(tempFile, asset, DownloadProgressReport, cancellationToken);
        if (result.State is not TaskState.Enums.TaskState.Success)
            return result.To<string>();
        
        await tempFile.FlushAsync(cancellationToken);
        return tempFilePath;
    }

    private async Task<TaskResult> InstallPak(CancellationToken cancellationToken)
    {
        if (_pakFileOnServer is null)
            return new TaskResult("Что-то пошло не так, перезапусти лаунчер.");

        var tempPak = await DownloadAssetFile(_pakFileOnServer, "tmp_wuwa_loc.tmp", cancellationToken);
        if (tempPak.State is not TaskState.Enums.TaskState.Success)
            return tempPak;

        var tempSha = await Utilities.CalculateSha256OfFile(tempPak.Content!, cancellationToken);
        if (tempSha.State is not TaskState.Enums.TaskState.Success)
            return tempSha;

        if (!tempSha.Content!.Equals(_pakShaServer))
            return new TaskResult("Во время загрузки, файл русификатора испортился...\n" +
                                  "Попробуй перезапустить лаунчер и попробуй установить заново.\n" +
                                  "\n" +
                                  $"SHA256[T]: '{tempSha.Content}'\n" +
                                  $"SHA256[S]: '{_pakShaServer}'");

        try
        {
            if (File.Exists(_translationFile))
                File.Delete(_translationFile);
        }
        catch (Exception e)
        {
            return new TaskResult("Не удалось удалить старый файл русификатора.\n" +
                                  "Если игра запущена, то выйди из игры.\n" +
                                  "\n" +
                                  e.Message, e);
        }

        try
        {
            File.Move(tempPak.Content!, _translationFile);
        }
        catch (Exception e)
        {
            return new TaskResult($"Не удалось установить файл русификатора: {e.Message}", e);
        }

        return TaskState.Enums.TaskState.Success;
    }

    private async void ButtonInstallPakFile_OnClick(object sender, RoutedEventArgs e)
    {
        _installCancellationTokenSource ??= new CancellationTokenSource();
        if (_installCancellationTokenSource.IsCancellationRequested)
            return;

        _context.InstallButtonVisible = false;

        _context.CancelButtonEnabled = true;
        _context.ProgressText = string.Empty;
        _context.Progress = 0.0d;
        _context.ProgressIndeterminate = true;
        _context.ProgressVisible = true;

        var result = await InstallPak(_installCancellationTokenSource.Token).ConfigureAwait(false);
        HandleError(result);

        _context.ProgressVisible = false;
        _context.InstallButtonVisible = true;
        _context.InstallButtonEnabled = true;

        if (result.State is TaskState.Enums.TaskState.Success)
        {
            _context.UpdateAvailable = false;
            _context.AllowLaunch = true;
        }
    }

    private void ButtonInstallCancel_OnClick(object sender, RoutedEventArgs e)
    {
        if (_installCancellationTokenSource is null)
            return;

        _context.CancelButtonEnabled = false;
        _installCancellationTokenSource.Cancel();
    }

    private TaskResult<Process> RunGame()
    {
        try
        {
            if (!File.Exists(_gameExeFile))
                return new TaskResult<Process>("Есть три варианта.\n" +
                                               "1) У тебя что-то с игрой не так.\n" +
                                               "\t- Запусти официальный лаунчер игры и проверь установлена ли игра.\n" +
                                               "2) Установщик русификатора (я) установлен не туда.\n" +
                                               "3) Установщий русификатора устарел и возможно, уже не поддерживается.\n" +
                                               "\t- Проверь обновления в официальных источниках.\n" +
                                               "\t- Если официальные источники не работают, забудь за этот русификатор, ищи другой или играй как есть...",
                    default);

            var procStartInfo = new ProcessStartInfo(_gameExeFile, _gameLaunchArgs);
            var proc = Process.Start(procStartInfo);
            if (proc is null)
                return new TaskResult<Process>(
                    "Скорее всего, игра не запустилась. Если это не так, проигнорируй это сообщение.",
                    default);

            return proc;
        }
        catch (Exception e)
        {
            return new TaskResult<Process>($"Всё сломалось... Сегодня не играешь: {e.Message}", e);
        }
    }

    private async void ButtonRunGame_OnClick(object sender, RoutedEventArgs e)
    {
        _context.LaunchButtonEnabled = false;

        try
        {
            var runResult = RunGame();
            if (runResult.State is not TaskState.Enums.TaskState.Success)
            {
                HandleError(runResult);
                return;
            }

            WindowState = WindowState.Minimized;
            await runResult.Content!.WaitForExitAsync().ConfigureAwait(false);
        }
        finally
        {
            _context.LaunchButtonEnabled = true;
            Dispatcher.Invoke(() => WindowState = WindowState.Normal);
        }
    }

    private void ButtonGitHub_OnClick(object sender, RoutedEventArgs e)
    {
        Utilities.OpenUrl("https://github.com/WuWa-Stuff/WuWaLauncher");
    }

    private void ButtonTelegram_OnClick(object sender, RoutedEventArgs e)
    {
        Utilities.OpenUrl("https://t.me/wutheringwaves_translate");
    }

    private void ButtonDiscord_OnClick(object sender, RoutedEventArgs e)
    {
        Utilities.OpenUrl("https://discord.gg/ruZfRQzZmB");
    }
}