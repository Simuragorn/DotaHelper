using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class MainMenu : IMenu
{
    private readonly DraftMenu _draftMenu;
    private readonly ProfileMenu _profileMenu;
    private readonly PatchMenu _patchMenu;
    private readonly RefetchStatsMenu _refetchStatsMenu;
    private readonly IStorageService<Patch> _patchStorageService;
    private readonly IUserProfileService _profileService;
    private readonly IOpenDotaService _openDotaService;
    private bool _isFirstRun = true;
    private string? _personaName;
    private bool _hasMatches;
    private string? _currentPatch;

    public MainMenu(DraftMenu draftMenu, ProfileMenu profileMenu, PatchMenu patchMenu, RefetchStatsMenu refetchStatsMenu, IStorageService<Patch> patchStorageService, IUserProfileService profileService, IOpenDotaService openDotaService)
    {
        _draftMenu = draftMenu;
        _profileMenu = profileMenu;
        _patchMenu = patchMenu;
        _refetchStatsMenu = refetchStatsMenu;
        _patchStorageService = patchStorageService;
        _profileService = profileService;
        _openDotaService = openDotaService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Dota Helper ===");

        if (!string.IsNullOrEmpty(_currentPatch))
        {
            Console.Write("Patch: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(_currentPatch);
            Console.ResetColor();
        }

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
        Console.WriteLine("3. Change Patch");
        Console.WriteLine("4. Refetch Stats");
        Console.WriteLine("5. Exit");
        Console.Write("\nSelect an option: ");
    }

    public async Task ExecuteAsync()
    {
        if (_isFirstRun)
        {
            _isFirstRun = false;

            LoadPatch();

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
                    await _patchMenu.ExecuteAsync();
                    LoadPatch();
                    break;
                case "4":
                    await _refetchStatsMenu.ExecuteAsync();
                    break;
                case "5":
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

    private void LoadPatch()
    {
        var patch = _patchStorageService.Load();
        _currentPatch = patch?.Version;
    }
}
