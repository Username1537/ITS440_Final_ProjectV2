using ITS440_Final_ProjectV2.Models;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Represents the result of an authentication attempt.
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccessful { get; set; }
        public string SteamId { get; set; }
        public AuthenticationUser User { get; set; }
        public string ErrorMessage { get; set; }
        public AuthenticationStatus Status { get; set; }
    }

    public enum AuthenticationStatus
    {
        Pending,
        Success,
        InvalidCredentials,
        NetworkError,
        Cancelled,
        UnknownError
    }

    /// <summary>
    /// Handles the complete authentication workflow and result processing.
    /// </summary>
    public class AuthenticationResultHandler
    {
        private readonly SteamAuthenticationService _authService;
        private readonly SecureCredentialsService _credentialsService;
        private readonly SteamApiService _steamApiService;

        public AuthenticationResultHandler(
            SteamAuthenticationService authService,
            SecureCredentialsService credentialsService,
            SteamApiService steamApiService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _credentialsService = credentialsService ?? throw new ArgumentNullException(nameof(credentialsService));
            _steamApiService = steamApiService ?? throw new ArgumentNullException(nameof(steamApiService));
        }

        /// <summary>
        /// Processes the authentication response from Steam.
        /// </summary>
        public async Task<AuthenticationResult> ProcessAuthenticationResponseAsync(
            Dictionary<string, string> responseParameters,
            string apiKey)
        {
            var result = new AuthenticationResult
            {
                Status = AuthenticationStatus.Pending,
                IsSuccessful = false
            };

            try
            {
                // Check for cancellation
                if (AuthenticationResponseHandler.WasAuthenticationCancelled(responseParameters))
                {
                    result.Status = AuthenticationStatus.Cancelled;
                    result.ErrorMessage = AuthenticationResponseHandler.GetErrorMessage(responseParameters);
                    Debug.WriteLine($"[Auth Result] Authentication cancelled: {result.ErrorMessage}");
                    return result;
                }

                // Validate response structure
                if (!AuthenticationResponseHandler.IsValidOpenIdResponse(responseParameters))
                {
                    result.Status = AuthenticationStatus.InvalidCredentials;
                    result.ErrorMessage = "Invalid authentication response from Steam.";
                    Debug.WriteLine("[Auth Result] Invalid OpenID response structure");
                    return result;
                }

                // Extract Steam ID
                string steamId;
                try
                {
                    steamId = AuthenticationResponseHandler.ExtractSteamIdFromResponse(responseParameters);
                }
                catch (FormatException ex)
                {
                    result.Status = AuthenticationStatus.InvalidCredentials;
                    result.ErrorMessage = $"Could not extract Steam ID: {ex.Message}";
                    Debug.WriteLine($"[Auth Result] Steam ID extraction failed: {ex.Message}");
                    return result;
                }

                result.SteamId = steamId;

                // Validate Steam ID format
                if (!System.Text.RegularExpressions.Regex.IsMatch(steamId, @"^\d+$"))
                {
                    result.Status = AuthenticationStatus.InvalidCredentials;
                    result.ErrorMessage = "Invalid Steam ID format.";
                    Debug.WriteLine("[Auth Result] Invalid Steam ID format");
                    return result;
                }

                // Verify authentication with Steam servers
                try
                {
                    string verifiedSteamId = await _authService.ValidateAuthenticationResponseAsync(responseParameters);
                    if (verifiedSteamId != steamId)
                    {
                        result.Status = AuthenticationStatus.InvalidCredentials;
                        result.ErrorMessage = "Steam ID verification failed.";
                        Debug.WriteLine("[Auth Result] Steam ID mismatch during verification");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.Status = AuthenticationStatus.NetworkError;
                    result.ErrorMessage = $"Failed to verify authentication with Steam: {ex.Message}";
                    Debug.WriteLine($"[Auth Result] Verification error: {ex.Message}");
                    return result;
                }

                // Fetch user profile from Steam API
                try
                {
                    var user = await _authService.FetchUserProfileAsync(steamId, apiKey);

                    // Save credentials securely
                    await _credentialsService.SaveUserCredentialsAsync(user.SteamId, user.Username);
                    await _credentialsService.SaveApiKeyAsync(apiKey);

                    result.IsSuccessful = true;
                    result.Status = AuthenticationStatus.Success;
                    result.User = user;
                    result.SteamId = user.SteamId;

                    Debug.WriteLine($"[Auth Result] Authentication successful for user: {user.Username} ({user.SteamId})");
                    return result;
                }
                catch (Exception ex)
                {
                    result.Status = AuthenticationStatus.NetworkError;
                    result.ErrorMessage = $"Failed to fetch user profile: {ex.Message}";
                    Debug.WriteLine($"[Auth Result] Profile fetch error: {ex.Message}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Status = AuthenticationStatus.UnknownError;
                result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                Debug.WriteLine($"[Auth Result] Unexpected error: {ex}");
                return result;
            }
        }

        /// <summary>
        /// Gets a user-friendly message for the authentication result.
        /// </summary>
        public string GetStatusMessage(AuthenticationResult result)
        {
            return result.Status switch
            {
                AuthenticationStatus.Success => $"Welcome, {result.User?.Username}!",
                AuthenticationStatus.Cancelled => "Authentication was cancelled.",
                AuthenticationStatus.InvalidCredentials => result.ErrorMessage,
                AuthenticationStatus.NetworkError => "Network error. Please check your connection and try again.",
                AuthenticationStatus.UnknownError => result.ErrorMessage,
                _ => "Authentication pending..."
            };
        }

        /// <summary>
        /// Gets the appropriate color for status messages.
        /// </summary>
        public Color GetStatusColor(AuthenticationResult result)
        {
            return result.Status switch
            {
                AuthenticationStatus.Success => Colors.Green,
                AuthenticationStatus.Cancelled => Colors.Orange,
                AuthenticationStatus.InvalidCredentials => Colors.Red,
                AuthenticationStatus.NetworkError => Colors.Red,
                AuthenticationStatus.UnknownError => Colors.Red,
                _ => Colors.Gray
            };
        }
    }
}