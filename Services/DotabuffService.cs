using DotaHelper.Models;
using HtmlAgilityPack;

namespace DotaHelper.Services;

public class DotabuffService : IDotabuffService
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService<List<Hero>> _heroStorageService;
    private readonly IStorageService<DotabuffStatsData> _statsStorageService;

    private const string BaseUrl = "https://www.dotabuff.com/heroes";

    public DotabuffService(
        HttpClient httpClient,
        IStorageService<List<Hero>> heroStorageService,
        IStorageService<DotabuffStatsData> statsStorageService)
    {
        _httpClient = httpClient;
        _heroStorageService = heroStorageService;
        _statsStorageService = statsStorageService;

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<List<DotabuffHeroStats>?> FetchHeroStatsAsync(string patchVersion)
    {
        var heroes = _heroStorageService.Load();
        if (heroes == null || heroes.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No heroes data found. Please refetch heroes first.");
            Console.ResetColor();
            return null;
        }

        var positions = new[] { "core-mid", "core-safe", "core-off", "support-safe", "support-off" };
        var positionData = new Dictionary<string, Dictionary<string, PositionStats>>();

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

        var heroStats = MergePositionData(positionData, heroes);

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

    private async Task<Dictionary<string, PositionStats>?> FetchPositionStatsAsync(string patchVersion, string position)
    {
        try
        {
            string url = $"{BaseUrl}?show=heroes&view=meta&mode=all-pick&date={patchVersion}&position={position}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"HTTP {response.StatusCode} for {position}");
                return null;
            }

            string html = await response.Content.ReadAsStringAsync();

            return ParseHtmlTable(html);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Request timeout");
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

            foreach (var row in rows)
            {
                var heroLinks = row.SelectNodes(".//td[1]//a[starts-with(@href, '/heroes/')]");
                if (heroLinks == null || heroLinks.Count < 2) continue;

                var heroLink = heroLinks[1];
                string heroName = heroLink.InnerText.Trim();

                var winRateNode = row.SelectSingleNode(".//td[3]//span");
                var pickRateNode = row.SelectSingleNode(".//td[5]//span");
                var banRateNode = row.SelectSingleNode(".//td[7]//span");

                double winRate = ParsePercentage(winRateNode?.InnerText);
                double pickRate = ParsePercentage(pickRateNode?.InnerText);
                double banRate = ParsePercentage(banRateNode?.InnerText);

                stats[heroName] = new PositionStats
                {
                    WinRate = winRate,
                    PickRate = pickRate,
                    BanRate = banRate
                };
            }

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
        var processedHeroes = new HashSet<string>();

        var firstPositionData = positionData.Values.FirstOrDefault();
        if (firstPositionData == null) return heroStats;

        foreach (var (heroName, _) in firstPositionData)
        {
            var hero = FindHeroByName(heroName, heroes);
            if (hero == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not match hero '{heroName}' to existing heroes");
                Console.ResetColor();
                continue;
            }

            if (processedHeroes.Contains(hero.LocalizedName))
                continue;

            processedHeroes.Add(hero.LocalizedName);

            var stat = new DotabuffHeroStats
            {
                Id = hero.Id,
                LocalizedName = hero.LocalizedName,
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

    private Hero? FindHeroByName(string dotabuffName, List<Hero> heroes)
    {
        string normalized = NormalizeName(dotabuffName);

        return heroes.FirstOrDefault(h =>
            NormalizeName(h.LocalizedName).Equals(normalized,
            StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizeName(string name)
    {
        return name.Replace("'", "")
                   .Replace("-", " ")
                   .Replace("  ", " ")
                   .Trim()
                   .ToLowerInvariant();
    }

    private class PositionStats
    {
        public double WinRate { get; set; }
        public double PickRate { get; set; }
        public double BanRate { get; set; }
    }
}
