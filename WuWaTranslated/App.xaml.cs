using System.Diagnostics;
using System.IO;
using System.Windows;
using WuWaTranslated.TaskState;

namespace WuWaTranslated;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void ShowError(string message)
        => MessageBox.Show(message, "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);

    private TaskResult InstallAppUpdate(string[] args)
    {
        var oldPidStr = args.ElementAtOrDefault(1);
        var dstExePath = args.ElementAtOrDefault(2);

        if (!int.TryParse(oldPidStr, out var oldPid) || string.IsNullOrEmpty(dstExePath))
        {
            return new TaskResult("Установка обновления провалилась с грохотом...\n" +
                                  "Придётся тебе установить обновление вручную, скачав его из наших официальных источников.");
        }

        try
        {
            var oldProc = Process.GetProcessById(oldPid);
            oldProc.WaitForExit();
        }
        catch (ArgumentException) { /* do nothing i guess */ }
        catch (Exception exception)
        {
            return new TaskResult($"При установке обновления что-то пошло не так: {exception.Message}", exception);
        }

        Thread.Sleep(3_000);
        var currentExe = Utilities.GetCurrentExecutable();

#if SINGLE_FILE_BUILD
        try
        {
            File.Copy(currentExe, dstExePath, true);
        }
        catch (Exception e)
        {
            return new TaskResult($"Во время установки произошла ошибка: {e.Message}", e);
        }
#else
        var currentFolder = Path.GetDirectoryName(currentExe);
        if (string.IsNullOrEmpty(currentFolder))
            return new TaskResult("Придётся тебе скачать и установить вручную...");

        try
        {
            var gameDirectory = Path.GetDirectoryName(dstExePath)!; // i think it will not be null... but...
            foreach (var file in Directory.EnumerateFiles(currentFolder, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(currentFolder, file);
                var targetPath = Path.GetFullPath(relativePath, gameDirectory);

                var targetDir = new FileInfo(targetPath).Directory!;
                if (!targetDir.Exists)
                    targetDir.Create();

                File.Copy(file, targetPath, true);
            }
        }
        catch (Exception e)
        {
            return new TaskResult($"Во время установки произошла ошибка: {e.Message}", e);
        }

        currentExe = currentFolder;
#endif

        try
        {
            var currentPid = Process.GetCurrentProcess().Id.ToString();
            var procStartInfo = new ProcessStartInfo(dstExePath,
                [
                    Ui.MainWindow.AppLaunchModePostUpdate,
                    currentPid,
                    currentExe
                ]);
            var procInfo = Process.Start(procStartInfo);
            if (procInfo is null)
                return new TaskResult("Требуется запуск из установленного месте вручную..." +
                                      $"Искать меня теперь где-то тут: {dstExePath}");

            Environment.Exit(0);
            return TaskState.Enums.TaskState.Success;
        }
        catch (Exception exception)
        {
            return new TaskResult($"При установке обновления что-то пошло не так: {exception.Message}", exception);
        }
    }

    private TaskResult CleanupPostUpdate(string[] args)
    {
        var oldPidStr = args.ElementAtOrDefault(1);
        var tmpExePath = args.ElementAtOrDefault(2);

        if (!int.TryParse(oldPidStr, out var oldPid) || string.IsNullOrEmpty(tmpExePath))
            return TaskState.Enums.TaskState.Success; // todo: maybe log or sth...

        try
        {
            var oldProc = Process.GetProcessById(oldPid);
            oldProc.WaitForExit();
        }
        catch (ArgumentException) { /* do nothing i guess */ }
        catch
        {
            // todo: maybe log or sth...
            return TaskState.Enums.TaskState.Success;
        }

        Thread.Sleep(3_000);
#if SINGLE_FILE_BUILD
        try
        {
            File.Delete(tmpExePath);
        }
        catch
        {
            // todo: maybe log or sth...
            return TaskState.Enums.TaskState.Success;
        }
#else
        try
        {
            var oldTempDir = Path.GetDirectoryName(tmpExePath);
            if (string.IsNullOrEmpty(oldTempDir))
                return TaskState.Enums.TaskState.Success; // todo: maybe log or sth...

            Directory.Delete(oldTempDir, true);
        }
        catch
        {
            // todo: maybe log or sth...
            return TaskState.Enums.TaskState.Success;
        }
#endif
        
        return TaskState.Enums.TaskState.Success;
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var mode = args.ElementAtOrDefault(0);
        if (string.IsNullOrEmpty(mode))
            return;

        TaskResult result = mode.ToUpperInvariant() switch
        {
            Ui.MainWindow.AppLaunchModeUpdate => InstallAppUpdate(args),
            Ui.MainWindow.AppLaunchModePostUpdate => CleanupPostUpdate(args),
            _ => TaskState.Enums.TaskState.Success
        };

        if (result.State is TaskState.Enums.TaskState.Success)
            return;

        if (result.State is TaskState.Enums.TaskState.Failed)
            ShowError(result.ErrorMessage ?? "Что-то пошло не так...");

        Environment.Exit(111);
    }
}