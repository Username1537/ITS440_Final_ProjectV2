using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    /// <summary>
    /// Centralized service for handling application navigation.
    /// </summary>
    public class NavigationService
    {
        private static NavigationService _instance;
        private static readonly object _lock = new object();

        public static NavigationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new NavigationService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Navigates to the main page.
        /// </summary>
        public async Task NavigateToMainPageAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("MainPage");
                Debug.WriteLine("[Navigation] Navigated to MainPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation] Error navigating to MainPage: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Navigates to the authentication page.
        /// </summary>
        public async Task NavigateToAuthenticationPageAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("authentication");
                Debug.WriteLine("[Navigation] Navigated to Authentication Page");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation] Error navigating to Authentication: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the navigation stack and navigates to the main page.
        /// </summary>
        public async Task ResetNavigationAsync()
        {
            try
            {
                // Clear any modals that might be open
                if (Shell.Current.CurrentPage is ContentPage page && page.Navigation.ModalStack.Count > 0)
                {
                    await page.Navigation.PopToRootAsync();
                }

                // Navigate to main page
                await Shell.Current.GoToAsync("MainPage");
                Debug.WriteLine("[Navigation] Navigation reset to MainPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation] Error resetting navigation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        public async Task GoBackAsync()
        {
            try
            {
                if (Shell.Current?.CurrentPage?.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.CurrentPage.Navigation.PopAsync();
                    Debug.WriteLine("[Navigation] Navigated back");
                }
                else
                {
                    await NavigateToMainPageAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation] Error navigating back: {ex.Message}");
                throw;
            }
        }
    }
}