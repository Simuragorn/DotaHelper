using DotaHelper.Models;

namespace DotaHelper.Services;

public interface IDotabuffService : IDisposable
{
    Task<List<DotabuffHeroStats>?> FetchHeroStatsAsync(string patchVersion);

    List<DotabuffHeroStats>? GetCachedStats();

    bool HasValidCache(string currentPatch);

    Task<List<DotabuffCounter>?> FetchHeroCountersAsync(string heroUrl, string patchVersion);
}
