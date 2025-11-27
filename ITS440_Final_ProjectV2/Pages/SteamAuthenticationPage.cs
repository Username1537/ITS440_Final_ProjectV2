using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Pages;
public partial class SteamAuthenticationPage : ContentPage
{
    private readonly SteamAuthenticationService _authService;
    private readonly SecureCredentialsService _credentialsService;
    private readonly SteamApiService _steamApiService;
    private readonly AuthenticationResultHandler _resultHandler;
    private readonly WebAuthenticationCallbackHandler _callbackHandler;
    private readonly LogoutService _logoutService;
    private readonly NavigationService _navigationService;
    private LogoutDialogService _logoutDialogService;

    private Entry _apiKeyEntry;
    private Button _loginButton;
    private Button _logoutButton;
    private Label _statusLabel;
    private Label _userInfoLabel;
    private VerticalStackLayout _authContainer;
    private VerticalStackLayout _loggedInContainer;
    private ActivityIndicator _loadingIndicator;

    public SteamAuthenticationPage()
    {
        _authService = new SteamAuthenticationService();
        _credentialsService = new SecureCredentialsService();
        _steamApiService = new SteamApiService();
        _resultHandler = new AuthenticationResultHandler(_authService, _credentialsService, _steamApiService);
        _callbackHandler = new WebAuthenticationCallbackHandler();
        _logoutService = new LogoutService(_authService, _credentialsService, App.GameDatabase);
        _navigationService = NavigationService.Instance;

        Title = "Steam Authentication";
        InitializeUI();
        CheckExistingAuthenticationAsync();
    }

