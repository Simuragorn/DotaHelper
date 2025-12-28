using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class DraftMenu : IMenu
{
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly List<DotabuffHeroStats> _dotabuffStats;
    private readonly IDotabuffService _dotabuffService;
    private readonly IStorageService<Patch> _patchStorageService;
    private List<Hero>? _heroes;

    public DraftMenu(
        IStorageService<List<Hero>> heroStorageService,
        List<DotabuffHeroStats> dotabuffStats,
        IDotabuffService dotabuffService,
        IStorageService<Patch> patchStorageService)
    {
        _heroStorageService = heroStorageService;
        _dotabuffStats = dotabuffStats;
        _dotabuffService = dotabuffService;
        _patchStorageService = patchStorageService;
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

            var counterPicks = ProcessCounterPicks(counters);
            DisplayCounterPicksTable(selectedHero, counterPicks);
            break;
        }
    }

    private List<CounterPickInfo> ProcessCounterPicks(List<DotabuffCounter> counters)
    {
        var counterPicks = new List<CounterPickInfo>();

        foreach (var counter in counters)
        {
            var hero = _heroes?.FirstOrDefault(h => h.Id == counter.HeroId);
            if (hero == null) continue;

            var counterPick = new CounterPickInfo
            {
                Hero = hero,
                Disadvantage = counter.Disadvantage
            };

            counterPicks.Add(counterPick);
        }

        return counterPicks
            .OrderBy(cp => cp.Disadvantage)
            .ToList();
    }


    private void DisplayCounterPicksTable(Hero selectedHero, List<CounterPickInfo> counterPicks)
    {
        Console.WriteLine($"\n=== Counters for {selectedHero.LocalizedName} ===\n");

        Console.WriteLine($"{"Hero",-25} {"Disadvantage",14}");
        Console.WriteLine(new string('-', 44));

        foreach (var pick in counterPicks)
        {
            Console.Write($"{pick.Hero.LocalizedName,-25}");

            Console.Write(" ");
            SetDisadvantageColor(pick.Disadvantage);
            Console.Write($"{pick.Disadvantage,13:0.00}%");
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
