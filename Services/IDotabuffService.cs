using DotaHelper.Models;

namespace DotaHelper.Services;

public interface IDotabuffService
{
    Task<List<DotabuffHeroStats>?> FetchHeroStatsAsync(string patchVersion);

    List<DotabuffHeroStats>? GetCachedStats();

    bool HasValidCache(string currentPatch);
}
