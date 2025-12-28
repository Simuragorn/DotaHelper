using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class RefetchStatsMenu : IMenu
{
    private readonly IDotabuffService _dotabuffService;
    private readonly IStorageService<Patch> _patchStorageService;

    public RefetchStatsMenu(IDotabuffService dotabuffService, IStorageService<Patch> patchStorageService)
    {
        _dotabuffService = dotabuffService;
        _patchStorageService = patchStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Refetch Stats ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        var patch = _patchStorageService.Load();

        if (patch == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nNo patch version found. Please set patch version first.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nCurrent patch: {patch.Version}");

        var cachedStats = _dotabuffService.GetCachedStats();
        if (cachedStats != null && cachedStats.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Last fetched: {cachedStats[0].LastFetched:yyyy-MM-dd HH:mm}");
            Console.ResetColor();
        }

        while (true)
        {
            Console.WriteLine("\nFetching hero statistics from Dotabuff...");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This will take approximately 25 seconds (5 positions with 5-second delays)");
            Console.ResetColor();

            var stats = await _dotabuffService.FetchHeroStatsAsync(patch.Version);

            if (stats != null && stats.Count > 0)
            {
                Console.WriteLine("\nPress any key to return to main menu...");
                Console.ReadKey();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nFailed to fetch statistics.");
            Console.ResetColor();

            var cached = _dotabuffService.GetCachedStats();
            if (cached != null && cached.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Cached data from {cached[0].LastFetched:yyyy-MM-dd HH:mm} is still available.");
                Console.ResetColor();
            }

            Console.Write("\nRetry? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            if (response != "y")
            {
                return;
            }

            Display();
            Console.WriteLine($"\nCurrent patch: {patch.Version}");
        }
    }
}
