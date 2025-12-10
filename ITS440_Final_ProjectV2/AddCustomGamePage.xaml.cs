using System.Diagnostics;
using ITS440_Final_ProjectV2.Services;
using ITS440_Final_ProjectV2.Models;

namespace ITS440_Final_ProjectV2;

public partial class AddCustomGamePage : ContentPage
{
    private readonly GameDatabase _gameDatabase;

    private Entry _titleEntry;
    private Editor _notesEditor;
    private Entry _priorityEntry;
    private Button _addGameButton;
    private Label _statusLabel;
    private Button _pickImageButton;
    private string _selectedImagePath;

    public AddCustomGamePage()
    {
        _gameDatabase = App.GameDatabase;

        Title = "Add Custom Game";
        InitializeUI();
    }

    private void InitializeUI()
    {
        _titleEntry = new Entry
        {
            Placeholder = "Enter game title",
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };

        _notesEditor = new Editor
        {
            Placeholder = "Enter game notes/details",
            HeightRequest = 100,
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };

        _priorityEntry = new Entry
        {
            Placeholder = "Priority (lower = more important)",
            Keyboard = Keyboard.Numeric,
            Margin = new Thickness(15),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black
        };

      

       

        // Image picker
        _pickImageButton = new Button
        {
            Text = "Pick Image (optional)",
            BackgroundColor = Colors.LightBlue,
            TextColor = Colors.White,
            Margin = new Thickness(15, 5)
        };
        _pickImageButton.Clicked += async (s, e) =>
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Select a game image"
                });

                if (result != null)
                {
                    _selectedImagePath = result.FullPath;
                    _pickImageButton.Text = "Image Selected ✅";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not pick image: {ex.Message}", "OK");
            }
        };

        // Add game button
        _addGameButton = new Button
        {
            Text = "Add Custom Game",
            BackgroundColor = Colors.SeaGreen,
            TextColor = Colors.White,
            Margin = new Thickness(15)
        };
        _addGameButton.Clicked += OnAddGameClicked;

        // Status label
        _statusLabel = new Label
        {
            Text = "",
            Margin = new Thickness(15),
            TextColor = Colors.Gray
        };

        // Layout
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(0, 20),
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
                    _titleEntry,
                    _notesEditor,
                    _priorityEntry,
                    _pickImageButton,
                    _addGameButton,
                    _statusLabel
                }
            }
        };
    }

    private async void OnAddGameClicked(object? sender, EventArgs e)
    {
        string title = _titleEntry.Text?.Trim() ?? string.Empty;
        string notes = _notesEditor.Text?.Trim() ?? string.Empty;
        int priority = 0;

        if (string.IsNullOrWhiteSpace(title))
        {
            _statusLabel.Text = "Title cannot be empty.";
            _statusLabel.TextColor = Colors.Red;
            return;
        }

        if (!int.TryParse(_priorityEntry.Text, out priority))
            priority = 0;

        var game = new Game
        {
            Title = title,
            Notes = notes,
            Priority = priority,
            ImagePath = _selectedImagePath, // optional image
            Source = "Custom",
            IsCompleted = false,
            DateAdded = DateTime.Now
        };

        try
        {
            await _gameDatabase.AddGameAsync(game);
            _statusLabel.Text = "Game added successfully!";
            _statusLabel.TextColor = Colors.Green;

            // Reset inputs
            _titleEntry.Text = "";
            _notesEditor.Text = "";
            _priorityEntry.Text = "";
            _pickImageButton.Text = "Pick Image (optional)";
            _selectedImagePath = null;

            Debug.WriteLine($"[Add Custom Game] Added: {game.Title}");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error adding game: {ex.Message}";
            _statusLabel.TextColor = Colors.Red;
            Debug.WriteLine($"[Add Custom Game] Error: {ex}");
        }
    }
}
