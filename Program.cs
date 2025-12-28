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
            var userStorageService = new JsonStorageService<UserProfile>("settings.json");
            var heroStorageService = new JsonStorageService<List<Hero>>("heroes.json");
            var validator = new DotaIdValidator();
            var profileService = new UserProfileService(userStorageService, validator);
            var openDotaService = new OpenDotaService(httpClient);

            var draftMenu = new DraftMenu(heroStorageService, openDotaService, profileService);
            var profileMenu = new ProfileMenu(profileService, validator, openDotaService);
            var refetchHeroesMenu = new RefetchHeroesMenu(openDotaService, heroStorageService);
            var mainMenu = new MainMenu(draftMenu, profileMenu, refetchHeroesMenu, profileService, openDotaService);

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
