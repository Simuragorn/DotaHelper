namespace DotaHelper.Models;

public class DotabuffCounter
{
    public int HeroId { get; set; }
    public string HeroName { get; set; } = string.Empty;
    public double Disadvantage { get; set; }
    public double WinRate { get; set; }
    public int MatchesPlayed { get; set; }
}
