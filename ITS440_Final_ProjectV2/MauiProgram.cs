using Microsoft.Extensions.Logging;

using ITS440_Final_ProjectV2.Services;

namespace ITS440_Final_ProjectV2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");


                });


            // Register services for dependency injection
            builder.Services.AddSingleton<SteamAuthenticationService>();
            builder.Services.AddSingleton<SecureCredentialsService>();
            builder.Services.AddSingleton<SteamApiService>();
            builder.Services.AddSingleton<Services.GameDatabase>();
            builder.Services.AddSingleton<LogoutService>();
            builder.Services.AddSingleton<NavigationService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
