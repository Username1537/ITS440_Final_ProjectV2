using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2;

public partial class GameDetailPage : ContentPage
{
    private readonly Game _game;
    private readonly Services.GameDatabase _db;

    private Entry priorityEntry;
    private Editor notesEditor;
    private int currentRating = 3;
    private HorizontalStackLayout starsLayout;

    public GameDetailPage(Game game, Services.GameDatabase db)
    {
        _game = game;
        _db = db;

        Title = $"{game.Title} Details";

        currentRating = game.Rating > 0 ? game.Rating : 3;

        // Priority Entry
        priorityEntry = new Entry
        {
            Keyboard = Keyboard.Numeric,
            Text = _game.Priority.ToString()
        };

        // Notes Editor
        notesEditor = new Editor
        {
            Text = _game.Notes,
            HeightRequest = 150
        };

        // Stars Layout
        starsLayout = new HorizontalStackLayout
        {
            Spacing = 5
        };
        BuildStars();

        // Save Button
        var saveButton = new Button
        {
            Text = "Save",
            BackgroundColor = Colors.DodgerBlue,
            TextColor = Colors.White
        };
        saveButton.Clicked += SaveButton_Click;

        // Layout
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    new Label { Text = game.Title, FontSize = 28, FontAttributes = FontAttributes.Bold },

                    new Label { Text = "Priority (lower = more important):" },
                    priorityEntry,

                    new Label { Text = "Notes:" },
                    notesEditor,

                    new Label { Text = "Rating:" },
                    starsLayout,

                    saveButton
                }
            }
        };
    }

    private void BuildStars()
    {
        starsLayout.Children.Clear();
        for (int i = 1; i <= 5; i++)
        {
            var starLabel = new Label
            {
                Text = i <= currentRating ? "★" : "☆",
                FontSize = 32,
                TextColor = Colors.Gold
            };

            int ratingValue = i;
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                currentRating = ratingValue;
                BuildStars(); // Rebuild to reflect updated rating
            };
            starLabel.GestureRecognizers.Add(tapGesture);

            starsLayout.Children.Add(starLabel);
        }
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        // Update properties
        if (int.TryParse(priorityEntry.Text, out int p))
            _game.Priority = p;

        _game.Notes = notesEditor.Text;
        _game.Rating = currentRating;

        try
        {
            await _db.UpdateGameAsync(_game);

            await DisplayAlert("Saved", "Game details updated.", "OK");
            Debug.WriteLine($"[GameDetailPage] Saved: {_game.Title}");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GameDetailPage] Update Error: {ex.Message}");
            await DisplayAlert("Error", $"Failed to update game: {ex.Message}", "OK");
        }
    }
}
