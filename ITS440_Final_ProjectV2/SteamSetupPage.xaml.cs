using ITS440_Final_ProjectV2.Services;
using ITS440_Final_ProjectV2.Models;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2;

public partial class SteamSetupPage : ContentPage
{
    private readonly GameDatabase _gameDatabase;
    private readonly SteamApiService _steamApiService;
    private readonly SecureCredentialsService _credentialsService;

    // Controls
    private Entry? _apiKeyEntry;
    private Entry? _steamIdEntry;
    private Button? _loadSteamGamesButton;
    private Label? _steamStatusLabel;

    public SteamSetupPage()
    {
        _gameDatabase = App.GameDatabase;
        _steamApiService = new SteamApiService();
        _credentialsService = new SecureCredentialsService();

        Title = "Steam Setup";
        InitializeUI();
        _ = LoadSavedCredentialsAsync();
    }

    private void InitializeUI()
    {
        _apiKeyEntry = new Entry
        {
            Placeholder = "Enter Steam API Key",
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black,
            IsPassword = true
        };

        _steamIdEntry = new Entry
        {
            Placeholder = "Enter your Steam ID",
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };

        _loadSteamGamesButton = new Button
        {
            Text = "Load Steam Games",
            Margin = new Thickness(15, 10),
            BackgroundColor = Colors.CadetBlue,
            TextColor = Colors.Black
        };
        _loadSteamGamesButton.Clicked += OnLoadSteamGamesClicked;

        _steamStatusLabel = new Label
        {
            Text = "Enter your credentials to load games from Steam.",
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black          
        };

        var content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(0, 20),
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "Steam Setup",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(15)
                    },
                    new Label
                    {
                        Text = "Find your Steam ID at: https://steamid.io",
                        FontSize = 12,
                        TextColor = Colors.Gray,
                        Margin = new Thickness(15, 0)
                    },
                    new Label
                    {
                        Text = "Get your API key at: https://steamcommunity.com/dev/apikey",
                        FontSize = 12,
                        TextColor = Colors.Gray,
                        Margin = new Thickness(15, 0, 15, 10)
                    },
                    new Label
                    {
                        Text = "IMPORTANT: Make sure your Steam library is PUBLIC at https://steamcommunity.com/settings/privacy",
                        FontSize = 11,
                        TextColor = Colors.Orange,
                        Margin = new Thickness(15)
                    },
                    new BoxView
                    {
                        HeightRequest = 1,
                        BackgroundColor = Colors.LightGray,
                        Margin = new Thickness(15, 5)
                    },
                    _apiKeyEntry,
                    _steamIdEntry,
                    _loadSteamGamesButton,
                    _steamStatusLabel
                }
            }
        };

        Content = content;
    }

    private async void OnLoadSteamGamesClicked(object? sender, EventArgs e)
    {
        string apiKey = _apiKeyEntry?.Text ?? string.Empty;
        string steamId = _steamIdEntry?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId))
        {
            if (_steamStatusLabel != null)
            {
                _steamStatusLabel.Text = "Please enter both API Key and Steam ID.";
                _steamStatusLabel.TextColor = Colors.Red;
            }
            return;
        }

        if (apiKey.Length < 32)
        {
            if (_steamStatusLabel != null)
            {
                _steamStatusLabel.Text = "API key appears invalid (too short).";
                _steamStatusLabel.TextColor = Colors.Red;
            }
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(steamId.Trim(), @"^\d+$"))
        {
            if (_steamStatusLabel != null)
            {
                _steamStatusLabel.Text = "Steam ID must contain only numbers.";
                _steamStatusLabel.TextColor = Colors.Red;
            }
            return;
        }

        if (_loadSteamGamesButton != null)
            _loadSteamGamesButton.IsEnabled = false;
        if (_steamStatusLabel != null)
        {
            _steamStatusLabel.Text = "Loading games...";
            _steamStatusLabel.TextColor = Colors.Gray;
        }

        try
        {
            bool isValid = await _steamApiService.ValidateApiKeyAsync(apiKey);
            if (!isValid)
            {
                if (_steamStatusLabel != null)
                {
                    _steamStatusLabel.Text = "Invalid API Key. Please check and try again.";
                    _steamStatusLabel.TextColor = Colors.Red;
                }
                return;
            }

            var steamGames = await _steamApiService.GetUserGamesAsync(steamId, apiKey);

            if (steamGames.Count == 0)
            {
                if (_steamStatusLabel != null)
                {
                    _steamStatusLabel.Text = "No games found. Ensure your Steam library is PUBLIC.";
                    _steamStatusLabel.TextColor = Colors.Orange;
                }
                return;
            }

            int addedCount = 0;
            foreach (var game in steamGames)
            {
                var dbGame = new Game
                {
                    Title = game.name,
                    Source = "Steam",
                    IsCompleted = false
                };

                try
                {
                    await _gameDatabase.AddGameAsync(dbGame);
                    addedCount++;
                }
                catch
                {
                    // Skip duplicates or invalid entries
                }
            }

            if (_steamStatusLabel != null)
            {
                _steamStatusLabel.Text = $"Successfully loaded {addedCount} games from Steam!";
                _steamStatusLabel.TextColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            if (_steamStatusLabel != null)
            {
                _steamStatusLabel.Text = $"Error: {ex.Message}";
                _steamStatusLabel.TextColor = Colors.Red;
            }
            Debug.WriteLine($"[Steam Load] Error: {ex}");
        }
        finally
        {
            if (_loadSteamGamesButton != null)
                _loadSteamGamesButton.IsEnabled = true;
        }
    }

    private async Task LoadSavedCredentialsAsync()
    {
        try
        {
            var savedApiKey = await _credentialsService.GetApiKeyAsync();
            var savedSteamId = await _credentialsService.GetSteamIdAsync();

            if (!string.IsNullOrEmpty(savedApiKey) && _apiKeyEntry != null)
                _apiKeyEntry.Text = savedApiKey;

            if (!string.IsNullOrEmpty(savedSteamId) && _steamIdEntry != null)
                _steamIdEntry.Text = savedSteamId;

            Debug.WriteLine("[SteamSetupPage] Credentials loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Load Credentials] Error: {ex.Message}");
        }
    }
}