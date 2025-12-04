using System.Diagnostics;
using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;

namespace ITS440_Final_ProjectV2
{
    //api key: 64B7C74FE6D4EDAC2575FE05F3237CFF
    //domain name: my-app
    public partial class MainPage1 : TabbedPage
    {
        private readonly GameDatabase _gameDatabase;
        private readonly SteamApiService _steamApiService;
        private readonly SecureCredentialsService _credentialsService;

        // Steam Setup Tab Controls
        private Entry? _apiKeyEntry;
        private Entry? _steamIdEntry;
        private Button? _loadSteamGamesButton;
        private Button? _navigateToAuthButton;
        private Label? _steamStatusLabel;

        // Add Custom Game Tab Controls
        private Entry? _customGameTitleEntry;
        private Button? _addCustomGameButton; //hello
        private Label? _addGameStatusLabel;

        // Game List Tab Controls
        private CollectionView? _gameCollectionView;
        private Picker? _filterPicker;
        private List<Game> _allGames;

        public MainPage1()
        {
            _gameDatabase = new GameDatabase();
            _steamApiService = new SteamApiService();
            _credentialsService = new SecureCredentialsService();
            _allGames = new List<Game>();

            Title = "Game Completion Tracker";

            InitializeUI();
            _ = LoadGamesAsync(); // Fire-and-forget, explicitly discard the returned Task
            _ = LoadSavedCredentialsAsync(); // Fire-and-forget, explicitly discard the returned Task
        }

        private void InitializeUI()
        {
            // Create tab pages
            Children.Add(CreateSteamSetupTab());
            Children.Add(CreateAddCustomGameTab());
            Children.Add(CreateGameListTab());
        }

        #region Steam Setup Tab

