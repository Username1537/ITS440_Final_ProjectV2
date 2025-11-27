using ITS440_Final_ProjectV2.Services;
using ITS440_Final_ProjectV2.Models;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2;

public partial class GameListPage : ContentPage
{
    private readonly GameDatabase _gameDatabase;
    private List<Game> _allGames;

    private CollectionView? _gameCollectionView;
    private Picker? _filterPicker;

    public GameListPage()
    {
        //InitializeComponent();

        _gameDatabase = App.GameDatabase;
        _allGames = new List<Game>();

        Title = "Game List";
        InitializeUI();
        _ = LoadGamesAsync();
    }

    private void InitializeUI()
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

        Content = new ScrollView { Content = content };
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
}