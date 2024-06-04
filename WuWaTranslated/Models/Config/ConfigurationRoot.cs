using System.Text.Json.Serialization;

namespace WuWaTranslated.Models.Config;

public class ConfigurationRoot
{
    [JsonPropertyName("game_path")]
    public string? GameDirectory { get; set; }
}