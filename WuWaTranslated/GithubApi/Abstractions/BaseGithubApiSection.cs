using RestSharp;
using WuWaTranslated.TaskState;

namespace WuWaTranslated.GithubApi.Abstractions;

public abstract class BaseGithubApiSection
{
    protected readonly GithubApiClient ApiClient;

    protected BaseGithubApiSection(GithubApiClient apiClient)
    {
        ApiClient = apiClient;
    }

    protected async Task<TaskResult<T>> RunRequest<T>(RestRequest request, CancellationToken cancellationToken)
        => await ApiClient.RunRequest<T>(request, cancellationToken).ConfigureAwait(false);
}