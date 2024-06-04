using System.IO;
using System.Net;
using System.Net.Http;
using RestSharp;
using WuWaTranslated.Exceptions;
using WuWaTranslated.GithubApi.Repos;
using WuWaTranslated.GithubApi.Repos.Models;
using WuWaTranslated.TaskState;

namespace WuWaTranslated.GithubApi;

public class GithubApiClient
{
    public const string DefaultBaseUrl = "https://api.github.com";

    private readonly RestClient _client;

    public readonly ReposSection Repos;

    public GithubApiClient(string baseUrl = DefaultBaseUrl)
    {
        _client = new RestClient(new RestClientOptions(baseUrl));

        Repos = new ReposSection(this);
    }

    public async Task<TaskResult> FetchAsset(
        Stream fetchDestination,
        AssetItem assetItem,
        ProgressReportDelegate progressReport,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest(assetItem.BrowserDownloadUrl)
        {
            CompletionOption = HttpCompletionOption.ResponseHeadersRead
        };

        var progressStream = new ProgressStream(fetchDestination, progressReport);
        request.AdvancedResponseWriter = (message, restRequest) =>
        {
            var len = message.Content.Headers.ContentLength;
            if (len.HasValue)
                progressStream.SetLength(len.Value);

            var task = message.Content.CopyToAsync(progressStream, cancellationToken);
            Exception? exception = default;
            try
            {
                task.Wait(cancellationToken);
            }
            catch (TaskCanceledException) { /* do nothing i guess */ }
            catch (OperationCanceledException) { /* do nothing i guess */ }
            catch (Exception e)
            {
                exception = e;
            }

            var respStatus = task.Status switch
            {
                TaskStatus.Canceled => ResponseStatus.Aborted,
                TaskStatus.Faulted => ResponseStatus.Error,
                TaskStatus.RanToCompletion => ResponseStatus.Completed,
                _ when cancellationToken.IsCancellationRequested => ResponseStatus.Aborted,
                _ => ResponseStatus.None
            };

            return new RestResponse(restRequest)
            {
                StatusCode = message.StatusCode,
                ResponseStatus = respStatus,
                ErrorException = exception,
                ErrorMessage = exception?.Message,
                Version = message.Version,
                IsSuccessStatusCode = message.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent
            };
        };

        var response = await _client.ExecuteAsync(request, cancellationToken);
        if (!response.IsSuccessful)
            return new RestRequestException(response.ErrorMessage ?? "Что-то пошло не так...", request, response);

        return TaskState.Enums.TaskState.Success;
    }

    public async Task<TaskResult<string>> FetchAssetAsString(AssetItem assetItem, CancellationToken cancellationToken)
    {
        var request = new RestRequest(assetItem.BrowserDownloadUrl);
        var response = await _client.ExecuteAsync(request, cancellationToken);
        if (!response.IsSuccessful)
            return new RestRequestException(response.ErrorMessage ?? "Unable to fetch asset file",
                request,
                response);
            
        if (response.Content is null)
            return new RestRequestException("Unable to fetch asset as text", request, response);

        return response.Content;
    }

    internal async Task<TaskResult<T>> RunRequest<T>(RestRequest request, CancellationToken cancellationToken)
    {
        var response = await _client.ExecuteAsync<T>(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessful)
            return new RestRequestException(response.ErrorMessage ?? "Что-то пошло не так...\n" +
                $"{request.Method.ToString().ToUpperInvariant()} {request.Resource}: [HTTP/{(int)response.StatusCode} ({response.StatusCode})]",
                request,
                response);

        if (response.Data is null)
            return new RestRequestException("Response data is NULL. Looks like something went wrong...",
                request,
                response);

        return response.Data;
    }
}