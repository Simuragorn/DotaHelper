namespace DotaHelper.Models;

public class FavoriteHeroes
{
    public List<int> HeroIds { get; set; } = new();
    public DateTime LastModified { get; set; }
}
