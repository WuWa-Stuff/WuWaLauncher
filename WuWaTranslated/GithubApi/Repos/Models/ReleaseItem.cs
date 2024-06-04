using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace WuWaTranslated.GithubApi.Repos.Models;

public class ReleaseItem
{
    [JsonInclude]
    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; protected set; }

    [JsonInclude]
    [JsonPropertyName("assets")]
    private AssetItem[] AssetItems { get; set; } = null!;

    [JsonIgnore]
    public ReadOnlyCollection<AssetItem> Assets => AssetItems.AsReadOnly();
}