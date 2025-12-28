namespace DotaHelper.Models;

public class DotabuffStatsData
{
    public List<DotabuffHeroStats> Stats { get; set; } = new();

    public DateTime LastFetched { get; set; }

    public string PatchVersion { get; set; } = string.Empty;
}
