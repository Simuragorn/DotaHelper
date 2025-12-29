using DotaHelper.Models;

namespace DotaHelper.Services;

public interface IDotabuffService : IDisposable
{
    Task<List<DotabuffHeroStats>?> FetchHeroStatsAsync(string patchVersion);

    List<DotabuffHeroStats>? GetCachedStats();

    bool HasValidCache(string currentPatch);

    Task<List<DotabuffCounter>?> FetchHeroCountersAsync(string heroUrl, string patchVersion);

    HeroCounterData? GetCachedCounters(string heroUrl);

    void ClearCountersCache();

    HeroCountersCache? GetCountersCacheInfo();

    Task<int> PreCacheAllCountersAsync(
        string patchVersion,
        List<Hero> heroes,
        Action<int, int> progressCallback,
        Func<bool> shouldContinue);
}
