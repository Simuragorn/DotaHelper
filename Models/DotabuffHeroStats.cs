namespace DotaHelper.Models;

public class DotabuffHeroStats
{
    public int Id { get; set; }

    public string LocalizedName { get; set; } = string.Empty;

    public string HeroUrl { get; set; } = string.Empty;

    public double WinRate { get; set; }

    public double PickRateCoreMid { get; set; }

    public double PickRateCoreSafe { get; set; }

    public double PickRateCoreOff { get; set; }

    public double PickRateSupportSafe { get; set; }

    public double PickRateSupportOff { get; set; }

    public double BanRate { get; set; }

    public DateTime LastFetched { get; set; }

    public string PatchVersion { get; set; } = string.Empty;
}
