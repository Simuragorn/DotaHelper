using DotaHelper.Services;

namespace DotaHelper.Menu;

public class MainMenu : IMenu
{
    private readonly DraftMenu _draftMenu;
    private readonly ProfileMenu _profileMenu;
    private readonly RefetchHeroesMenu _refetchHeroesMenu;
    private readonly IUserProfileService _profileService;
    private readonly IOpenDotaService _openDotaService;
    private bool _isFirstRun = true;
    private string? _personaName;
    private bool _hasMatches;

    public MainMenu(DraftMenu draftMenu, ProfileMenu profileMenu, RefetchHeroesMenu refetchHeroesMenu, IUserProfileService profileService, IOpenDotaService openDotaService)
    {
        _draftMenu = draftMenu;
        _profileMenu = profileMenu;
        _refetchHeroesMenu = refetchHeroesMenu;
        _profileService = profileService;
        _openDotaService = openDotaService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Dota Helper ===");
        if (!string.IsNullOrEmpty(_personaName))
        {
            Console.Write("Welcome, ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(_personaName);
            Console.ResetColor();
            Console.WriteLine("!");

            if (_hasMatches)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Matches loaded successfully");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No matches found");
                Console.ResetColor();
                Console.WriteLine("Please allow Expose Public Match Data in Dota privacy settings and refresh statistic in opendota profile");
            }
        }
        Console.WriteLine("\n1. Draft");
        Console.WriteLine("2. Profile");
        Console.WriteLine("3. Refetch Heroes");
        Console.WriteLine("4. Exit");
        Console.Write("\nSelect an option: ");
    }

    public async Task ExecuteAsync()
    {
        if (_isFirstRun)
        {
            _isFirstRun = false;

            if (_profileService.HasProfile())
            {
                var profile = _profileService.GetProfile();
                if (profile != null)
                {
                    await LoadPlayerDataAsync(profile.DotaId);
                }
            }
            else
            {
                await _profileMenu.ExecuteAsync();
                var profile = _profileService.GetProfile();
                if (profile != null)
                {
                    await LoadPlayerDataAsync(profile.DotaId);
                }
            }
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
                    await _profileMenu.ExecuteAsync();
                    var profile = _profileService.GetProfile();
                    if (profile != null)
                    {
                        await LoadPlayerDataAsync(profile.DotaId);
                    }
                    break;
                case "3":
                    await _refetchHeroesMenu.ExecuteAsync();
                    break;
                case "4":
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

    private async Task LoadPlayerDataAsync(string dotaId)
    {
        _personaName = await _openDotaService.GetPlayerPersonaNameAsync(dotaId);

        var heroes = await _openDotaService.GetPlayerHeroesAsync(dotaId);
        _hasMatches = heroes != null && heroes.Any(h => h.Games > 0);
    }
}
