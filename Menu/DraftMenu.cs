using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class DraftMenu : IMenu
{
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private List<Hero>? _heroes;

    public DraftMenu(IStorageService<List<Hero>> heroStorageService)
    {
        _heroStorageService = heroStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Draft ===");
    }

    public Task ExecuteAsync()
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
            return Task.CompletedTask;
        }

        var selectedHero = GetHeroInput();

        if (selectedHero != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Selected hero: {selectedHero.LocalizedName}");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to return to main menu...");
        Console.ReadKey();
        return Task.CompletedTask;
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
}
