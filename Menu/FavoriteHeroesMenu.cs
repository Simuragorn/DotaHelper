using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class FavoriteHeroesMenu : IMenu
{
    private readonly IStorageService<FavoriteHeroes> _favoriteHeroesStorage;
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly IStorageService<DotabuffStatsData> _dotabuffStatsStorage;
    private List<Hero>? _heroes;
    private FavoriteHeroes? _favoriteHeroes;

    public FavoriteHeroesMenu(
        IStorageService<FavoriteHeroes> favoriteHeroesStorage,
        IStorageService<List<Hero>> heroStorageService,
        IStorageService<DotabuffStatsData> dotabuffStatsStorage)
    {
        _favoriteHeroesStorage = favoriteHeroesStorage;
        _heroStorageService = heroStorageService;
        _dotabuffStatsStorage = dotabuffStatsStorage;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Favorite Heroes Management ===\n");

        _favoriteHeroes = _favoriteHeroesStorage.Load() ?? new FavoriteHeroes();
        _heroes = _heroStorageService.Load();
        var statsData = _dotabuffStatsStorage.Load();

        if (_heroes == null || _heroes.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No heroes data found. Please fetch hero statistics first.");
            Console.ResetColor();
            return;
        }

        if (_favoriteHeroes.HeroIds.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("No favorite heroes yet. Add some below!");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        var displayList = new List<FavoriteHeroDisplay>();

        foreach (var heroId in _favoriteHeroes.HeroIds)
        {
            var hero = _heroes.FirstOrDefault(h => h.Id == heroId);
            if (hero == null) continue;

            var heroStats = statsData?.Stats.FirstOrDefault(s => s.Id == heroId);
            var winRate = heroStats?.WinRate ?? 0.0;
            var positions = GetViablePositions(heroStats);

            displayList.Add(new FavoriteHeroDisplay
            {
                LocalizedName = hero.LocalizedName,
                Roles = positions,
                WinRate = winRate
            });
        }

        var sortedList = displayList.OrderByDescending(h => h.WinRate).ToList();

        Console.WriteLine($"{"Hero",-25} {"Positions",-30} {"WinRate",10}");
        Console.WriteLine(new string('-', 70));

        foreach (var item in sortedList)
        {
            Console.Write($"{item.LocalizedName,-25} {item.Roles,-30} ");

            SetWinRateColor(item.WinRate);
            Console.Write($"{item.WinRate,9:0.00}%");
            Console.ResetColor();

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    public async Task ExecuteAsync()
    {
        _heroes = _heroStorageService.Load();
        _favoriteHeroes = _favoriteHeroesStorage.Load() ?? new FavoriteHeroes();

        if (_heroes == null || _heroes.Count == 0)
        {
            Console.Clear();
            Console.WriteLine("=== Favorite Heroes Management ===\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No heroes data found. Please fetch hero statistics first.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
            return;
        }

        while (true)
        {
            Display();

            var selectedHero = GetHeroInput();

            if (selectedHero == null)
            {
                return;
            }

            ToggleFavoriteHero(selectedHero);

            await Task.CompletedTask;
        }
    }

    private Hero? GetHeroInput()
    {
        string input = string.Empty;
        Hero? expectedHero = null;

        Console.WriteLine("Type hero name to add/remove (or press Escape to return):");
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
        Console.Write($"Type hero name: {input}");
    }

    private void ToggleFavoriteHero(Hero hero)
    {
        if (_favoriteHeroes == null)
        {
            _favoriteHeroes = new FavoriteHeroes();
        }

        Console.WriteLine();
        Console.WriteLine();

        if (_favoriteHeroes.HeroIds.Contains(hero.Id))
        {
            _favoriteHeroes.HeroIds.Remove(hero.Id);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"✓ Removed {hero.LocalizedName} from favorites");
            Console.ResetColor();
        }
        else
        {
            _favoriteHeroes.HeroIds.Add(hero.Id);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Added {hero.LocalizedName} to favorites");
            Console.ResetColor();
        }

        _favoriteHeroes.LastModified = DateTime.UtcNow;
        _favoriteHeroesStorage.Save(_favoriteHeroes);

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private string GetViablePositions(DotabuffHeroStats? heroStats)
    {
        if (heroStats == null)
        {
            return "N/A";
        }

        var viablePositions = new List<string>();

        if (IsHeroViableForPosition(heroStats, 1))
            viablePositions.Add("1");
        if (IsHeroViableForPosition(heroStats, 2))
            viablePositions.Add("2");
        if (IsHeroViableForPosition(heroStats, 3))
            viablePositions.Add("3");
        if (IsHeroViableForPosition(heroStats, 4))
            viablePositions.Add("4");
        if (IsHeroViableForPosition(heroStats, 5))
            viablePositions.Add("5");

        return viablePositions.Count > 0 ? string.Join(", ", viablePositions) : "N/A";
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

    private void SetWinRateColor(double winRate)
    {
        if (winRate > 52)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (winRate >= 50)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else if (winRate >= 48)
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
    }

    private class FavoriteHeroDisplay
    {
        public string LocalizedName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public double WinRate { get; set; }
    }
}
