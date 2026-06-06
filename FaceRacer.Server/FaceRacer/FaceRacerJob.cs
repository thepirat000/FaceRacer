using FaceRacer.Services;
using FaceRacer.Services.Notifiers;
using FaceRacer.Settings;
using FaceRacer.Shared;

using Polly;
using Polly.Retry;

namespace FaceRacer;

public class FaceRacerJob
{
    private readonly AppSettings _serverSettings;
    private readonly Action<string> _logger;
    private readonly CancellationTokenSource _cts;

    private readonly Services.FaceRacerService _faceRacerService;
    private readonly TelegramBot _telegramBot = null;

    public FaceRacerJob(AppSettings serverSettings, Action<string> logger, CancellationTokenSource cts)
    {
        _serverSettings = serverSettings;
        _logger = logger;
        _cts = cts;

        var httpClient = new HttpClient();
        var api = new RaceFacerApi(httpClient);
        var chart = new ChartLaps(httpClient);
        _faceRacerService = new Services.FaceRacerService(_serverSettings, api);
        if (!_serverSettings.BotDisabled)
        {
            _telegramBot = new TelegramBot(_serverSettings, api, chart);
        }
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

    public async Task ExecuteFaceRacerLoopAsync()
    {
        // Setup Business Logic
        if (!_serverSettings.BotDisabled)
        {
            // Setup Telegram Bot
            _logger.Invoke("Setting up Telegram Bot...");
            var pipeline = new ResiliencePipelineBuilder().AddRetry(RetryOptions).Build();
            await pipeline.ExecuteAsync(async ct => await _telegramBot.SetupBot(ct), _cts.Token);
            _logger.Invoke("Telegram Bot setup complete.");
        }

        // Setup Notifiers
        var notifiers = new List<INotifier> { new ConsoleNotifier(), new TelegramNotifier(_serverSettings) };

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                DateTime? firstChangedDate = null;

                // Update & Notify
                try
                {
                    firstChangedDate = await _faceRacerService.RunUpdateAsync(cancellationToken: _cts.Token);
                }
                catch (Exception e)
                {
                    _logger.Invoke("##### Exception thrown during update:\n" + e);
                }

                if (firstChangedDate.HasValue)
                {
                    _logger.Invoke("Running notifications...");
                    try
                    {
                        await _faceRacerService.NotifyUpdateAsync(firstChangedDate.Value, notifiers, _cts.Token);
                    }
                    catch (Exception e)
                    {
                        _logger.Invoke("##### Exception thrown during notify:\n" + e);
                    }
                }

                // Check for racer alerts & Notify
                var userIds = _serverSettings.WatchRacers.UserIds;
                _ = userIds.Count > 0 ? await _faceRacerService.GetUsersLastSessionAndNotifyAsync(userIds, notifiers, _cts.Token) : [];
                
                if (_serverSettings.RunOnce)
                {
                    _cts.Cancel(false);
                }
                else
                {
                    _logger.Invoke($"Wait for {_serverSettings.Pause} secs...\n");
                    await Task.Delay(_serverSettings.Pause * 1000, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // token triggered cancellation
            _logger.Invoke("Operation canceled.");
        }

        _logger.Invoke("Exited gracefully.");
    }
}