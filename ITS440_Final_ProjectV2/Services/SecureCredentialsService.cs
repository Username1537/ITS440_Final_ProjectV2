using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    public class SecureCredentialsService
    {
        private const string ApiKeyKey = "steam_api_key";
        private const string SteamIdKey = "steam_id";
        private const string UsernameKey = "steam_username";

        /// <summary>
        /// Saves Steam API key securely using device secure storage.
        /// </summary>
        public async Task SaveApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty.");

            try
            {
                await SecureStorage.SetAsync(ApiKeyKey, apiKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error saving API key: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the saved Steam API key.
        /// </summary>
        public async Task<string> GetApiKeyAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(ApiKeyKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error retrieving API key: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves user credentials after successful authentication.
        /// </summary>
        public async Task SaveUserCredentialsAsync(string steamId, string username)
        {
            if (string.IsNullOrWhiteSpace(steamId))
                throw new ArgumentException("Steam ID cannot be empty.");

            try
            {
                await SecureStorage.SetAsync(SteamIdKey, steamId);
                await SecureStorage.SetAsync(UsernameKey, username ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error saving credentials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves saved Steam ID.
        /// </summary>
        public async Task<string> GetSteamIdAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(SteamIdKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error retrieving Steam ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves saved username.
        /// </summary>
        public async Task<string> GetUsernameAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(UsernameKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error retrieving username: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears all stored credentials.
        /// </summary>
        public void ClearAllCredentials()
        {
            try
            {
                SecureStorage.Remove(ApiKeyKey);
                SecureStorage.Remove(SteamIdKey);
                SecureStorage.Remove(UsernameKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Secure Storage] Error clearing credentials: {ex.Message}");
            }
        }
    }
}