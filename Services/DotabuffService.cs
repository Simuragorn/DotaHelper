using DotaHelper.Models;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace DotaHelper.Services;

public class DotabuffService : IDotabuffService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly IStorageService<DotabuffStatsData> _statsStorageService;
    private List<Hero> _discoveredHeroes = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private bool _disposed;

    private const string BaseUrl = "https://www.dotabuff.com/heroes";

    public DotabuffService(
        HttpClient httpClient,
        IStorageService<List<Hero>> heroStorageService,
        IStorageService<DotabuffStatsData> statsStorageService)
    {
        _httpClient = httpClient;
        _heroStorageService = heroStorageService;
        _statsStorageService = statsStorageService;

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Arch", "\"x86\"");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Bitness", "\"64\"");
        _httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Full-Version", "\"131.0.6778.140\"");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
    }

    private async Task EnsureBrowserInitializedAsync()
    {
        if (_browser != null)
            return;

        await _browserLock.WaitAsync();
        try
        {
            if (_browser != null)
                return;

            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox"
                }
            });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to initialize browser: {ex.Message}");
            Console.ResetColor();
            throw;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private int GetHeroIdFromUrl(string heroUrl)
    {
        if (string.IsNullOrEmpty(heroUrl))
            return 0;

        int hash = 0;
        foreach (char c in heroUrl)
        {
            hash = ((hash << 5) - hash) + c;
        }
        return Math.Abs(hash);
    }

    public async Task<List<DotabuffHeroStats>?> FetchHeroStatsAsync(string patchVersion)
    {
        var positions = new[] { "core-mid", "core-safe", "core-off", "support-safe", "support-off" };
        var positionData = new Dictionary<string, Dictionary<string, PositionStats>>();

        _discoveredHeroes.Clear();

        for (int i = 0; i < positions.Length; i++)
        {
            Console.WriteLine($"Fetching stats for {positions[i]}... ({i + 1}/5)");

            var stats = await FetchPositionStatsAsync(patchVersion, positions[i]);

            if (stats == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to fetch stats for {positions[i]}");
                Console.ResetColor();
                return null;
            }

            positionData[positions[i]] = stats;

            if (i < positions.Length - 1)
            {
                await Task.Delay(5000);
            }
        }

        var uniqueHeroes = _discoveredHeroes
            .GroupBy(h => h.Id)
            .Select(g => g.First())
            .OrderBy(h => h.LocalizedName)
            .ToList();

        if (uniqueHeroes.Count > 0)
        {
            _heroStorageService.Save(uniqueHeroes);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Built heroes list with {uniqueHeroes.Count} heroes");
            Console.ResetColor();
        }

        var heroStats = MergePositionData(positionData, uniqueHeroes);

        if (heroStats.Count == 0)
        {
            return null;
        }

        var statsData = new DotabuffStatsData
        {
            Stats = heroStats,
            LastFetched = DateTime.UtcNow,
            PatchVersion = patchVersion
        };

        _statsStorageService.Save(statsData);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Successfully fetched stats for {heroStats.Count} heroes");
        Console.ResetColor();

        return heroStats;
    }

    public List<DotabuffHeroStats>? GetCachedStats()
    {
        var data = _statsStorageService.Load();
        return data?.Stats;
    }

    public bool HasValidCache(string currentPatch)
    {
        var data = _statsStorageService.Load();

        if (data == null || data.Stats == null || data.Stats.Count == 0)
        {
            return false;
        }

        if (data.PatchVersion != currentPatch)
        {
            return false;
        }

        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        if (data.LastFetched < oneDayAgo)
        {
            return false;
        }

        return true;
    }

    public async Task<List<DotabuffCounter>?> FetchHeroCountersAsync(string heroUrl, string patchVersion)
    {
        try
        {
            await EnsureBrowserInitializedAsync();

            if (_browser == null)
            {
                Console.WriteLine("Browser not initialized");
                return null;
            }

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "America/New_York",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                }
            });

            var page = await context.NewPageAsync();

            string url = $"{BaseUrl}/{heroUrl}/counters?date=patch_{patchVersion}";
            Console.WriteLine($"Navigating to: {url}");

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            if (response == null || !response.Ok)
            {
                Console.WriteLine($"Navigation failed: {response?.Status ?? 0}");
                await context.CloseAsync();
                return null;
            }

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.WaitForSelectorAsync("tr[data-link-to]", new PageWaitForSelectorOptions
            {
                Timeout = 10000,
                State = WaitForSelectorState.Visible
            });

            string html = await page.ContentAsync();

            await context.CloseAsync();

            return ParseCountersHtml(html);
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Request timeout while loading counters page");
            return null;
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"Browser error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching hero counters: {ex.Message}");
            return null;
        }
    }

    private List<DotabuffCounter>? ParseCountersHtml(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//tr[@data-link-to]");

            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("No counter data found in HTML");
                return new List<DotabuffCounter>();
            }

            var heroes = _heroStorageService.Load();
            if (heroes == null || heroes.Count == 0)
            {
                Console.WriteLine("No heroes data available for matching");
                return null;
            }

            var counters = new List<DotabuffCounter>();

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 5) continue;

                var heroLink = cells[1].SelectSingleNode(".//a[@class='link-type-hero']");
                if (heroLink == null) continue;

                string heroName = heroLink.InnerText.Trim();

                string heroUrl = heroLink.GetAttributeValue("href", "")
                                         .Replace("/heroes/", "")
                                         .Trim();

                int heroId = GetHeroIdFromUrl(heroUrl);

                double disadvantage = ParseDataValue(cells[2].GetAttributeValue("data-value", "0"));
                double winRate = ParseDataValue(cells[3].GetAttributeValue("data-value", "0"));
                int matches = (int)ParseDataValue(cells[4].GetAttributeValue("data-value", "0"));

                counters.Add(new DotabuffCounter
                {
                    HeroId = heroId,
                    HeroName = heroName,
                    Disadvantage = disadvantage,
                    WinRate = winRate,
                    MatchesPlayed = matches
                });
            }

            return counters.OrderBy(c => c.Disadvantage).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTML parsing error: {ex.Message}");
            return null;
        }
    }

    private double ParseDataValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0.0;

        if (double.TryParse(value, out double result))
            return result;

        return 0.0;
    }

    private async Task<Dictionary<string, PositionStats>?> FetchPositionStatsAsync(string patchVersion, string position)
    {
        try
        {
            await EnsureBrowserInitializedAsync();

            if (_browser == null)
            {
                Console.WriteLine("Browser not initialized");
                return null;
            }

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "America/New_York",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                }
            });

            var page = await context.NewPageAsync();

            string url = $"{BaseUrl}?show=heroes&view=meta&mode=all-pick&date={patchVersion}&position={position}";

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 60000
            });

            if (response == null || !response.Ok)
            {
                Console.WriteLine($"Navigation failed: {response?.Status ?? 0}");
                await context.CloseAsync();
                return null;
            }

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Task.Delay(2000);

            await page.WaitForSelectorAsync("tbody tr", new PageWaitForSelectorOptions
            {
                Timeout = 15000,
                State = WaitForSelectorState.Attached
            });

            string html = await page.ContentAsync();

            await context.CloseAsync();

            return ParseHtmlTable(html);
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Request timeout while loading position stats page");
            return null;
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"Browser error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {position}: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, PositionStats>? ParseHtmlTable(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//tbody//tr");

            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("No hero rows found in HTML");
                return new Dictionary<string, PositionStats>();
            }

            var stats = new Dictionary<string, PositionStats>();
            var discoveredHeroes = new List<Hero>();

            foreach (var row in rows)
            {
                var heroLinks = row.SelectNodes(".//td[1]//a[starts-with(@href, '/heroes/')]");
                if (heroLinks == null || heroLinks.Count < 2) continue;

                var heroLink = heroLinks[1];
                string heroName = heroLink.InnerText.Trim();
                string heroUrl = heroLink.GetAttributeValue("href", "")
                                         .Replace("/heroes/", "")
                                         .Trim();

                int heroId = GetHeroIdFromUrl(heroUrl);

                var hero = new Hero
                {
                    Id = heroId,
                    Name = $"npc_dota_hero_{heroUrl.Replace("-", "_")}",
                    LocalizedName = heroName,
                    PrimaryAttr = string.Empty,
                    AttackType = string.Empty,
                    Roles = new List<string>(),
                    Legs = 0
                };

                discoveredHeroes.Add(hero);

                var winRateNode = row.SelectSingleNode(".//td[3]//span");
                var pickRateNode = row.SelectSingleNode(".//td[5]//span");
                var banRateNode = row.SelectSingleNode(".//td[7]//span");

                double winRate = ParsePercentage(winRateNode?.InnerText);
                double pickRate = ParsePercentage(pickRateNode?.InnerText);
                double banRate = ParsePercentage(banRateNode?.InnerText);

                stats[heroName] = new PositionStats
                {
                    HeroUrl = heroUrl,
                    HeroId = heroId,
                    WinRate = winRate,
                    PickRate = pickRate,
                    BanRate = banRate
                };
            }

            _discoveredHeroes.AddRange(discoveredHeroes);

            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTML parsing error: {ex.Message}");
            return null;
        }
    }

    private double ParsePercentage(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0.0;

        string cleaned = text.Replace("%", "").Trim();

        if (double.TryParse(cleaned, out double value))
            return value;

        return 0.0;
    }

    private List<DotabuffHeroStats> MergePositionData(
        Dictionary<string, Dictionary<string, PositionStats>> positionData,
        List<Hero> heroes)
    {
        var heroStats = new List<DotabuffHeroStats>();
        var processedHeroes = new HashSet<int>();

        var firstPositionData = positionData.Values.FirstOrDefault();
        if (firstPositionData == null) return heroStats;

        foreach (var (heroName, firstPositionStats) in firstPositionData)
        {
            var hero = heroes.FirstOrDefault(h => h.Id == firstPositionStats.HeroId);
            if (hero == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not find hero with ID {firstPositionStats.HeroId}");
                Console.ResetColor();
                continue;
            }

            if (processedHeroes.Contains(hero.Id))
                continue;

            processedHeroes.Add(hero.Id);

            var stat = new DotabuffHeroStats
            {
                Id = hero.Id,
                LocalizedName = hero.LocalizedName,
                HeroUrl = firstPositionStats.HeroUrl,
                LastFetched = DateTime.UtcNow,
                PatchVersion = string.Empty
            };

            if (positionData.ContainsKey("core-mid") && positionData["core-mid"].ContainsKey(heroName))
            {
                stat.WinRate = positionData["core-mid"][heroName].WinRate;
                stat.BanRate = positionData["core-mid"][heroName].BanRate;
                stat.PickRateCoreMid = positionData["core-mid"][heroName].PickRate;
            }

            if (positionData.ContainsKey("core-safe") && positionData["core-safe"].ContainsKey(heroName))
            {
                stat.PickRateCoreSafe = positionData["core-safe"][heroName].PickRate;
            }

            if (positionData.ContainsKey("core-off") && positionData["core-off"].ContainsKey(heroName))
            {
                stat.PickRateCoreOff = positionData["core-off"][heroName].PickRate;
            }

            if (positionData.ContainsKey("support-safe") && positionData["support-safe"].ContainsKey(heroName))
            {
                stat.PickRateSupportSafe = positionData["support-safe"][heroName].PickRate;
            }

            if (positionData.ContainsKey("support-off") && positionData["support-off"].ContainsKey(heroName))
            {
                stat.PickRateSupportOff = positionData["support-off"][heroName].PickRate;
            }

            heroStats.Add(stat);
        }

        return heroStats;
    }

    private class PositionStats
    {
        public string HeroUrl { get; set; } = string.Empty;
        public int HeroId { get; set; }
        public double WinRate { get; set; }
        public double PickRate { get; set; }
        public double BanRate { get; set; }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _browserLock.Wait();
            try
            {
                _browser?.CloseAsync().Wait();
                _browser?.DisposeAsync().AsTask().Wait();
                _playwright?.Dispose();
            }
            finally
            {
                _browserLock.Release();
                _browserLock.Dispose();
            }
        }

        _disposed = true;
    }
}
