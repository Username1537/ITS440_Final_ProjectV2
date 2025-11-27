using System.Collections.Specialized;
using System.Web;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Handles parsing and processing of Steam OpenID authentication responses.
    /// </summary>
    public class AuthenticationResponseHandler
    {
        /// <summary>
        /// Parses the authentication response URL and extracts OpenID parameters.
        /// </summary>
        public static Dictionary<string, string> ParseAuthenticationResponse(Uri responseUri)
        {
            if (responseUri == null)
                throw new ArgumentNullException(nameof(responseUri));

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Parse query string from the response URI
                var queryString = HttpUtility.ParseQueryString(responseUri.Query);

                foreach (string key in queryString.Keys)
                {
                    var value = queryString[key];
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        parameters[key] = value;
                    }
                }

                Debug.WriteLine($"[Auth Response] Parsed {parameters.Count} parameters from response");

                return parameters;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth Response] Error parsing response: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extracts the Steam ID from the OpenID response.
        /// </summary>
        public static string ExtractSteamIdFromResponse(Dictionary<string, string> responseParameters)
        {
            if (responseParameters == null || responseParameters.Count == 0)
                throw new ArgumentException("Response parameters cannot be null or empty.");

            try
            {
                // Check for OpenID claimed_id parameter
                if (!responseParameters.ContainsKey("openid.claimed_id"))
                    throw new FormatException("Missing 'openid.claimed_id' in authentication response.");

                var claimedId = responseParameters["openid.claimed_id"];
                
                // Extract Steam ID from claimed_id (format: https://steamcommunity.com/openid/id/[STEAMID64])
                var steamIdMatch = System.Text.RegularExpressions.Regex.Match(
                    claimedId, 
                    @"(?:https?://)?(?:www\.)?steamcommunity\.com/openid/id/(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!steamIdMatch.Success)
                {
                    // Try alternative format
                    steamIdMatch = System.Text.RegularExpressions.Regex.Match(claimedId, @"(\d+)/?$");
                }

                if (!steamIdMatch.Success)
                    throw new FormatException($"Could not extract Steam ID from claimed_id: {claimedId}");

                string steamId = steamIdMatch.Groups[1].Value;

                if (string.IsNullOrEmpty(steamId) || !System.Text.RegularExpressions.Regex.IsMatch(steamId, @"^\d+$"))
                    throw new FormatException("Extracted Steam ID is invalid.");

                Debug.WriteLine($"[Auth Response] Successfully extracted Steam ID: {steamId}");

                return steamId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth Response] Error extracting Steam ID: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates the OpenID response structure.
        /// </summary>
        public static bool IsValidOpenIdResponse(Dictionary<string, string> responseParameters)
        {
            if (responseParameters == null || responseParameters.Count == 0)
                return false;

            try
            {
                // Check for required OpenID parameters
                var requiredParams = new[] { "openid.ns", "openid.mode", "openid.claimed_id" };

                foreach (var param in requiredParams)
                {
                    if (!responseParameters.ContainsKey(param))
                    {
                        Debug.WriteLine($"[Auth Response] Missing required parameter: {param}");
                        return false;
                    }
                }

                // Verify mode is id_res (successful response)
                var mode = responseParameters.GetValueOrDefault("openid.mode");
                if (mode != "id_res")
                {
                    Debug.WriteLine($"[Auth Response] Invalid mode: {mode}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth Response] Validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the response indicates an authentication cancellation.
        /// </summary>
        public static bool WasAuthenticationCancelled(Dictionary<string, string> responseParameters)
        {
            if (responseParameters == null)
                return false;

            var mode = responseParameters.GetValueOrDefault("openid.mode");
            return mode == "cancel" || mode == "error";
        }

        /// <summary>
        /// Gets the error message from the response if present.
        /// </summary>
        public static string GetErrorMessage(Dictionary<string, string> responseParameters)
        {
            if (responseParameters == null)
                return "Unknown error occurred.";

            if (responseParameters.TryGetValue("openid.error", out var error))
                return $"Steam authentication error: {error}";

            if (responseParameters.TryGetValue("error", out var genericError))
                return $"Authentication error: {genericError}";

            return "Authentication failed. Please try again.";
        }
    }
}