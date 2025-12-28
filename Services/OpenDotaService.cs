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
        catch (Exception ex)
        {
            return null;
        }
    }
}
