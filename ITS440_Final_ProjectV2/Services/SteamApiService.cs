using System.Diagnostics;

using System.Net.Http.Json;

namespace ITS440_Final_ProjectV2.Services
{
    public class SteamGame
    {
        public int appid { get; set; }
        public string name { get; set; }
    }

    public class SteamGameResponse
    {
        public GameList response { get; set; }

        public class GameList
        {
            public int game_count { get; set; }
            public List<SteamGame> games { get; set; }
        }
    }

    public class SteamApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // Validate API key with a simple test endpoint
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            try
            {
                string url = $"https://api.steampowered.com/ISteamWebAPIUtil/GetSupportedAPIList/v1/?key={apiKey}";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Fetch user's Steam game library
        public async Task<List<SteamGame>> GetUserGamesAsync(string steamId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Steam ID and API Key are required.");

            try
            {
                string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                             $"?steamid={steamId}&key={apiKey}&format=json&include_appinfo=true&include_played_free_games=true";

                var response = await _httpClient.GetFromJsonAsync<SteamGameResponse>(url);
                
                Debug.WriteLine($"[Steam API] Game count: {response?.response?.game_count}");
                Debug.WriteLine($"[Steam API] Games returned: {response?.response?.games?.Count ?? 0}");
                
                return response?.response?.games ?? new List<SteamGame>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Steam API] Error fetching games: {ex.Message}");
                throw;
            }
        }
    }
}