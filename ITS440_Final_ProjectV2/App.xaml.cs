using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;

namespace ITS440_Final_ProjectV2
{
    public partial class App : Application
    {
        public static Services.GameDatabase GameDatabase { get; set; }

        public App()
        {
            InitializeComponent();
            // Global resources for the light blue theme
            var fontFamily = "Anton-Bold"; // must match the font alias in Resources/Fonts
            var primaryColor = Colors.LightBlue;
            var secondaryColor = Colors.DodgerBlue;
            var buttonTextColor = Colors.White;

            // Apply styles globally
            Resources["LabelStyle"] = new Style(typeof(Label))
            {
                Setters =
            {
                new Setter { Property = Label.FontFamilyProperty, Value = fontFamily },
                new Setter { Property = Label.TextColorProperty, Value = Colors.Black }
            }
            };

            Resources["ButtonStyle"] = new Style(typeof(Button))
            {
                Setters =
            {
                new Setter { Property = Button.FontFamilyProperty, Value = fontFamily },
                new Setter { Property = Button.BackgroundColorProperty, Value = secondaryColor },
                new Setter { Property = Button.TextColorProperty, Value = buttonTextColor },
                new Setter { Property = Button.CornerRadiusProperty, Value = 8 },
                new Setter { Property = Button.PaddingProperty, Value = new Thickness(10,5) }
            }
            };

            Resources["EntryStyle"] = new Style(typeof(Entry))
            {
                Setters =
            {
                new Setter { Property = Entry.FontFamilyProperty, Value = fontFamily }
            }
            };

            Resources["EditorStyle"] = new Style(typeof(Editor))
            {
                Setters =
                {
                    new Setter { Property = Editor.FontFamilyProperty, Value = fontFamily }
                }
            };
        }
            

        protected override Window CreateWindow(IActivationState activationState)
        {
            // Initialize GameDatabase as a singleton
            GameDatabase = new Services.GameDatabase();
            return new Window(new AppShell());
        }
    }
}