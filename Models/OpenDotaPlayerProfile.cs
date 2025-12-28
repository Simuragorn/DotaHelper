using System.Text.Json.Serialization;

namespace DotaHelper.Models;

public class OpenDotaPlayerProfile
{
    [JsonPropertyName("profile")]
    public ProfileData? Profile { get; set; }
}

public class ProfileData
{
    [JsonPropertyName("personaname")]
    public string? Personaname { get; set; }
}
