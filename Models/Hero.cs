using System.Text.Json.Serialization;

namespace DotaHelper.Models;

public class Hero
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("localized_name")]
    public string LocalizedName { get; set; } = string.Empty;

    [JsonPropertyName("primary_attr")]
    public string PrimaryAttr { get; set; } = string.Empty;

    [JsonPropertyName("attack_type")]
    public string AttackType { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("legs")]
    public int Legs { get; set; }
}
