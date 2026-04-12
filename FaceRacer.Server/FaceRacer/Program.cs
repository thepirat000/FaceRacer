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

        // Setup Business Logic
        var httpClient = new HttpClient();
        var api = new RaceFacerApi(httpClient);
        var chart = new ChartLaps(httpClient);
        var bl = new Services.FaceRacer(appSettings, api);

        if (!appSettings.BotDisabled)
        {
            // Setup Telegram Bot
            Console.WriteLine("Setting up Telegram Bot...");
            var telegramBot = new TelegramBot(appSettings, api, chart);
            var pipeline = new ResiliencePipelineBuilder().AddRetry(RetryOptions).Build();
            await pipeline.ExecuteAsync(async ct => await telegramBot.SetupBot(ct), cts.Token);
        }

        // Setup Notifiers
        var notifiers = new List<INotifier> { new ConsoleNotifier(), new TelegramNotifier(appSettings) };

        try
        {
            while (!cts.IsCancellationRequested)
            {
                DateTime? firstChangedDate = null;

                // Update & Notify
                try
                {
                    firstChangedDate = await bl.RunUpdateAsync(cancellationToken: cts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine("##### Exception thrown during update:\n" + e);
                }
                
                if (firstChangedDate.HasValue)
                {
                    Console.WriteLine("Running notifications...");
                    try
                    {
                        await bl.NotifyAsync(firstChangedDate.Value, notifiers, cts.Token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("##### Exception thrown during notify:\n" + e);
                    }
                }

                if (appSettings.RunOnce)
                {
                    cts.Cancel(false);
                }
                else
                {
                    Console.WriteLine($"Wait for {appSettings.Pause} secs...\n");
                    await Task.Delay(appSettings.Pause * 1000, cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // token triggered cancellation
            Console.WriteLine("Operation canceled.");
        }

        Console.WriteLine("Exited gracefully.");
    }

    private static readonly RetryStrategyOptions RetryOptions = new()
    {
        Delay = TimeSpan.Zero,
        MaxRetryAttempts = 4,
        OnRetry = args =>
        {
            Console.WriteLine($"Retry #{args.AttemptNumber}");
            return default;
        }
    };
}