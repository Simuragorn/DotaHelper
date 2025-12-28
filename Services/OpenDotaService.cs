using System.Text.Json;
using DotaHelper.Models;

namespace DotaHelper.Services;

public class OpenDotaService : IOpenDotaService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.opendota.com/api/players/";

    public OpenDotaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetPlayerPersonaNameAsync(string dotaId)
    {
        try
        {
            string url = $"{BaseUrl}{dotaId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            var playerProfile = JsonSerializer.Deserialize<OpenDotaPlayerProfile>(json);

            return playerProfile?.Profile?.Personaname;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PlayerHero>?> GetPlayerHeroesAsync(string dotaId)
    {
        try
        {
            string url = $"{BaseUrl}{dotaId}/heroes";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            var heroes = JsonSerializer.Deserialize<List<PlayerHero>>(json);

            return heroes;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Hero>?> GetAllHeroesAsync()
    {
        try
        {
            string url = "https://api.opendota.com/api/heroes";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            var heroes = JsonSerializer.Deserialize<List<Hero>>(json);

            return heroes;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<HeroMatchup>?> GetHeroMatchupsAsync(int heroId)
    {
        try
        {
            string url = $"https://api.opendota.com/api/heroes/{heroId}/matchups";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            var matchups = JsonSerializer.Deserialize<List<HeroMatchup>>(json);

            return matchups;
        }
        catch
        {
            return null;
        }
    }
}
