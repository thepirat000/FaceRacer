using FaceRacer.DB;
using FaceRacer.Services;
using FaceRacer.Services.Notifiers;
using FaceRacer.Settings;
using FaceRacer.Shared;

using Microsoft.Extensions.Configuration;

using Polly;
using Polly.Retry;

namespace FaceRacer;

internal static class Program
{
    /// <summary>
    /// Serves as the entry point for the application, initializing configuration, services, and running the main asynchronous processing loop.
    /// </summary>
    /// <param name="args">
    /// - TrackId: The identifier for the racing track to monitor.
    /// - KartId: The identifier for the kart type to monitor.
    /// </param>
    [STAThread]
    static async Task Main(string[] args)
    {
        // Bind settings
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "FaceRacer_")      // e.g. FaceRacer_TrackId=...
            .AddCommandLine(args)                               // e.g. --TrackId=...
            .Build();

        var appSettings = new AppSettings();
        config.Bind(appSettings);

        Console.WriteLine($"Running for track ID: {appSettings.TrackId}, kart ID: {appSettings.KartId}\n");

        await using (var ctx = new FaceRacerDbContext())
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nCTRL+C pressed. Canceling...");
            e.Cancel = true;
            cts.Cancel();
        };

        var faceRacerJob = new FaceRacerJob(appSettings, Console.WriteLine, cts);

        await faceRacerJob.ExecuteFaceRacerLoopAsync();
    }

}