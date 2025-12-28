using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class RefetchHeroesMenu : IMenu
{
    private readonly IOpenDotaService _openDotaService;
    private readonly IStorageService<List<Hero>> _heroStorageService;

    public RefetchHeroesMenu(IOpenDotaService openDotaService, IStorageService<List<Hero>> heroStorageService)
    {
        _openDotaService = openDotaService;
        _heroStorageService = heroStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Refetch Heroes ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        while (true)
        {
            Console.WriteLine("\nFetching heroes data from OpenDota API...");

            var heroes = await _openDotaService.GetAllHeroesAsync();

            if (heroes != null && heroes.Count > 0)
            {
                _heroStorageService.Save(heroes);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nSuccess! {heroes.Count} heroes fetched and saved locally.");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to return to main menu...");
                Console.ReadKey();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nFailed to fetch heroes data.");
            Console.ResetColor();
            Console.Write("\nRetry? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            if (response != "y")
            {
                return;
            }

            Display();
        }
    }
}
