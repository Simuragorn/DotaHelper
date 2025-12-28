using System.Text.Json.Serialization;

namespace DotaHelper.Models;

public class HeroMatchup
{
    [JsonPropertyName("hero_id")]
    public int HeroId { get; set; }

    [JsonPropertyName("games_played")]
    public int GamesPlayed { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }
}
