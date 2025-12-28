using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class DraftMenu : IMenu
{
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly IOpenDotaService _openDotaService;
    private readonly IUserProfileService _profileService;
    private readonly List<DotabuffHeroStats> _dotabuffStats;
    private List<Hero>? _heroes;

    public DraftMenu(
        IStorageService<List<Hero>> heroStorageService,
        IOpenDotaService openDotaService,
        IUserProfileService profileService,
        List<DotabuffHeroStats> dotabuffStats)
    {
        _heroStorageService = heroStorageService;
        _openDotaService = openDotaService;
        _profileService = profileService;
        _dotabuffStats = dotabuffStats;
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
            Console.WriteLine("Please use \"Refetch Heroes\" option from main menu first.");
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
        var profile = _profileService.GetProfile();
        if (profile == null)
        {
            Console.WriteLine("\nNo user profile found. Cannot load play history.");
            return;
        }

        while (true)
        {
            Console.WriteLine("\nProcessing matchup data...");

            var playerHeroes = await _openDotaService.GetPlayerHeroesAsync(profile.DotaId);
            var matchups = await _openDotaService.GetHeroMatchupsAsync(selectedHero.Id);

            if (matchups == null || matchups.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to fetch matchup data.");
                Console.ResetColor();
                Console.Write("\nRetry? (y/n): ");
                string? response = Console.ReadLine()?.Trim().ToLower();

                if (response != "y")
                {
                    return;
                }

                continue;
            }

            var counterPicks = ProcessCounterPicks(matchups, playerHeroes, _dotabuffStats);
            DisplayCounterPicksTable(selectedHero, counterPicks);
            break;
        }
    }

    private List<CounterPickInfo> ProcessCounterPicks(List<HeroMatchup> matchups, List<PlayerHero>? playerHeroes, List<DotabuffHeroStats> dotabuffStats)
    {
        var counterPicks = new List<CounterPickInfo>();

        foreach (var matchup in matchups)
        {
            var hero = _heroes?.FirstOrDefault(h => h.Id == matchup.HeroId);
            if (hero == null) continue;

            if (matchup.GamesPlayed < 50) continue;

            var stats = dotabuffStats.FirstOrDefault(hs => hs.Id == matchup.HeroId);
            if (stats == null) continue;

            var playerHero = playerHeroes?.FirstOrDefault(ph => ph.HeroId == matchup.HeroId);

            double matchupWinRate = CalculateWinRate(matchup.GamesPlayed, matchup.Wins);
            double avgWinRate = stats.WinRate;
            double advantage = matchupWinRate - avgWinRate;
            if (hero.Name == "npc_dota_hero_earthshaker")
            {
                ;
            }
            var counterPick = new CounterPickInfo
            {
                Hero = hero,
                Advantage = advantage,
                UserGames = playerHero?.Games,
                LastPlayed = playerHero?.LastPlayed,
                NeverPlayed = playerHero == null || playerHero.Games == 0
            };

            counterPicks.Add(counterPick);
        }

        var goodCounters = counterPicks.Where(cp => cp.Advantage >= 0).ToList();

        var playedBadCounters = counterPicks
            .Where(cp => cp.Advantage < 0 && !cp.NeverPlayed)
            .Take(3)
            .ToList();

        var allDisplayed = goodCounters.Concat(playedBadCounters).ToList();

        return allDisplayed
            .OrderBy(cp => GetColorPriority(cp))
            .ThenByDescending(cp => cp.Advantage)
            .ToList();
    }

    private int GetColorPriority(CounterPickInfo pick)
    {
        if (!pick.NeverPlayed && pick.LastPlayed.HasValue && IsPlayedRecently(pick.LastPlayed.Value))
        {
            return 0;
        }
        else if (!pick.NeverPlayed)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }

    private double CalculateWinRate(int gamesPlayed, int wins)
    {
        if (gamesPlayed == 0) return 0;
        return (wins / (double)gamesPlayed) * 100;
    }

    private void DisplayCounterPicksTable(Hero selectedHero, List<CounterPickInfo> counterPicks)
    {
        Console.WriteLine($"\n=== Counter Picks Against {selectedHero.LocalizedName} ===\n");

        Console.WriteLine($"{"Hero",-25} {"Advantage",12}");
        Console.WriteLine(new string('-', 42));

        foreach (var pick in counterPicks)
        {
            SetPlayHistoryColor(pick);
            Console.Write($"{pick.Hero.LocalizedName,-25}");
            Console.ResetColor();

            Console.Write(" ");
            SetAdvantageColor(pick.Advantage);
            Console.Write($"{pick.Advantage,11:+0.0;-0.0;+0.0}%");
            Console.ResetColor();

            Console.WriteLine();
        }
    }

    private void SetPlayHistoryColor(CounterPickInfo pick)
    {
        if (pick.NeverPlayed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        else if (pick.LastPlayed.HasValue && IsPlayedRecently(pick.LastPlayed.Value))
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
    }

    private void SetAdvantageColor(double advantage)
    {
        if (advantage >= 4)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        else if (advantage >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (advantage >= 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
    }

    private void SetWinRateColor(double winRate)
    {
        if (winRate > 50)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (winRate >= 48)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
    }

    private bool IsPlayedRecently(long lastPlayed)
    {
        var lastPlayedDate = DateTimeOffset.FromUnixTimeSeconds(lastPlayed);
        var oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7);
        return lastPlayedDate >= oneWeekAgo;
    }

    private class CounterPickInfo
    {
        public Hero Hero { get; set; } = null!;
        public double Advantage { get; set; }
        public int? UserGames { get; set; }
        public long? LastPlayed { get; set; }
        public bool NeverPlayed { get; set; }
    }
}
