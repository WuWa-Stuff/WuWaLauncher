using System.IO;
using System.Text.Json;
using WuWaTranslated.Models.Config;
using WuWaTranslated.TaskState;

namespace WuWaTranslated;

public class ConfigHolder
{
    private readonly string _jsonPath;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private ConfigurationRoot _config;

    public ConfigHolder(string jsonPath)
    {
        _jsonPath = jsonPath;
        _config = new ConfigurationRoot();
    }

    public async Task<TaskResult> Load(CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_jsonPath))
                return await CreateDefault(cancellationToken);

            await using var jsonFile = File.OpenRead(_jsonPath);
            _config = await JsonSerializer.DeserializeAsync<ConfigurationRoot>(jsonFile,
                          cancellationToken: cancellationToken)
                      ?? _config;

            return TaskState.Enums.TaskState.Success;
        }
        catch (JsonException)
        {
            return await CreateDefault(cancellationToken);
        }
        catch (Exception e)
        {
            return new TaskResult($"При загрузке настроек что-то пошло не так: {e.Message}", e);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<TaskResult> CreateDefault(CancellationToken cancellationToken = default)
    {
        _config = new ConfigurationRoot();
        return await Save(false, cancellationToken);
    }

    public async Task<TaskResult> Save(bool useSemaphore, CancellationToken cancellationToken)
    {
        if (useSemaphore)
            await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            await using var file = File.Open(_jsonPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(file, _config, cancellationToken: cancellationToken);
            return TaskState.Enums.TaskState.Success;
        }
        catch (Exception e)
        {
            return new TaskResult($"При сохранении настроек что-то пошло не так: {e.Message}", e);
        }
        finally
        {
            if (useSemaphore)
                _semaphoreSlim.Release();
        }
    }

    public async Task<TaskResult> Save(CancellationToken cancellationToken = default)
        => await Save(true, cancellationToken);

    public async Task<ConfigurationRoot> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            return _config;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<TaskResult> EditConfig(
        Action<ConfigurationRoot> configFn,
        bool save = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            configFn(_config);
            return await Save(false, cancellationToken);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}