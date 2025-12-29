using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class CountersCacheMenu : IMenu
{
    private readonly IDotabuffService _dotabuffService;
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly IStorageService<Patch> _patchStorageService;

    public CountersCacheMenu(
        IDotabuffService dotabuffService,
        IStorageService<List<Hero>> heroStorageService,
        IStorageService<Patch> patchStorageService)
    {
        _dotabuffService = dotabuffService;
        _heroStorageService = heroStorageService;
        _patchStorageService = patchStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Counterpicks Cache Management ===\n");

        var cacheInfo = _dotabuffService.GetCountersCacheInfo();
        var heroes = _heroStorageService.Load();
        var patch = _patchStorageService.Load();

        if (cacheInfo != null && heroes != null && cacheInfo.PatchVersion == patch?.Version)
        {
            int totalHeroes = heroes.Count;
            int cachedHeroes = cacheInfo.Cache.Count;

            Console.Write("Cached heroes: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{cachedHeroes}/{totalHeroes}");
            Console.ResetColor();

            Console.Write("Current patch: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(cacheInfo.PatchVersion);
            Console.ResetColor();

            if (cachedHeroes > 0)
            {
                var oldestCache = cacheInfo.Cache.Values.Min(c => c.LastFetched);
                var newestCache = cacheInfo.Cache.Values.Max(c => c.LastFetched);
                var daysSinceOldest = (DateTime.UtcNow - oldestCache).Days;
                var daysSinceNewest = (DateTime.UtcNow - newestCache).Days;

                Console.Write("Oldest cache: ");
                if (daysSinceOldest > 7)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (daysSinceOldest > 3)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{daysSinceOldest} day(s) ago");
                Console.ResetColor();

                Console.Write("Newest cache: ");
                if (daysSinceNewest > 7)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (daysSinceNewest > 3)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{daysSinceNewest} day(s) ago");
                Console.ResetColor();
            }
        }
        else if (cacheInfo != null && patch != null && cacheInfo.PatchVersion != patch.Version)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Cache is for a different patch version.");
            Console.WriteLine($"Cache patch: {cacheInfo.PatchVersion}");
            Console.WriteLine($"Current patch: {patch.Version}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("No cache data available.");
        }

        Console.WriteLine("\nOptions:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("1");
        Console.ResetColor();
        Console.WriteLine(". Pre-cache all heroes' counterpicks");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("2");
        Console.ResetColor();
        Console.WriteLine(". Clear cache");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("3");
        Console.ResetColor();
        Console.WriteLine(". Return to main menu");
        Console.Write("\nSelect an option: ");
    }

    public async Task ExecuteAsync()
    {
        while (true)
        {
            Display();

            string? input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await PreCacheAllCountersAsync();
                    break;
                case "2":
                    ClearCache();
                    break;
                case "3":
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid option. Press any key to try again...");
                    Console.ResetColor();
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task PreCacheAllCountersAsync()
    {
        Console.Clear();
        Console.WriteLine("=== Pre-cache All Counterpicks ===\n");

        var heroes = _heroStorageService.Load();
        var patch = _patchStorageService.Load();

        if (heroes == null || heroes.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No heroes data available. Please fetch hero stats first.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
            return;
        }

        if (patch == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No patch version set. Please set patch version first.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Preparing to cache counterpicks for {heroes.Count} heroes...");
        Console.WriteLine("Press any key to cancel at any time.\n");

        bool cancelled = false;
        int lastCached = 0;

        await _dotabuffService.PreCacheAllCountersAsync(
            patch.Version,
            heroes,
            (cached, total) =>
            {
                Console.SetCursorPosition(0, Console.CursorTop - (lastCached == 0 ? 0 : 1));
                Console.WriteLine($"Progress: {cached}/{total} heroes cached");
                lastCached = cached;
            },
            () =>
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    cancelled = true;
                    return false;
                }
                return true;
            });

        if (cancelled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n✓ Caching cancelled. Progress has been saved.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ All heroes cached successfully!");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey();
    }

    private void ClearCache()
    {
        Console.Clear();
        Console.WriteLine("=== Clear Counterpicks Cache ===\n");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Are you sure you want to clear all cached counterpicks? (y/n)");
        Console.ResetColor();

        string? input = Console.ReadLine();

        if (input?.ToLower() == "y")
        {
            _dotabuffService.ClearCountersCache();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Counterpicks cache cleared successfully!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("\nCache clear cancelled.");
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey();
    }
}
