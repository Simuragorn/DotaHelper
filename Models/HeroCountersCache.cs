namespace DotaHelper.Models;

public class HeroCountersCache
{
    public Dictionary<string, HeroCounterData> Cache { get; set; } = new();
    public string PatchVersion { get; set; } = string.Empty;
}

public class HeroCounterData
{
    public string HeroUrl { get; set; } = string.Empty;
    public List<DotabuffCounter> Counters { get; set; } = new();
    public DateTime LastFetched { get; set; }
}