        private ContentPage CreateSteamSetupTab()
        {
            _navigateToAuthButton = new Button
            {
                Text = "Go to Steam Authentication",
                Margin = new Thickness(15, 10),
                BackgroundColor = Colors.DodgerBlue
            };
            _navigateToAuthButton.Clicked += OnNavigateToAuthenticationClicked;

            _apiKeyEntry = new Entry
            {
                Placeholder = "Enter Steam API Key (or use Authentication tab)",
                Margin = new Thickness(15),
                IsPassword = true
            };

            _steamIdEntry = new Entry
            {
                Placeholder = "Enter your Steam ID (or use Authentication tab)",
                Margin = new Thickness(15)
            };

            _loadSteamGamesButton = new Button
            {
                Text = "Load Steam Games",
                Margin = new Thickness(15, 10),
                BackgroundColor = Colors.CornflowerBlue
            };
            _loadSteamGamesButton.Clicked += OnLoadSteamGamesClicked;

            _steamStatusLabel = new Label
            {
                Text = "Use the Steam Authentication tab first, or enter credentials manually.",
                Margin = new Thickness(15),
                TextColor = Colors.Gray
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
                        _navigateToAuthButton,
                        new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = Colors.LightGray,
                            Margin = new Thickness(15, 5)
                        },
                        new Label
                        {
                            Text = "Or enter manually:",
                            FontSize = 12,
                            FontAttributes = FontAttributes.Bold,
                            Margin = new Thickness(15, 10, 15, 5)
                        },
                        _apiKeyEntry,
                        _steamIdEntry,
                        _loadSteamGamesButton,
                        _steamStatusLabel
                    }
                }
            };

            return new ContentPage
            {
                Title = "Steam Setup",
                Content = content
            };
        }

        private async void OnNavigateToAuthenticationClicked(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("authentication");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", $"Could not navigate to authentication: {ex.Message}", "OK");
                Debug.WriteLine($"[Navigation] Error: {ex}");
            }
        }

        private async void OnLoadSteamGamesClicked(object? sender, EventArgs e)
        {
            string apiKey = _apiKeyEntry?.Text ?? string.Empty;
            string steamId = _steamIdEntry?.Text ?? string.Empty;

            // Validation
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
                // Validate API key
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

                // Fetch games
                var steamGames = await _steamApiService.GetUserGamesAsync(steamId, apiKey);

                if (steamGames.Count == 0)
                {
                    if (_steamStatusLabel != null)
                    {
                        _steamStatusLabel.Text = "No games found. Make sure your Steam library is PUBLIC at: https://steamcommunity.com/settings/privacy";
                        _steamStatusLabel.TextColor = Colors.Orange;
                    }
                    return;
                }

                // Add games to database
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

                await LoadGamesAsync();
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

        #endregion

        #region Add Custom Game Tab

        private ContentPage CreateAddCustomGameTab()
        {
            _customGameTitleEntry = new Entry
            {
                Placeholder = "Enter game title",
                Margin = new Thickness(15)
            };

            _addCustomGameButton = new Button
            {
                Text = "Add Custom Game",
                Margin = new Thickness(15, 10),
                BackgroundColor = Colors.SeaGreen
            };
            _addCustomGameButton.Clicked += OnAddCustomGameClicked;

            _addGameStatusLabel = new Label
            {
                Text = "Add games not in your Steam library.",
                Margin = new Thickness(15),
                TextColor = Colors.Gray
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
                            Text = "Add Custom Game",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(15)
                        },
                        _customGameTitleEntry,
                        _addCustomGameButton,
                        _addGameStatusLabel
                    }
                }
            };

            return new ContentPage
            {
                Title = "Add Custom",
                Content = content
            };
        }

        private async void OnAddCustomGameClicked(object? sender, EventArgs e)
        {
            string title = _customGameTitleEntry?.Text ?? string.Empty;

            // Validation
            if (string.IsNullOrWhiteSpace(title))
            {
                if (_addGameStatusLabel != null)
                {
                    _addGameStatusLabel.Text = "Game title cannot be empty.";
                    _addGameStatusLabel.TextColor = Colors.Red;
                }
                return;
            }

            if (title.Length > 255)
            {
                if (_addGameStatusLabel != null)
                {
                    _addGameStatusLabel.Text = "Game title must be 255 characters or less.";
                    _addGameStatusLabel.TextColor = Colors.Red;
                }
                return;
            }

            if (_addCustomGameButton != null)
                _addCustomGameButton.IsEnabled = false;

            try
            {
                var game = new Game
                {
                    Title = title.Trim(),
                    Source = "Custom",
                    IsCompleted = false
                };

                await _gameDatabase.AddGameAsync(game);

                if (_addGameStatusLabel != null)
                {
                    _addGameStatusLabel.Text = "Game added successfully!";
                    _addGameStatusLabel.TextColor = Colors.Green;
                }
                if (_customGameTitleEntry != null)
                    _customGameTitleEntry.Text = string.Empty;

                await LoadGamesAsync();
            }
            catch (Exception ex)
            {
                if (_addGameStatusLabel != null)
                {
                    _addGameStatusLabel.Text = $"Error adding game: {ex.Message}";
                    _addGameStatusLabel.TextColor = Colors.Red;
                }
                Debug.WriteLine($"[Add Custom Game] Error: {ex}");
            }
            finally
            {
                if (_addCustomGameButton != null)
                    _addCustomGameButton.IsEnabled = true;
            }
        }

        #endregion

        #region Game List Tab

        private ContentPage CreateGameListTab()
        {
            _filterPicker = new Picker
            {
                Title = "Filter Games",
                ItemsSource = new List<string> { "All Games", "Not Started", "Completed" },
                SelectedIndex = 0,
                Margin = new Thickness(15)
            };
            _filterPicker.SelectedIndexChanged += OnFilterChanged;

            _gameCollectionView = new CollectionView
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Single,
                ItemTemplate = new DataTemplate(CreateGameItemTemplate),
                EmptyView = new Label
                {
                    Text = "No games found. Add games from Steam or create custom games.",
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(20)
                }
            };

            var refreshButton = new Button
            {
                Text = "Refresh",
                Margin = new Thickness(15, 10),
                BackgroundColor = Colors.DodgerBlue
            };
            refreshButton.Clicked += async (s, e) => await LoadGamesAsync();

            var statsLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Gray,
                Margin = new Thickness(15, 5),
                HorizontalTextAlignment = TextAlignment.Center
            };

            var content = new VerticalStackLayout
            {
                Padding = new Thickness(0, 10),
                Spacing = 5,
                Children =
                {
                    new Label
                    {
                        Text = "Game Completion List",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(15)
                    },
                    _filterPicker,
                    _gameCollectionView,
                    statsLabel,
                    refreshButton
                }
            };

            return new ContentPage
            {
                Title = "Game List",
                Content = new ScrollView { Content = content }
            };
        }

        private Border CreateGameItemTemplate()
        {
            var titleLabel = new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 16
            };
            titleLabel.SetBinding(Label.TextProperty, "Title");

            var sourceLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Gray
            };
            sourceLabel.SetBinding(Label.TextProperty, "Source");

            var checkbox = new CheckBox
            {
                VerticalOptions = LayoutOptions.Center
            };
            checkbox.SetBinding(CheckBox.IsCheckedProperty, "IsCompleted");
            checkbox.CheckedChanged += OnGameCompletionToggled;

            var deleteButton = new Button
            {
                Text = "Delete",
                FontSize = 12,
                Padding = new Thickness(10, 5),
                BackgroundColor = Colors.Tomato,
                CornerRadius = 5,
                WidthRequest = 80
            };
            deleteButton.Clicked += OnDeleteGameClicked;

            var gameBorder = new Border
            {
                Padding = new Thickness(15),
                //BorderColor = Colors.LightGray,
                //HasShadow = true,
                Margin = new Thickness(10, 5),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new HorizontalStackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                new VerticalStackLayout
                                {
                                    HorizontalOptions = LayoutOptions.Center,
                                    Spacing = 3,
                                    Children = { titleLabel, sourceLabel }
                                },
                                new VerticalStackLayout
                                {
                                    VerticalOptions = LayoutOptions.Center,
                                    Children = { checkbox }
                                }
                            }
                        },
                        new HorizontalStackLayout
                        {
                            Spacing = 10,
                            HorizontalOptions = LayoutOptions.End,
                            Children = { deleteButton }
                        }
                    }
                }
            };

            return gameBorder;
        }

        private async void OnGameCompletionToggled(object? sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.BindingContext is Game game)
            {
                game.IsCompleted = e.Value;
                try
                {
                    await _gameDatabase.UpdateGameAsync(game);
                    Debug.WriteLine($"[Game] Toggled completion status for: {game.Title}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Update Game] Error: {ex.Message}");
                }
            }
        }

        private async void OnDeleteGameClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Game game)
            {
                bool confirm = await DisplayAlert("Confirm Delete",
                    $"Delete '{game.Title}'?", "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _gameDatabase.DeleteGameAsync(game.Id);
                        await LoadGamesAsync();
                        Debug.WriteLine($"[Game] Deleted: {game.Title}");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete game: {ex.Message}", "OK");
                        Debug.WriteLine($"[Delete Game] Error: {ex.Message}");
                    }
                }
            }
        }

        private async void OnFilterChanged(object? sender, EventArgs e)
        {
            await ApplyFilter();
        }

        private async Task ApplyFilter()
        {
            if (_filterPicker != null && _gameCollectionView != null)
            {
                if (_filterPicker.SelectedIndex == 1)
                {
                    _gameCollectionView.ItemsSource = _allGames.Where(g => !g.IsCompleted).ToList();
                }
                else if (_filterPicker.SelectedIndex == 2)
                {
                    _gameCollectionView.ItemsSource = _allGames.Where(g => g.IsCompleted).ToList();
                }
                else
                {
                    _gameCollectionView.ItemsSource = _allGames;
                }
            }
            await Task.CompletedTask;
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                _allGames = await _gameDatabase.GetAllGamesAsync();
                await ApplyFilter();
                Debug.WriteLine($"[Load Games] Loaded {_allGames.Count} games from database");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Load Games] Error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load games: {ex.Message}", "OK");
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

                Debug.WriteLine("[MainPage] Credentials loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Load Credentials] Error: {ex.Message}");
            }
        }
        #endregion
    }
}