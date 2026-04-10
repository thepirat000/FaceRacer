using System.Net;
using CommunityToolkit.Maui;

using FaceRacerLive.Monitor;
using FaceRacerLive.Services;

using Microsoft.Extensions.Logging;


namespace FaceRacerLive;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // Custom Fonts:
                fonts.AddFont("OpenSans-ExtraBold.ttf", "Sans");
                fonts.AddFont("digital-7.ttf", "Digital");
                fonts.AddFont("AtkinsonHyperlegibleNext-ExtraBold.ttf", "Atkinson");
                fonts.AddFont("Frutiger_bold.ttf", "Frutiger");
                fonts.AddFont("SpaceMono-Regular.ttf", "SpaceMono");
            });
#if ANDROID
        builder.Services.AddSingleton<IMicToSpeakerService, MicToSpeakerService>();
        Platforms.Android.MonitorRankingWebViewHandler.EnableZoom();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddHttpClient(string.Empty, x =>
        {
            x.Timeout = AppSettings.TimeoutForLiveRequest;
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            ConnectTimeout = AppSettings.TimeoutForLiveRequest
        });

        builder.Services.AddSingleton<RaceMonitorState>();
        builder.Services.AddSingleton<SampleRaceSimulator>();

        var app = builder.Build();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // this will only work after the UI has initialized
            await SpeakHelper.Initialize("es-mx");
        });

        return app;
    }

    
}

