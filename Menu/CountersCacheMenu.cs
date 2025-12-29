using DotaHelper.Helpers;
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

        DisplayCacheStatus(cacheInfo, heroes, patch);
        DisplayMenuOptions();
    }

    private void DisplayCacheStatus(HeroCountersCache? cacheInfo, List<Hero>? heroes, Patch? patch)
    {
        if (cacheInfo != null && heroes != null && cacheInfo.PatchVersion == patch?.Version)
        {
            DisplayValidCache(cacheInfo, heroes.Count);
        }
        else if (cacheInfo != null && patch != null && cacheInfo.PatchVersion != patch.Version)
        {
            DisplayPatchMismatch(cacheInfo, patch);
        }
        else
        {
            Console.WriteLine("No cache data available.");
        }
    }

    private void DisplayValidCache(HeroCountersCache cacheInfo, int totalHeroes)
    {
        int cachedHeroes = cacheInfo.Cache.Count;

        ConsoleHelper.WriteLabelValue("Cached heroes: ", $"{cachedHeroes}/{totalHeroes}", ConsoleColor.Cyan);
        ConsoleHelper.WriteLabelValue("Current patch: ", cacheInfo.PatchVersion, ConsoleColor.Yellow);

        if (cachedHeroes > 0)
        {
            var oldestCache = cacheInfo.Cache.Values.Min(c => c.LastFetched);
            var newestCache = cacheInfo.Cache.Values.Max(c => c.LastFetched);
            var daysSinceOldest = (DateTime.UtcNow - oldestCache).Days;
            var daysSinceNewest = (DateTime.UtcNow - newestCache).Days;

            DisplayCacheAge("Oldest cache: ", daysSinceOldest);
            DisplayCacheAge("Newest cache: ", daysSinceNewest);
        }
    }

    private void DisplayCacheAge(string label, int days)
    {
        var color = days > 7 ? ConsoleColor.Red : days > 3 ? ConsoleColor.Yellow : ConsoleColor.Green;
        ConsoleHelper.WriteLabelValue(label, $"{days} day(s) ago", color);
    }

    private void DisplayPatchMismatch(HeroCountersCache cacheInfo, Patch patch)
    {
        ConsoleHelper.WriteLineColored("Cache is for a different patch version.", ConsoleColor.Yellow);
        ConsoleHelper.WriteLineColored($"Cache patch: {cacheInfo.PatchVersion}", ConsoleColor.Yellow);
        ConsoleHelper.WriteLineColored($"Current patch: {patch.Version}", ConsoleColor.Yellow);
    }

    private void DisplayMenuOptions()
    {
        Console.WriteLine("\nOptions:");
        DisplayMenuOption("1", "Pre-cache all heroes' counterpicks");
        DisplayMenuOption("2", "Clear cache");
        DisplayMenuOption("3", "Return to main menu");
        Console.Write("\nSelect an option: ");
    }

    private void DisplayMenuOption(string number, string description)
    {
        ConsoleHelper.WriteColored(number, ConsoleColor.Cyan);
        Console.WriteLine($". {description}");
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
