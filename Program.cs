using DotaHelper.Menu;
using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper;

using System.Text;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var cookieContainer = new System.Net.CookieContainer();
            var httpClient = new HttpClient();
            var heroStorageService = new JsonStorageService<List<Hero>>("heroes.json");
            var patchStorageService = new JsonStorageService<Patch>("patch.json");
            var dotabuffStatsStorage = new JsonStorageService<DotabuffStatsData>("dotabuff-stats.json");
            using var dotabuffService = new DotabuffService(httpClient, heroStorageService, dotabuffStatsStorage);

            var patchMenu = new PatchMenu(patchStorageService);

            var currentPatch = patchStorageService.Load();
            if (currentPatch == null)
            {
                await patchMenu.ExecuteAsync();
                currentPatch = patchStorageService.Load();
            }

            List<DotabuffHeroStats> dotabuffStats;

            if (!dotabuffService.HasValidCache(currentPatch?.Version ?? ""))
            {
                var cachedData = dotabuffStatsStorage.Load();

                if (cachedData == null || cachedData.Stats == null || cachedData.Stats.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No statistics found. Fetching from Dotabuff...");
                    Console.ResetColor();
                }
                else
                {
                    var daysSinceLastFetch = (DateTime.UtcNow - cachedData.LastFetched).Days;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Statistics are {daysSinceLastFetch} day(s) old. Fetching fresh data from Dotabuff...");
                    Console.ResetColor();
                }

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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Using cached statistics (less than 1 day old).");
                Console.ResetColor();
                dotabuffStats = dotabuffService.GetCachedStats() ?? new List<DotabuffHeroStats>();
            }

            var refetchStatsMenu = new RefetchStatsMenu(dotabuffService, patchStorageService);
            var draftMenu = new DraftMenu(heroStorageService, dotabuffStats, dotabuffService, patchStorageService);
            var mainMenu = new MainMenu(draftMenu, patchMenu, refetchStatsMenu, patchStorageService, dotabuffStatsStorage);

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
