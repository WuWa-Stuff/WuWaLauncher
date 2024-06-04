using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using WuWaTranslated.TaskState;

namespace WuWaTranslated;

internal static class Utilities
{
    private static readonly string[] ReadableSizes = ["B", "KB", "MB", "GB", "TB"];
    public static string ByteSizeToHumanReadable(long szBytes, IFormatProvider? formatProvider)
    {
        double len = szBytes;
        int order = 0;
        while (len >= 1000 && order < ReadableSizes.Length - 1) {
            order++;
            len /= 1000;
        }

        return string.Format(formatProvider, "{0:N0} {1}", len, ReadableSizes[order]);
    }

    public static async Task<TaskResult<string>> CalculateSha256OfFile(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var file = File.OpenRead(path);
            var shaHash = await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false);
            return BitConverter.ToString(shaHash)
                .Replace("-", string.Empty, StringComparison.InvariantCulture)
                .ToLower(CultureInfo.InvariantCulture);
        }
        catch (TaskCanceledException)
        {
            return TaskResult<string>.Cancelled;
        }
        catch (Exception e)
        {
            return new TaskResult<string>($"Failed to calculate sha256 of '{path}': {e.Message}", e);
        }
    }

    public static string GetCurrentExecutable()
    {
        var currentProcess = Process.GetCurrentProcess();
        var location = currentProcess.MainModule?.FileName;
        if (!string.IsNullOrEmpty(location))
            return location;

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.Location;
    }

    public static bool ShouldDisableDlss()
    {
        // ref: https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-videocontroller
        const string query = "SELECT * FROM Win32_VideoController";

        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            foreach (var result in searcher.Get())
            {
                var gpuName = result["Name"] as string;
                if (string.IsNullOrEmpty(gpuName))
                    continue;

                if (gpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }
        catch { /* Do nothing, return true to disable DLSS, idk... */ }

        return true;
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start("explorer", url);
        }
        catch { /* Do nothing */ }
    }
}