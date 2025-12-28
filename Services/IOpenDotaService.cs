using DotaHelper.Models;

namespace DotaHelper.Services;

public interface IOpenDotaService
{
    Task<string?> GetPlayerPersonaNameAsync(string dotaId);
    Task<List<PlayerHero>?> GetPlayerHeroesAsync(string dotaId);
    Task<List<Hero>?> GetAllHeroesAsync();
    Task<List<HeroMatchup>?> GetHeroMatchupsAsync(int heroId);
    Task<List<HeroStats>?> GetHeroStatsAsync();
}
