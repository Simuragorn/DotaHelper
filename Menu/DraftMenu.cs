using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class DraftMenu : IMenu
{
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly List<DotabuffHeroStats> _dotabuffStats;
    private readonly IDotabuffService _dotabuffService;
    private readonly IStorageService<Patch> _patchStorageService;
    private readonly IStorageService<FavoriteHeroes> _favoriteHeroesStorage;
    private List<Hero>? _heroes;

    public DraftMenu(
        IStorageService<List<Hero>> heroStorageService,
        List<DotabuffHeroStats> dotabuffStats,
        IDotabuffService dotabuffService,
        IStorageService<Patch> patchStorageService,
        IStorageService<FavoriteHeroes> favoriteHeroesStorage)
    {
        _heroStorageService = heroStorageService;
        _dotabuffStats = dotabuffStats;
        _dotabuffService = dotabuffService;
        _patchStorageService = patchStorageService;
        _favoriteHeroesStorage = favoriteHeroesStorage;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Draft ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        _heroes = _heroStorageService.Load();

        if (_heroes == null || _heroes.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No heroes data found.");
            Console.ResetColor();
            Console.WriteLine("Please use \"Refetch heroes statistic\" option from main menu first.");
            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
            return;
        }

        var selectedHero = GetHeroInput();

        if (selectedHero != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Selected hero: {selectedHero.LocalizedName}");
            Console.ResetColor();

            await DisplayCounterPicksAsync(selectedHero);
        }

        Console.WriteLine("\nPress any key to return to main menu...");
        Console.ReadKey();
    }

    private Hero? GetHeroInput()
    {
        string input = string.Empty;
        Hero? expectedHero = null;

        Console.WriteLine("\nEnter opponent hero name (or press Escape to cancel):");
        Console.WriteLine();

        int expectedHeroLine = Console.CursorTop;
        int inputLine = expectedHeroLine + 1;

        while (true)
        {
            DisplayExpectedHero(expectedHero, expectedHeroLine);
            DisplayInputLine(input, inputLine);

            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (expectedHero != null)
                {
                    return expectedHero;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Escape)
            {
                return null;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input[..^1];
                    expectedHero = FindMatchingHero(input);
                }
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                expectedHero = FindMatchingHero(input);
            }
        }
    }

    private int? GetPositionInput()
    {
        Console.WriteLine("\nSelect counter pick position (1-5), or press Enter for all positions:");
        Console.WriteLine("1 - Core Safe");
        Console.WriteLine("2 - Core Mid");
        Console.WriteLine("3 - Core Off");
        Console.WriteLine("4 - Support Off");
        Console.WriteLine("5 - Support Safe");
        Console.Write("\nPosition: ");

        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (int.TryParse(input, out int position) && position >= 1 && position <= 5)
        {
            return position;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Invalid position. Showing all counters.");
        Console.ResetColor();
        return null;
    }

    private Hero? FindMatchingHero(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        return _heroes?.FirstOrDefault(h =>
            h.LocalizedName.Contains(input, StringComparison.OrdinalIgnoreCase));
    }

    private void DisplayExpectedHero(Hero? hero, int line)
    {
        Console.SetCursorPosition(0, line);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, line);

        if (hero != null)
        {
            Console.Write("Expected hero: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(hero.LocalizedName);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Start typing to search...");
            Console.ResetColor();
        }
    }

    private void DisplayInputLine(string input, int line)
    {
        Console.SetCursorPosition(0, line);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, line);
        Console.Write($"Enter opponent hero name: {input}");
    }

    private async Task DisplayCounterPicksAsync(Hero selectedHero)
    {
        var heroStats = _dotabuffStats.FirstOrDefault(h => h.Id == selectedHero.Id);
        if (heroStats == null || string.IsNullOrEmpty(heroStats.HeroUrl))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Hero URL not found in Dotabuff stats.");
            Console.ResetColor();
            return;
        }

        var currentPatch = _patchStorageService.Load();
        if (currentPatch == null || string.IsNullOrEmpty(currentPatch.Version))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Patch version not found.");
            Console.ResetColor();
            return;
        }

        int? selectedPosition = GetPositionInput();

        while (true)
        {
            Console.WriteLine("\nFetching counter data from Dotabuff...");

            var counters = await _dotabuffService.FetchHeroCountersAsync(heroStats.HeroUrl, currentPatch.Version);

            if (counters == null || counters.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to fetch counter data from Dotabuff.");
                Console.ResetColor();
                Console.Write("\nRetry? (y/n): ");
                string? response = Console.ReadLine()?.Trim().ToLower();

                if (response != "y")
                {
                    return;
                }

                continue;
            }

            var counterPicks = ProcessCounterPicks(counters, selectedPosition);
            DisplayCounterPicksTable(selectedHero, counterPicks, selectedPosition);
            break;
        }
    }

    private List<CounterPickInfo> ProcessCounterPicks(List<DotabuffCounter> counters, int? selectedPosition)
    {
        var counterPicks = new List<CounterPickInfo>();
        int skippedCount = 0;

        foreach (var counter in counters)
        {
            var hero = _heroes?.FirstOrDefault(h => h.Id == counter.HeroId);
            if (hero == null) continue;

            var heroStats = _dotabuffStats?.FirstOrDefault(h => h.Id == counter.HeroId);

            if (selectedPosition.HasValue && heroStats != null)
            {
                if (!IsHeroViableForPosition(heroStats, selectedPosition.Value))
                {
                    skippedCount++;
                    continue;
                }
            }

            var counterPick = new CounterPickInfo
            {
                Hero = hero,
                Disadvantage = counter.Disadvantage
            };

            counterPicks.Add(counterPick);
        }

        if (selectedPosition.HasValue && skippedCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{skippedCount} hero(es) skipped due to position filter.");
            Console.ResetColor();
        }

        return counterPicks
            .OrderBy(cp => cp.Disadvantage)
            .ToList();
    }

    private bool IsHeroViableForPosition(DotabuffHeroStats heroStats, int position)
    {
        double averagePickRate = (
            heroStats.PickRateCoreMid +
            heroStats.PickRateCoreSafe +
            heroStats.PickRateCoreOff +
            heroStats.PickRateSupportSafe +
            heroStats.PickRateSupportOff
        ) / 5.0;

        double positionPickRate = position switch
        {
            1 => heroStats.PickRateCoreSafe,
            2 => heroStats.PickRateCoreMid,
            3 => heroStats.PickRateCoreOff,
            4 => heroStats.PickRateSupportOff,
            5 => heroStats.PickRateSupportSafe,
            _ => 0
        };

        if (averagePickRate <= 0)
            return false;

        double threshold = averagePickRate * 0.20;
        return positionPickRate >= threshold;
    }

    private void DisplayCounterPicksTable(Hero selectedHero, List<CounterPickInfo> counterPicks, int? selectedPosition)
    {
        Console.WriteLine($"\n=== Counters for {selectedHero.LocalizedName} ===");

        if (selectedPosition.HasValue)
        {
            string positionName = selectedPosition.Value switch
            {
                1 => "Core Safe (Pos 1)",
                2 => "Core Mid (Pos 2)",
                3 => "Core Off (Pos 3)",
                4 => "Support Off (Pos 4)",
                5 => "Support Safe (Pos 5)",
                _ => "Unknown"
            };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Position Filter: {positionName}");
            Console.ResetColor();
        }

        var favoriteHeroes = _favoriteHeroesStorage.Load();
        var favoriteHeroIds = favoriteHeroes?.HeroIds ?? new List<int>();

        var favoritePicks = counterPicks
            .Where(cp => favoriteHeroIds.Contains(cp.Hero.Id))
            .OrderByDescending(cp => cp.Disadvantage)
            .ToList();

        var top3Favorites = favoritePicks.Take(3).Reverse().ToList();
        var otherFavorites = favoritePicks.Skip(3).ToList();

        var regularPicks = counterPicks
            .Where(cp => !favoriteHeroIds.Contains(cp.Hero.Id))
            .ToList();

        var mixedPicks = regularPicks.Concat(otherFavorites)
            .OrderBy(cp => cp.Disadvantage)
            .ToList();

        Console.WriteLine();
        Console.WriteLine($"{"Hero",-25} {"Disadvantage",14}");
        Console.WriteLine(new string('-', 44));

        foreach (var pick in mixedPicks)
        {
            bool isFavorite = favoriteHeroIds.Contains(pick.Hero.Id);

            if (isFavorite)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }

            Console.Write($"{pick.Hero.LocalizedName,-25}");

            if (isFavorite)
            {
                Console.ResetColor();
            }

            Console.Write(" ");
            SetDisadvantageColor(pick.Disadvantage);
            Console.Write($"{pick.Disadvantage,13:0.00}%");
            Console.ResetColor();

            if (isFavorite)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(" (Favorite)");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        foreach (var pick in top3Favorites)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{pick.Hero.LocalizedName,-25}");
            Console.ResetColor();

            Console.Write(" ");
            SetDisadvantageColor(pick.Disadvantage);
            Console.Write($"{pick.Disadvantage,13:0.00}%");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(" (Favorite)");
            Console.ResetColor();

            Console.WriteLine();
        }
    }


    private void SetDisadvantageColor(double disadvantage)
    {
        if (disadvantage > 3)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (disadvantage > 1)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else if (disadvantage > -0.5)
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
    }


    private class CounterPickInfo
    {
        public Hero Hero { get; set; } = null!;
        public double Disadvantage { get; set; }
    }
}
