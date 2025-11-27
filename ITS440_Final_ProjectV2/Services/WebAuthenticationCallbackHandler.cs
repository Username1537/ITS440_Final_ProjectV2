using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Handles the callback URL from Steam authentication in the web browser.
    /// Manages the deep linking and response processing.
    /// </summary>
    public class WebAuthenticationCallbackHandler
    {
        private TaskCompletionSource<Dictionary<string, string>> _authenticationCompletionSource;

        /// <summary>
        /// Initiates the web authentication flow with a completion handler.
        /// </summary>
        public Task<Dictionary<string, string>> AuthenticateWithBrowserAsync(
            string authenticationUrl,
            string callbackScheme = "steamauth")
        {
            _authenticationCompletionSource = new TaskCompletionSource<Dictionary<string, string>>();

            // Open the browser for authentication
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Browser.OpenAsync(authenticationUrl, BrowserLaunchMode.SystemPreferred);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Web Auth] Browser launch error: {ex.Message}");
                    _authenticationCompletionSource?.TrySetException(ex);
                }
            });

            // Set a timeout to cancel the authentication if no response is received
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
            Task.WhenAny(_authenticationCompletionSource.Task, timeoutTask).ContinueWith(async task =>
            {
                if (task.Result == timeoutTask)
                {
                    Debug.WriteLine("[Web Auth] Authentication timeout");
                    _authenticationCompletionSource?.TrySetResult(new Dictionary<string, string>
                    {
                        { "openid.mode", "error" },
                        { "openid.error", "timeout" }
                    });
                }
            });

            return _authenticationCompletionSource.Task;
        }

        /// <summary>
        /// Processes the callback URL from the authentication browser session.
        /// </summary>
        public void HandleCallback(Uri callbackUri)
        {
            try
            {
                if (callbackUri == null)
                {
                    Debug.WriteLine("[Web Auth] Callback URI is null");
                    _authenticationCompletionSource?.TrySetResult(new Dictionary<string, string>
                    {
                        { "openid.mode", "error" }
                    });
                    return;
                }

                Debug.WriteLine($"[Web Auth] Received callback: {callbackUri}");

                // Parse the authentication response
                var responseParameters = AuthenticationResponseHandler.ParseAuthenticationResponse(callbackUri);

                // Complete the authentication task
                _authenticationCompletionSource?.TrySetResult(responseParameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Web Auth] Callback handling error: {ex.Message}");
                _authenticationCompletionSource?.TrySetException(ex);
            }
        }

        /// <summary>
        /// Cancels the pending authentication.
        /// </summary>
        public void CancelAuthentication()
        {
            Debug.WriteLine("[Web Auth] Authentication cancelled");
            _authenticationCompletionSource?.TrySetResult(new Dictionary<string, string>
            {
                { "openid.mode", "cancel" }
            });
        }
    }
}