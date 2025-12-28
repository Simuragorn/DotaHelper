using DotaHelper.Menu;
using DotaHelper.Models;
using DotaHelper.Services;
using DotaHelper.Validation;

namespace DotaHelper;

using System.Text;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var httpClient = new HttpClient();
            var storageService = new JsonStorageService<UserProfile>("settings.json");
            var validator = new DotaIdValidator();
            var profileService = new UserProfileService(storageService, validator);
            var openDotaService = new OpenDotaService(httpClient);

            var draftMenu = new DraftMenu();
            var profileMenu = new ProfileMenu(profileService, validator, openDotaService);
            var mainMenu = new MainMenu(draftMenu, profileMenu, profileService, openDotaService);

            await mainMenu.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
