using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class MainMenu : IMenu
{
    private readonly DraftMenu _draftMenu;
    private readonly PatchMenu _patchMenu;
    private readonly RefetchStatsMenu _refetchStatsMenu;
    private readonly CountersCacheMenu _countersCacheMenu;
    private readonly IStorageService<Patch> _patchStorageService;
    private readonly IStorageService<DotabuffStatsData> _dotabuffStatsStorageService;
    private readonly IDotabuffService _dotabuffService;
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private bool _isFirstRun = true;
    private string? _currentPatch;
    private DateTime? _lastFetchedStats;

    public MainMenu(
        DraftMenu draftMenu,
        PatchMenu patchMenu,
        RefetchStatsMenu refetchStatsMenu,
        CountersCacheMenu countersCacheMenu,
        IStorageService<Patch> patchStorageService,
        IStorageService<DotabuffStatsData> dotabuffStatsStorageService,
        IDotabuffService dotabuffService,
        IStorageService<List<Hero>> heroStorageService)
    {
        _draftMenu = draftMenu;
        _patchMenu = patchMenu;
        _refetchStatsMenu = refetchStatsMenu;
        _countersCacheMenu = countersCacheMenu;
        _patchStorageService = patchStorageService;
        _dotabuffStatsStorageService = dotabuffStatsStorageService;
        _dotabuffService = dotabuffService;
        _heroStorageService = heroStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Dota Helper ===");

        if (!string.IsNullOrEmpty(_currentPatch))
        {
            Console.Write("Patch: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(_currentPatch);
            Console.ResetColor();

            Console.Write(" (");
            Console.ForegroundColor = ConsoleColor.DarkGray;

            if (_lastFetchedStats.HasValue)
            {
                var daysDiff = (DateTime.UtcNow - _lastFetchedStats.Value).Days;

                if (daysDiff == 0)
                {
                    Console.Write("statistic fetched today");
                }
                else if (daysDiff == 1)
                {
                    Console.Write("statistic fetched 1 day ago");
                }
                else
                {
                    Console.Write($"statistic fetched {daysDiff} days ago");
                }
            }
            else
            {
                Console.Write("statistic fetched never");
            }

            Console.ResetColor();
            Console.Write(")");
            Console.WriteLine();
        }

        var cacheInfo = _dotabuffService.GetCountersCacheInfo();
        var heroes = _heroStorageService.Load();

        if (heroes != null)
        {
            int cachedHeroes = 0;
            bool validCache = cacheInfo != null && cacheInfo.PatchVersion == _currentPatch;

            if (validCache)
            {
                cachedHeroes = cacheInfo!.Cache.Count;
            }

            int totalHeroes = heroes.Count;

            Console.Write("Cached counterpicks: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{cachedHeroes}/{totalHeroes} heroes");
            Console.ResetColor();

            if (validCache && cachedHeroes > 0)
            {
                var oldestCache = cacheInfo!.Cache.Values.Min(c => c.LastFetched);
                var daysSinceOldest = (DateTime.UtcNow - oldestCache).Days;

                Console.Write(" (oldest: ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                if (daysSinceOldest == 0)
                {
                    Console.Write("today");
                }
                else if (daysSinceOldest == 1)
                {
                    Console.Write("1 day ago");
                }
                else
                {
                    Console.Write($"{daysSinceOldest} days ago");
                }
                Console.ResetColor();
                Console.Write(")");
            }

            Console.WriteLine();
        }

        Console.WriteLine("\n1. Draft");
        Console.WriteLine("2. Change Patch");
        Console.WriteLine("3. Refetch heroes statistic");
        Console.WriteLine("4. Cache Management");
        Console.WriteLine("0. Exit");
        Console.Write("\nSelect an option: ");
    }

    public async Task ExecuteAsync()
    {
        if (_isFirstRun)
        {
            _isFirstRun = false;

            LoadPatch();
            LoadStatsTime();
        }

        while (true)
        {
            Display();
            string? choice = Console.ReadLine()?.Trim();

            Console.Clear();

            switch (choice)
            {
                case "1":
                    await _draftMenu.ExecuteAsync();
                    break;
                case "2":
                    await _patchMenu.ExecuteAsync();
                    LoadPatch();
                    LoadStatsTime();
                    break;
                case "3":
                    await _refetchStatsMenu.ExecuteAsync();
                    LoadStatsTime();
                    break;
                case "4":
                    await _countersCacheMenu.ExecuteAsync();
                    break;
                case "0":
                    Console.WriteLine("\nGoodbye!");
                    return;
                default:
                    Console.WriteLine("\nInvalid option. Please try again.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private void LoadPatch()
    {
        var patch = _patchStorageService.Load();
        _currentPatch = patch?.Version;
    }

    private void LoadStatsTime()
    {
        var statsData = _dotabuffStatsStorageService.Load();
        _lastFetchedStats = statsData?.LastFetched;
    }
}
