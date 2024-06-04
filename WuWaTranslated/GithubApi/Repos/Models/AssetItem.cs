using System.Text.Json.Serialization;

namespace WuWaTranslated.GithubApi.Repos.Models;

public class AssetItem
{
    [JsonInclude]
    [JsonPropertyName("name")]
    public string Name { get; protected set; } = null!;

    [JsonInclude]
    [JsonPropertyName("size")]
    public long Size { get; protected set; }

    [JsonInclude]
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; protected set; } = null!;
}