    private void InitializeUI()
    {
        // Initialize dialog service
        _logoutDialogService = new LogoutDialogService(this);

        // Loading indicator
        _loadingIndicator = new ActivityIndicator
        {
            IsRunning = false,
            IsVisible = false,
            Margin = new Thickness(15)
        };

        // Authentication UI
        _apiKeyEntry = new Entry
        {
            Placeholder = "Enter Steam Web API Key",
            Margin = new Thickness(15),
            IsPassword = true
        };

        _loginButton = new Button
        {
            Text = "Login with Steam",
            Margin = new Thickness(15, 10),
            BackgroundColor = Colors.CornflowerBlue
        };
        _loginButton.Clicked += OnLoginClicked;

        _statusLabel = new Label
        {
            Text = "Enter your Steam API key and click Login.",
            Margin = new Thickness(15),
            TextColor = Colors.Gray
        };

        _authContainer = new VerticalStackLayout
        {
            Padding = new Thickness(0, 20),
            Spacing = 10,
            Children =
                {
                    new Label
                    {
                        Text = "Steam Login",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(15)
                    },
                    new Label
                    {
                        Text = "Get your API key from: https://steamcommunity.com/dev/apikey",
                        FontSize = 12,
                        TextColor = Colors.Gray,
                        Margin = new Thickness(15, 0)
                    },
                    _apiKeyEntry,
                    _loginButton,
                    _loadingIndicator,
                    _statusLabel
                }
        };

        // Logged-in UI
        _userInfoLabel = new Label
        {
            FontSize = 14,
            Margin = new Thickness(15)
        };

        _logoutButton = new Button
        {
            Text = "Logout",
            Margin = new Thickness(15, 10),
            BackgroundColor = Colors.Tomato
        };
        _logoutButton.Clicked += OnLogoutClicked;

        _loggedInContainer = new VerticalStackLayout
        {
            Padding = new Thickness(0, 20),
            Spacing = 10,
            IsVisible = false,
            Children =
                {
                    new Label
                    {
                        Text = "Logged In",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(15)
                    },
                    _userInfoLabel,
                    _logoutButton
                }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(0, 10),
                Spacing = 10,
                Children =
                    {
                        _authContainer,
                        _loggedInContainer
                    }
            }
        };
    }

    private async void CheckExistingAuthenticationAsync()
    {
        try
        {
            var apiKey = await _credentialsService.GetApiKeyAsync();
            var steamId = await _credentialsService.GetSteamIdAsync();
            var username = await _credentialsService.GetUsernameAsync();

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(steamId))
            {
                await UpdateAuthenticationUI(steamId, username);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth Check] Error: {ex.Message}");
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        string apiKey = _apiKeyEntry.Text;

        // Validation
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _statusLabel.Text = "API key cannot be empty.";
            _statusLabel.TextColor = Colors.Red;
            return;
        }

        if (apiKey.Length < 32)
        {
            _statusLabel.Text = "API key appears to be invalid (too short).";
            _statusLabel.TextColor = Colors.Red;
            return;
        }

        _loginButton.IsEnabled = false;
        _loadingIndicator.IsRunning = true;
        _loadingIndicator.IsVisible = true;
        _statusLabel.Text = "Validating API key...";
        _statusLabel.TextColor = Colors.Gray;

        try
        {
            // Validate the API key
            bool isValid = await _steamApiService.ValidateApiKeyAsync(apiKey);
            if (!isValid)
            {
                _statusLabel.Text = "Invalid API key. Please check and try again.";
                _statusLabel.TextColor = Colors.Red;
                return;
            }

            _statusLabel.Text = "Opening Steam login in browser...";
            _statusLabel.TextColor = Colors.Orange;

            // Use a custom URI scheme for the return URL
            string returnUrl = "my-app://steam-auth/return";
            string authUrl = _authService.GetAuthenticationUrl(returnUrl);

            // Initiate web-based authentication
            var responseParameters = await _callbackHandler.AuthenticateWithBrowserAsync(authUrl);

            // Process the authentication response
            var result = await _resultHandler.ProcessAuthenticationResponseAsync(responseParameters, apiKey);

            // Update UI based on result
            await HandleAuthenticationResult(result);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _statusLabel.TextColor = Colors.Red;
            Debug.WriteLine($"[Login] Error: {ex}");
        }
        finally
        {
            _loginButton.IsEnabled = true;
            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }
    }

    private async Task HandleAuthenticationResult(AuthenticationResult result)
    {
        _statusLabel.Text = _resultHandler.GetStatusMessage(result);
        _statusLabel.TextColor = _resultHandler.GetStatusColor(result);

        if (result.IsSuccessful && result.User != null)
        {
            await UpdateAuthenticationUI(result.User.SteamId, result.User.Username);
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        // Show logout confirmation
        bool confirm = await _logoutDialogService.ConfirmLogoutAsync();
        if (!confirm)
            return;

        // Optionally ask about clearing game data
        bool clearGameData = await _logoutDialogService.ConfirmClearGameDataAsync();

        // Disable button and show loading
        _logoutButton.IsEnabled = false;
        _loadingIndicator.IsRunning = true;
        _loadingIndicator.IsVisible = true;

        try
        {
            // Execute logout
            var logoutResult = await _logoutService.LogoutAsync(clearGameData);

            if (logoutResult.IsSuccessful)
            {
                // Validate logout completion
                bool isLoggedOut = await _logoutService.ValidateLogoutAsync();
                if (isLoggedOut)
                {
                    await _logoutDialogService.ShowLogoutSuccessAsync();

                    // Reset UI
                    ResetUI();

                    // Navigate back to main page
                    await _navigationService.ResetNavigationAsync();
                }
                else
                {
                    await _logoutDialogService.ShowLogoutErrorAsync(
                        new LogoutResult
                        {
                            ErrorMessage = "Logout validation failed. Credentials were not fully cleared."
                        });
                }
            }
            else
            {
                await _logoutDialogService.ShowLogoutErrorAsync(logoutResult);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logout] Unexpected error: {ex}");
            await _logoutDialogService.ShowLogoutErrorAsync(
                new LogoutResult
                {
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                });
        }
        finally
        {
            _logoutButton.IsEnabled = true;
            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }
    }

    private void ResetUI()
    {
        _apiKeyEntry.Text = string.Empty;
        _authContainer.IsVisible = true;
        _loggedInContainer.IsVisible = false;
        _statusLabel.Text = "Enter your Steam API key and click Login.";
        _statusLabel.TextColor = Colors.Gray;
        _userInfoLabel.Text = string.Empty;
    }

    private async Task UpdateAuthenticationUI(string steamId, string username)
    {
        _userInfoLabel.Text = $"Welcome, {username}!\nSteam ID: {steamId}";
        _authContainer.IsVisible = false;
        _loggedInContainer.IsVisible = true;
        _statusLabel.Text = "Successfully authenticated with Steam!";
        _statusLabel.TextColor = Colors.Green;

        await Task.CompletedTask;
    }

    private async void OnNavigateToAuthenticationClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("///SteamSetup");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Could not navigate to setup: {ex.Message}", "OK");
            Debug.WriteLine($"[Navigation] Error: {ex}");
        }
    }
}