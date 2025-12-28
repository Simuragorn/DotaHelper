using DotaHelper.Services;

namespace DotaHelper.Menu;

public class MainMenu : IMenu
{
    private readonly DraftMenu _draftMenu;
    private readonly ProfileMenu _profileMenu;
    private readonly IUserProfileService _profileService;
    private readonly IOpenDotaService _openDotaService;
    private bool _isFirstRun = true;
    private string? _personaName;

    public MainMenu(DraftMenu draftMenu, ProfileMenu profileMenu, IUserProfileService profileService, IOpenDotaService openDotaService)
    {
        _draftMenu = draftMenu;
        _profileMenu = profileMenu;
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
        }
        Console.WriteLine("\n1. Draft");
        Console.WriteLine("2. Profile");
        Console.WriteLine("3. Exit");
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
                    _personaName = await _openDotaService.GetPlayerPersonaNameAsync(profile.DotaId);
                }
            }
            else
            {
                await _profileMenu.ExecuteAsync();
                var profile = _profileService.GetProfile();
                if (profile != null)
                {
                    _personaName = await _openDotaService.GetPlayerPersonaNameAsync(profile.DotaId);
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
                        _personaName = await _openDotaService.GetPlayerPersonaNameAsync(profile.DotaId);
                    }
                    break;
                case "3":
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
}
