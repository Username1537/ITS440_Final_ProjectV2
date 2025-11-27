namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Provides user-facing dialogs and confirmations for logout operations.
    /// </summary>
    public class LogoutDialogService
    {
        private readonly Page _page;

        public LogoutDialogService(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        /// <summary>
        /// Shows a confirmation dialog before logout.
        /// </summary>
        public async Task<bool> ConfirmLogoutAsync()
        {
            return await _page.DisplayAlert(
                "Confirm Logout",
                "Are you sure you want to logout? Your Steam credentials will be cleared.",
                "Yes, Logout",
                "Cancel");
        }

        /// <summary>
        /// Shows a confirmation dialog for clearing game data during logout.
        /// </summary>
        public async Task<bool> ConfirmClearGameDataAsync()
        {
            return await _page.DisplayAlert(
                "Clear Game Data",
                "Would you like to clear all game data (custom and Steam games) before logging out?",
                "Yes, Clear All",
                "Keep Data");
        }

        /// <summary>
        /// Shows a success message after logout.
        /// </summary>
        public async Task ShowLogoutSuccessAsync()
        {
            await _page.DisplayAlert(
                "Logged Out",
                "You have been successfully logged out.",
                "OK");
        }

        /// <summary>
        /// Shows an error message if logout failed.
        /// </summary>
        public async Task ShowLogoutErrorAsync(LogoutResult result)
        {
            var message = result.ErrorMessage ?? "An unexpected error occurred during logout.";

            if (result.ErrorDetails.Count > 0)
            {
                message += "\n\nDetails:\n";
                foreach (var error in result.ErrorDetails)
                {
                    message += $"• {error.Key}: {error.Value}\n";
                }
            }

            await _page.DisplayAlert("Logout Error", message, "OK");
        }

        /// <summary>
        /// Shows a message indicating logout is in progress.
        /// </summary>
        public async Task ShowLogoutInProgressAsync(string message = "Logging out...")
        {
            await _page.DisplayAlert("Processing", message, "OK");
        }
    }
}