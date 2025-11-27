using System.Diagnostics;

using System.Net.Http.Json;
using ITS440_Final_ProjectV2.Models;

namespace ITS440_Final_ProjectV2.Services
{
    public class SteamUser
    {
        public class Response
        {
            public List<Player> players { get; set; }
        }

        public class Player
        {
            public string steamid { get; set; }
            public int communityvisibilitystate { get; set; }
            public int profilestate { get; set; }
            public string personaname { get; set; }
            public long lastlogoff { get; set; }
            public int commentpermission { get; set; }
            public string profileurl { get; set; }
            public string avatar { get; set; }
            public string avatarmedium { get; set; }
            public string avatarfull { get; set; }
            public string avatarhash { get; set; }
            public int personastate { get; set; }
            public string realname { get; set; }
            public string primaryclanid { get; set; }
            public long timecreated { get; set; }
            public int personastateflags { get; set; }
            public string loccountrycode { get; set; }
        }
    }

    public class SteamAuthenticationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private AuthenticationUser? _currentUser;

        public AuthenticationUser? CurrentUser => _currentUser;

        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Initiates Steam OpenID authentication flow.
        /// Returns the authentication URL that should be opened in a browser.
        /// </summary>
        public string GetAuthenticationUrl(string returnUrl)
        {
            const string steamOpenIdUrl = "https://steamcommunity.com/openid/login";

            var parameters = new Dictionary<string, string>
            {
                { "openid.ns", "http://specs.openid.net/auth/2.0" },
                { "openid.mode", "checkid_setup" },
                { "openid.return_to", returnUrl },
                { "openid.realm", "https://my-app" },
                { "openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" },
                { "openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" }
            };

            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return $"{steamOpenIdUrl}?{queryString}";
        }

        /// <summary>
        /// Validates the OpenID response from Steam and extracts the Steam ID.
        /// </summary>
        public async Task<string> ValidateAuthenticationResponseAsync(Dictionary<string, string> responseParameters)
        {
            if (responseParameters == null || !responseParameters.ContainsKey("openid.claimed_id"))
                throw new ArgumentException("Invalid authentication response from Steam.");

            var claimedId = responseParameters["openid.claimed_id"];
            var steamIdMatch = System.Text.RegularExpressions.Regex.Match(claimedId, @"(\d+)/?$");

            if (!steamIdMatch.Success)
                throw new FormatException("Could not extract Steam ID from authentication response.");

            string steamId = steamIdMatch.Groups[1].Value;

            // Verify the response with Steam servers
            var verificationParams = new Dictionary<string, string>(responseParameters)
            {
                { "openid.mode", "check_auth" }
            };

            try
            {
                var content = new FormUrlEncodedContent(verificationParams);
                var response = await _httpClient.PostAsync("https://steamcommunity.com/openid/login", content);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Steam server verification failed.");

                var responseText = await response.Content.ReadAsStringAsync();
                if (!responseText.Contains("is_valid:true"))
                    throw new Exception("Steam authentication validation failed.");

                return steamId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Steam Auth] Verification error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetches user profile information from Steam API.
        /// Requires a valid Steam API key.
        /// </summary>
        public async Task<AuthenticationUser> FetchUserProfileAsync(string steamId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Steam ID and API Key are required.");

            try
            {
                string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/" +
                             $"?steamids={steamId}&key={apiKey}";

                var response = await _httpClient.GetFromJsonAsync<SteamUser.Response>(url);

                if (response?.players == null || response.players.Count == 0)
                    throw new Exception("Could not fetch user profile from Steam.");

                var player = response.players[0];

                _currentUser = new AuthenticationUser
                {
                    SteamId = player.steamid,
                    Username = player.personaname,
                    AvatarUrl = player.avatarfull,
                    AuthenticationTime = DateTime.UtcNow
                };

                return _currentUser;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Steam Auth] Profile fetch error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            _currentUser = null;
        }
    }
}