using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Handles the complete logout workflow including data cleanup and navigation.
    /// </summary>
    public class LogoutService
    {
        private readonly SteamAuthenticationService _authService;
        private readonly SecureCredentialsService _credentialsService;
        private readonly GameDatabase _gameDatabase;

        public event EventHandler<LogoutEventArgs>? LogoutCompleted;
        public event EventHandler<LogoutEventArgs>? LogoutFailed;

        public LogoutService(
            SteamAuthenticationService authService,
            SecureCredentialsService credentialsService,
            GameDatabase gameDatabase)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _credentialsService = credentialsService ?? throw new ArgumentNullException(nameof(credentialsService));
            _gameDatabase = gameDatabase ?? throw new ArgumentNullException(nameof(gameDatabase));
        }

        /// <summary>
        /// Executes the complete logout workflow.
        /// </summary>
        public async Task<LogoutResult> LogoutAsync(bool clearGameData = false)
        {
            var result = new LogoutResult
            {
                IsSuccessful = false,
                LogoutTime = DateTime.UtcNow
            };

            try
            {
                Debug.WriteLine("[Logout] Starting logout process...");

                // Step 1: Clear authentication state
                try
                {
                    _authService.Logout();
                    Debug.WriteLine("[Logout] Authentication state cleared");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Logout] Error clearing auth state: {ex.Message}");
                    result.ErrorDetails.Add("Authentication State", ex.Message);
                }

                // Step 2: Clear secure credentials
                try
                {
                    _credentialsService.ClearAllCredentials();
                    Debug.WriteLine("[Logout] Secure credentials cleared");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Logout] Error clearing credentials: {ex.Message}");
                    result.ErrorDetails.Add("Secure Credentials", ex.Message);
                }

                // Step 3: Optionally clear game data
                if (clearGameData)
                {
                    try
                    {
                        int deletedCount = await _gameDatabase.DeleteAllGamesAsync();
                        Debug.WriteLine($"[Logout] Deleted {deletedCount} games from database");
                        result.GamesDeletedCount = deletedCount;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Logout] Error clearing game data: {ex.Message}");
                        result.ErrorDetails.Add("Game Data", ex.Message);
                    }
                }

                result.IsSuccessful = true;
                Debug.WriteLine("[Logout] Logout process completed successfully");

                // Raise success event
                LogoutCompleted?.Invoke(this, new LogoutEventArgs { Result = result });

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = $"Logout failed: {ex.Message}";
                Debug.WriteLine($"[Logout] Unexpected error: {ex}");

                // Raise failure event
                LogoutFailed?.Invoke(this, new LogoutEventArgs { Result = result, Exception = ex });

                return result;
            }
        }

        /// <summary>
        /// Validates logout completion by checking if credentials were actually cleared.
        /// </summary>
        public async Task<bool> ValidateLogoutAsync()
        {
            try
            {
                var apiKey = await _credentialsService.GetApiKeyAsync();
                var steamId = await _credentialsService.GetSteamIdAsync();

                bool isLoggedOut = string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(steamId);
                Debug.WriteLine($"[Logout] Validation result: {(isLoggedOut ? "Successfully logged out" : "Still logged in")}");

                return isLoggedOut;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Logout] Validation error: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Represents the result of a logout operation.
    /// </summary>
    public class LogoutResult
    {
        public bool IsSuccessful { get; set; }
        public DateTime LogoutTime { get; set; }
        public int GamesDeletedCount { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string> ErrorDetails { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Event arguments for logout events.
    /// </summary>
    public class LogoutEventArgs : EventArgs
    {
        public LogoutResult? Result { get; set; }
        public Exception? Exception { get; set; }
    }
}