using DotaHelper.Services;
using DotaHelper.Validation;

namespace DotaHelper.Menu;

public class ProfileMenu : IMenu
{
    private readonly IUserProfileService _profileService;
    private readonly IValidator<string> _validator;
    private readonly IOpenDotaService _openDotaService;

    public ProfileMenu(IUserProfileService profileService, IValidator<string> validator, IOpenDotaService openDotaService)
    {
        _profileService = profileService;
        _validator = validator;
        _openDotaService = openDotaService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Profile ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        if (_profileService.HasProfile())
        {
            var profile = _profileService.GetProfile();
            if (profile != null)
            {
                string? personaName = await _openDotaService.GetPlayerPersonaNameAsync(profile.DotaId);

                Console.WriteLine($"Current Dota 2 ID: {profile.DotaId}");
                if (!string.IsNullOrEmpty(personaName))
                {
                    Console.Write("Player Name: ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(personaName);
                    Console.ResetColor();
                }

                Console.Write("\nEdit? (y/n): ");
                string? response = Console.ReadLine()?.Trim().ToLower();

                if (response != "y")
                {
                    return;
                }
            }
        }
        else
        {
            Console.WriteLine("Please login to https://www.opendota.com/ and copy your id (9 digital numbers) from http address.");
            Console.WriteLine("Make sure you allowed Expose Public Match Data in dota2 settings and refreshed statistic in opendota afterwards.\n");
        }

        PromptForDotaId();
    }

    private void PromptForDotaId()
    {
        while (true)
        {
            Console.Write("Enter your Dota 2 ID (9 digits) or press Enter to cancel: ");
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            if (_validator.IsValid(input))
            {
                _profileService.SaveProfile(input);
                Console.WriteLine("Profile saved successfully!");
                return;
            }

            Console.WriteLine($"{_validator.GetErrorMessage()}. Try again or press Enter to cancel.");
        }
    }
}
