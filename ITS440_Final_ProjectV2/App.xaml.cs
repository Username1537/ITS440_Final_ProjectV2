using ITS440_Final_ProjectV2.Models;
using ITS440_Final_ProjectV2.Services;

namespace ITS440_Final_ProjectV2
{
    public partial class App : Application
    {
        public static GameDatabase GameDatabase { get; set; }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // Initialize GameDatabase as a singleton
            GameDatabase = new GameDatabase();
            return new Window(new AppShell());
        }
    }
}