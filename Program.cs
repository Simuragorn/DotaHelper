using DotaHelper.Menu;
using DotaHelper.Models;
using DotaHelper.Services;
using System.Runtime.InteropServices;

namespace DotaHelper;

using System.Text;

internal class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CONSOLE_FONT_INFOEX
    {
        public uint cbSize;
        public uint nFont;
        public short dwFontSizeX;
        public short dwFontSizeY;
        public uint FontFamily;
        public uint FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FaceName;
    }

    private const int STD_OUTPUT_HANDLE = -11;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            SetConsoleFontSize(24);
            SetBufferSize();

            var cookieContainer = new System.Net.CookieContainer();
            var httpClient = new HttpClient();
            var heroStorageService = new JsonStorageService<List<Hero>>("heroes.json");
            var patchStorageService = new JsonStorageService<Patch>("patch.json");
            var dotabuffStatsStorage = new JsonStorageService<DotabuffStatsData>("dotabuff-stats.json");
            var countersCache = new JsonStorageService<HeroCountersCache>("counters-cache.json");
            var favoriteHeroesStorage = new JsonStorageService<FavoriteHeroes>("favorite-heroes.json");
            using var dotabuffService = new DotabuffService(httpClient, heroStorageService, dotabuffStatsStorage, countersCache);

            var patchMenu = new PatchMenu(patchStorageService, dotabuffStatsStorage, countersCache, dotabuffService);

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

            var draftMenu = new DraftMenu(heroStorageService, dotabuffStats, dotabuffService, patchStorageService, favoriteHeroesStorage);
            var countersCacheMenu = new CountersCacheMenu(dotabuffService, heroStorageService, patchStorageService);
            var favoriteHeroesMenu = new FavoriteHeroesMenu(favoriteHeroesStorage, heroStorageService, dotabuffStatsStorage);
            var mainMenu = new MainMenu(draftMenu, patchMenu, countersCacheMenu, favoriteHeroesMenu, patchStorageService, dotabuffStatsStorage, dotabuffService, heroStorageService, favoriteHeroesStorage);

            await mainMenu.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void SetBufferSize()
    {
        int width = Math.Max(Console.BufferWidth, Console.WindowWidth);
        int height = Math.Max(5000, Console.WindowHeight);

        Console.SetBufferSize(width, height);
    }

    private static void SetConsoleFontSize(short fontSize)
    {
        try
        {
            IntPtr hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);
            if (hConsoleOutput == IntPtr.Zero)
                return;

            CONSOLE_FONT_INFOEX fontInfo = new CONSOLE_FONT_INFOEX
            {
                cbSize = (uint)Marshal.SizeOf<CONSOLE_FONT_INFOEX>(),
                nFont = 0,
                dwFontSizeX = 0,
                dwFontSizeY = fontSize,
                FontFamily = 54,
                FontWeight = 400,
                FaceName = "Consolas"
            };

            SetCurrentConsoleFontEx(hConsoleOutput, false, ref fontInfo);
        }
        catch
        {
        }
    }
}
