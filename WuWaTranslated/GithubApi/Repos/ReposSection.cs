using System.Net;
using RestSharp;
using WuWaTranslated.GithubApi.Abstractions;
using WuWaTranslated.GithubApi.Repos.Models;
using WuWaTranslated.TaskState;

namespace WuWaTranslated.GithubApi.Repos;

public class ReposSection : BaseGithubApiSection
{
    public ReposSection(GithubApiClient apiClient) : base(apiClient)
    {
    }

    public async Task<TaskResult<ReleaseItem>> FetchLatestRelease(
        string repoOwner,
        string repoName,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest(
            $"/repos/{WebUtility.UrlEncode(repoOwner)}/{WebUtility.UrlEncode(repoName)}/releases/latest");

        return await RunRequest<ReleaseItem>(request, cancellationToken).ConfigureAwait(false);
    }
}