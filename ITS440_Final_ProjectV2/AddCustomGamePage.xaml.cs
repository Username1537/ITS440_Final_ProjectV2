using ITS440_Final_ProjectV2.Services;
using ITS440_Final_ProjectV2.Models;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2;

public partial class AddCustomGamePage : ContentPage
{
    private readonly GameDatabase _gameDatabase;

    private Entry? _customGameTitleEntry;
    private Button? _addCustomGameButton;
    private Label? _addGameStatusLabel;

    public AddCustomGamePage()
    {
        //InitializeComponent();

        _gameDatabase = App.GameDatabase;

        Title = "Add Custom";
        InitializeUI();
    }

    private void InitializeUI()
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

        Content = content;
    }

    private async void OnAddCustomGameClicked(object? sender, EventArgs e)
    {
        string title = _customGameTitleEntry?.Text ?? string.Empty;

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

            Debug.WriteLine($"[Add Custom Game] Added: {game.Title}");
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
}