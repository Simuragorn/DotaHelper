using System.Text.Json.Serialization;

namespace DotaHelper.Models;

public class HeroStats
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("localized_name")]
    public string LocalizedName { get; set; } = string.Empty;

    [JsonPropertyName("pub_pick")]
    public int PubPick { get; set; }

    [JsonPropertyName("pub_win")]
    public int PubWin { get; set; }
}
