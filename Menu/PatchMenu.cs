using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class PatchMenu : IMenu
{
    private readonly IStorageService<Patch> _patchStorageService;
    private readonly IStorageService<DotabuffStatsData> _statsStorageService;
    private readonly IStorageService<HeroCountersCache> _countersCache;
    private readonly IDotabuffService _dotabuffService;

    public PatchMenu(
        IStorageService<Patch> patchStorageService,
        IStorageService<DotabuffStatsData> statsStorageService,
        IStorageService<HeroCountersCache> countersCache,
        IDotabuffService dotabuffService)
    {
        _patchStorageService = patchStorageService;
        _statsStorageService = statsStorageService;
        _countersCache = countersCache;
        _dotabuffService = dotabuffService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Change Patch ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        var currentPatch = _patchStorageService.Load();

        if (currentPatch != null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nCurrent patch: {currentPatch.Version}");
            Console.ResetColor();
        }

        Console.WriteLine("\nEnter new patch version:");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nPatch version cannot be empty.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
            return;
        }

        var patch = new Patch
        {
            Version = input.Trim(),
            LastModified = DateTime.UtcNow
        };

        _patchStorageService.Save(patch);
        _statsStorageService.Delete();
        _countersCache.Delete();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Patch updated to: {patch.Version}");
        Console.WriteLine("✓ Cached statistics cleared");
        Console.WriteLine("✓ Counterpicks cache cleared");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nFetching hero statistics for new patch...");
        Console.ResetColor();

        var fetchedStats = await _dotabuffService.FetchHeroStatsAsync(patch.Version);

        if (fetchedStats == null || fetchedStats.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Failed to fetch statistics. You can try again from the main menu.");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to return to main menu...");
        Console.ReadKey();
    }
}
