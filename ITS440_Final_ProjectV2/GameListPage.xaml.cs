using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2;

public partial class GameListPage : ContentPage
{
    private readonly Services.GameDatabase _gameDatabase;
    private List<Game> _allGames;

    private CollectionView? _gameCollectionView;
    private Picker? _filterPicker;

    public GameListPage()
    {

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
            Title = "Filter / Sort Games",
            ItemsSource = new List<string>
            {
                "All Games",
                "Not Started",
                "Completed",
                "Order by Importance",
                "Order by Completion"
            },
            SelectedIndex = 0,
            Margin = new Thickness(15)
,           BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };
        _filterPicker.SelectedIndexChanged += OnFilterChanged;

        _gameCollectionView = new CollectionView
        {
            Margin = new Thickness(10),
            BackgroundColor = Colors.White,
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

        var searchBar = new SearchBar
        {
            Placeholder = "Search games...",
            Margin = new Thickness(15, 5),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };
        searchBar.TextChanged += OnSearchBarTextChanged;

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
                searchBar,
                _filterPicker,
                _gameCollectionView,
                statsLabel,
                refreshButton,
                

            }
        };

        Content = new ScrollView { Content = content };
    }

    private Border CreateGameItemTemplate()
    {
        var border = new Border
        {
            Padding = new Thickness(15),
            Margin = new Thickness(10, 5),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Colors.LightBlue.WithAlpha(4)
        };

        // Image
        var gameImage = new Image
        {
            WidthRequest = 100,
            HeightRequest = 80,
            Aspect = Aspect.AspectFill,
            BackgroundColor = Colors.LightGray
        };
        gameImage.SetBinding(Image.SourceProperty, "ImagePath");

        // Labels
        var titleLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        titleLabel.SetBinding(Label.TextProperty, "Title");

        var sourceLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        sourceLabel.SetBinding(Label.TextProperty, "Source");

        var notesLabel = new Label { FontSize = 12, TextColor = Colors.Green };
        notesLabel.SetBinding(Label.TextProperty, "Notes");

        var priorityLabel = new Label { FontSize = 12, TextColor = Colors.Purple };
        priorityLabel.SetBinding(Label.TextProperty, new Binding("Priority", stringFormat: "Priority: {0}"));

        var ratingLabel = new Label { FontSize = 12, TextColor = Colors.Goldenrod };
        ratingLabel.SetBinding(Label.TextProperty, new Binding("Rating", stringFormat: "Rating: {0}/5"));

        // Buttons
        var deleteButton = new Button
        {
            Text = "Delete",
            BackgroundColor = Colors.IndianRed,
            TextColor = Colors.White,
            Padding = new Thickness(10, 5),
            CornerRadius = 5,
            WidthRequest = 80
        };
        deleteButton.SetBinding(Button.BindingContextProperty, ".");
        deleteButton.Clicked += OnDeleteGameClicked;

        var completeButton = new Button
        {
            Text = "Mark Completed",
            BackgroundColor = Colors.LimeGreen,
            TextColor = Colors.White,
            Padding = new Thickness(10, 5),
            CornerRadius = 5
        };
        completeButton.SetBinding(Button.BindingContextProperty, ".");
        completeButton.Clicked += async (s, e) =>
        {
            if (completeButton.BindingContext is Game game && !game.IsCompleted)
            {
                game.IsCompleted = true;
                game.Notes = $"Completed on {DateTime.Now:MM/dd/yyyy}";
                await _gameDatabase.UpdateGameAsync(game);
                await LoadGamesAsync();
            }
        };

        var editButton = new Button
        {
            Text = "Edit",
            BackgroundColor = Colors.CadetBlue,
            TextColor = Colors.White,
            Padding = new Thickness(10, 5),
            CornerRadius = 5,
            WidthRequest = 80
        };
        editButton.SetBinding(Button.BindingContextProperty, ".");
        editButton.Clicked += async (s, e) =>
        {
            if (editButton.BindingContext is Game game)
            {
                await Navigation.PushAsync(new GameDetailPage(game, _gameDatabase));
            }
        };

        var buttonsRow = new HorizontalStackLayout
        {
            Spacing = 10,
            Children = { completeButton, deleteButton, editButton }
        };

        // Layout
        var mainLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
        {
            gameImage,
            new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    titleLabel,
                    sourceLabel,
                    notesLabel,
                    ratingLabel,
                    priorityLabel,
                    buttonsRow
                }
            }
        }
        };

        border.Content = mainLayout;
        return border;
    }


    private async void OnDeleteGameClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Game game)
        {
            bool confirm = await DisplayAlert("Confirm Delete",
                $"Delete '{game.Title}'?", "Yes", "No");

            if (confirm)
            {
                await _gameDatabase.DeleteGameAsync(game.Id);
                await LoadGamesAsync();
                Debug.WriteLine($"[Game] Deleted: {game.Title}");
            }
        }
    }



    private async void OnFilterChanged(object? sender, EventArgs e)
    {
        await ApplyFilter();
    }

    private async Task ApplyFilter()
    {
        if (_filterPicker == null || _gameCollectionView == null) return;

        IEnumerable<Game> filtered = _allGames;

        switch (_filterPicker.SelectedIndex)
        {
            case 1: // Not Started
                filtered = _allGames.Where(g => !g.IsCompleted);
                break;
            case 2: // Completed
                filtered = _allGames.Where(g => g.IsCompleted);
                break;
            case 3: // Order by Urgency (Priority ascending)
                filtered = _allGames.OrderBy(g => g.Priority);
                break;
            case 4: // Order by Completion (Completed first)
                filtered = _allGames.OrderByDescending(g => g.IsCompleted);
                break;
            default: // All Games
                filtered = _allGames;
                break;
        }

        _gameCollectionView.ItemsSource = filtered.ToList();
        await Task.CompletedTask;
    }

    private async void OnSearchBarTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_gameCollectionView == null) return;

        string query = e.NewTextValue?.ToLower() ?? string.Empty;

        var filtered = _allGames.Where(g =>
            string.IsNullOrWhiteSpace(query) ||
            g.Title.ToLower().Contains(query));

        // Apply filter picker on top of search
        switch (_filterPicker?.SelectedIndex)
        {
            case 1: // Not Started
                filtered = filtered.Where(g => !g.IsCompleted);
                break;
            case 2: // Completed
                filtered = filtered.Where(g => g.IsCompleted);
                break;
            case 3: // Order by Priority
                filtered = filtered.OrderBy(g => g.Priority);
                break;
            case 4: // Order by Completion
                filtered = filtered.OrderByDescending(g => g.IsCompleted);
                break;
        }

        _gameCollectionView.ItemsSource = filtered.ToList();
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
