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
            var patchStorageService = new JsonStorageService<Patch>("patch.json");
            var dotabuffStatsStorage = new JsonStorageService<DotabuffStatsData>("dotabuff-stats.json");
            var validator = new DotaIdValidator();
            var profileService = new UserProfileService(userStorageService, validator);
            var openDotaService = new OpenDotaService(httpClient);
            var dotabuffService = new DotabuffService(httpClient, heroStorageService, dotabuffStatsStorage);

            var patchMenu = new PatchMenu(patchStorageService);

            var currentPatch = patchStorageService.Load();
            if (currentPatch == null)
            {
                await patchMenu.ExecuteAsync();
                currentPatch = patchStorageService.Load();
            }

            if (!heroStorageService.Exists())
            {
                Console.WriteLine("Fetching heroes data from OpenDota API...");
                var heroes = await openDotaService.GetAllHeroesAsync();

                if (heroes != null && heroes.Count > 0)
                {
                    heroStorageService.Save(heroes);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ {heroes.Count} heroes fetched and saved locally.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to fetch heroes data. The application may not work correctly.");
                    Console.ResetColor();
                }
            }

            List<DotabuffHeroStats> dotabuffStats;

            if (!dotabuffService.HasValidCache(currentPatch?.Version ?? ""))
            {
                Console.WriteLine("Fetching Dotabuff statistics...");
                var fetchedStats = await dotabuffService.FetchHeroStatsAsync(currentPatch?.Version ?? "");

                if (fetchedStats == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to fetch. Using cached data if available.");
                    Console.ResetColor();
                    dotabuffStats = dotabuffService.GetCachedStats() ?? new List<DotabuffHeroStats>();
                }
                else
                {
                    dotabuffStats = fetchedStats;
                }
            }
            else
            {
                dotabuffStats = dotabuffService.GetCachedStats() ?? new List<DotabuffHeroStats>();
            }

            var refetchStatsMenu = new RefetchStatsMenu(dotabuffService, patchStorageService);
            var draftMenu = new DraftMenu(heroStorageService, openDotaService, profileService, dotabuffStats);
            var profileMenu = new ProfileMenu(profileService, validator, openDotaService);
            var mainMenu = new MainMenu(draftMenu, profileMenu, patchMenu, refetchStatsMenu, patchStorageService, profileService, openDotaService);

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
