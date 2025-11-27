using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2
{
    public partial class MainPage : ContentPage
    {
        //int count = 0;

        //steam web API INFO
        //key:  0C6FFFE121ED4B4252AA4830DA19586A
        //domain name: my-app 

        // UI Controls
        private readonly Entry _apiKeyEntry;
        private readonly Button _testButton;
        private readonly Label _resultLabel;

        // HttpClient for making web requests.
        // For a real app, use IHttpClientFactory, but this is simpler for a test.
        private static readonly HttpClient _httpClient = new HttpClient();

        public MainPage()
        {
            Title = "Steam API Connection Test 2";

            // --- Define the UI Controls ---

            _apiKeyEntry = new Entry
            {
                Placeholder = "Enter your Steam API Key here",
                Margin = new Thickness(20)
            };

            _testButton = new Button
            {
                Text = "Test Connection",
                Margin = new Thickness(20, 0)
            };

            _resultLabel = new Label
            {
                Text = "Waiting for test...",
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(20)
            };

            // --- Wire up the Button Click Event ---
            _testButton.Clicked += OnTestConnectionClicked;

            // --- Build the Page Layout ---
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = "Steam API Tester",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    _apiKeyEntry,
                    _testButton,
                    _resultLabel
                }
            };
        }

        /// <summary>
        /// Called when the "Test Connection" button is clicked.
        /// </summary>
        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            string apiKey = _apiKeyEntry.Text;

            // 1. Basic validation
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _resultLabel.Text = "Please enter an API key.";
                _resultLabel.TextColor = Colors.Red;
                return;
            }

            // 2. Update UI to show work is being done
            _resultLabel.Text = "Testing connection...";
            _resultLabel.TextColor = Colors.Grey;
            _testButton.IsEnabled = false;

            try
            {
                // 3. Define the API endpoint to test.
                // We use "ISteamWebAPIUtil/GetSupportedAPIList" as it's a simple
                // endpoint that requires a key and confirms the API is reachable.
                string apiUrl = $"https://api.steampowered.com/ISteamWebAPIUtil/GetSupportedAPIList/v1/?key={apiKey}";

                // 4. Make the asynchronous web request
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                // 5. Check the response
                if (response.IsSuccessStatusCode)
                {
                    // HTTP 200 OK
                    _resultLabel.Text = "Connection Successful!";
                    _resultLabel.TextColor = Colors.Green;
                }
                else
                {
                    // Handle common errors
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // HTTP 403 Forbidden - most likely an invalid API key
                        _resultLabel.Text = "Connection Failed. Check your API key (403 Forbidden).";
                    }
                    else
                    {
                        // Other HTTP errors
                        _resultLabel.Text = $"Connection Failed. Status code: {response.StatusCode}";
                    }
                    _resultLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                // 6. Handle exceptions (e.g., no internet connection)
                _resultLabel.Text = $"An error occurred: {ex.Message}";
                _resultLabel.TextColor = Colors.Red;
                Console.WriteLine($"[Steam API Test] Error: {ex}");
            }
            finally
            {
                // 7. Re-enable the button
                _testButton.IsEnabled = true;
            }
        }
    }
}
