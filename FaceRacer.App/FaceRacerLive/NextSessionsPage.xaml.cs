using FaceRacer.Shared;
using FaceRacer.Shared.Dto;

using FaceRacerLive.Monitor;
using FaceRacerLive.ViewModels;

namespace FaceRacerLive;

public partial class NextSessionsPage
{
    private readonly NextSessionsPageViewModel _vm;

    private bool _consoleAutoScroll = true;
    private int _consoleLineCount;

    private RaceMonitorApi? _raceMonitorApi;
    private SampleRaceSimulator? _raceMonitorSimulator;
    private CancellationTokenSource? _pollCts;

    public NextSessionsPage()
    {
        InitializeComponent();

        _vm = new NextSessionsPageViewModel();
        BindingContext = _vm;
        _raceMonitorSimulator = new SampleRaceSimulator();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        InitializeRaceMonitorApi();

        AppendConsole("Starting poll loop...", Colors.LightGray);

        StartPolling();
    }

    protected override void OnDisappearing()
    {
        StopPolling();

        base.OnDisappearing();
    }

    private void OnConsoleScrolled(object? sender, ScrolledEventArgs e)
    {
        var scroll = (ScrollView?)sender;
        if (scroll is null)
        {
            return;
        }

        var padding = 12;
        _consoleAutoScroll = e.ScrollY >= (scroll.ContentSize.Height - scroll.Height - padding);
    }

    private void OnConsoleClearClicked(object? sender, EventArgs e)
    {
        ConsoleStack.Clear();
        _consoleLineCount = 0;
    }

    public void AppendConsole(string message, Color? color = null, bool noDateTime = false)
    {
        color ??= Colors.White;

        var label = new Label
        {
            Text = noDateTime ? message : $"[{DateTime.Now:HH\\:mm\\:ss}]: {message}",
            TextColor = color,
            FontSize = 9,
            LineBreakMode = LineBreakMode.CharacterWrap
        };

        // Newest at bottom
        ConsoleStack.Add(label);

        if (_consoleAutoScroll)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Yield();

                if (ConsoleStack.Count > 0)
                {
                    await ConsoleScrollView.ScrollToAsync(ConsoleStack[^1] as Label, ScrollToPosition.End, false);
                }
            });
        }
    }

    private void InitializeRaceMonitorApi()
    {
        if (_raceMonitorApi != null)
        {
            return;
        }

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var httpClient = services?.GetService<HttpClient>();

        if (httpClient is null)
        {
            return;
        }

        _raceMonitorApi = new RaceMonitorApi(httpClient, AppSettings.CurrentSessionMonitorUrl);
    }

    private void StartPolling()
    {
        if (_pollCts is { IsCancellationRequested: false })
        {
            return;
        }

        StopPolling();

        _pollCts = new CancellationTokenSource();
        _ = RunPollLoopAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        if (_pollCts is null)
        {
            return;
        }

        _pollCts.Cancel();
        _pollCts.Dispose();
        _pollCts = null;
    }

    private async Task RunPollLoopAsync(CancellationToken ct)
    {
        bool firstPass = true;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_raceMonitorApi is not null)
                {
                    var hasSimulation = SimulationSessionStore.HasSimulation();

                    if (firstPass)
                    {
                        var mode = hasSimulation ? "SIM" : "API";
                        AppendConsole($"Poll next sessions ({mode})... " + (!hasSimulation ? AppSettings.NextSessionsMonitorUrl : ""));
                    }

                    firstPass = false;

                    var payload = hasSimulation
                        ? await _raceMonitorSimulator!.GetNextSessionsAsync(ct)
                        : await _raceMonitorApi!.GetNextSessionsAsync(AppSettings.NextSessionsMonitorUrl, ct);

                    if (payload is null)
                    {
                        AppendConsole($"Poll result: null payload");
                    }
                    else if (!payload.success)
                    {
                        AppendConsole($"Poll result: not success", Colors.OrangeRed);
                    }

                    if (payload?.success == true)
                    {
                        var sessions = MapSessions(payload);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _vm.UpdateSessions(sessions);
                        });
                    }
                }
                else
                {
                    AppendConsole("Poll skipped: RaceMonitorApi not initialized", Colors.Yellow);
                }
            }
            catch (Exception ex)
            {
                AppendConsole($"ERROR: {ex.GetType().Name}: {ex.Message}", Colors.OrangeRed);

                if (ct.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private static List<NextSessionSummaryRowViewModel> MapSessions(NextSessionsResponse payload)
    {
        var now = DateTime.Now;

        var sessions = payload.data?.data ?? new List<NextSession>();

        var mapped = new List<NextSessionSummaryRowViewModel>();

        foreach (var s in sessions)
        {
            var runs = s.runs?.data ?? new List<NextSessionRun>();
            if (runs.Count == 0)
            {
                continue;
            }

            var startTimeText = s.start_time;
            var startsInText = "—";

            if (TimeOnly.TryParseExact(startTimeText, "HH:mm", out var startTime))
            {
                var startDateTime = now.Date.Add(startTime.ToTimeSpan());

                var remaining = startDateTime - now;
                if (remaining < TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                }

                var rounded = TimeSpan.FromSeconds(Math.Round(remaining.TotalSeconds, MidpointRounding.AwayFromZero));
                startsInText = $"{(int)rounded.TotalMinutes:00}:{rounded.Seconds:00}";
            }

            var ind = runs.Count(r => string.Equals(r.kart_name, "Individual", StringComparison.OrdinalIgnoreCase));
            var dbl = runs.Count(r => string.Equals(r.kart_name, "Doble", StringComparison.OrdinalIgnoreCase));

            var driverRows = runs
                .Select(r => new NextSessionDriverRowViewModel
                {
                    FullName = r.full_name ?? r.name ?? "—",
                    BestTime = string.IsNullOrWhiteSpace(r.best_time) ? "-" : r.best_time,
                    Kart = string.IsNullOrWhiteSpace(r.kart) ? "-" : r.kart,
                    KartColor = ParseKartColor(r.kart_color)
                })
                .OrderBy(r => r.FullName)
                .ToList();

            mapped.Add(new NextSessionSummaryRowViewModel
            {
                Id = s.id,
                SessionName = NormalizeSessionName(s.name ?? s.label ?? "—"),
                StartsAtText = string.IsNullOrWhiteSpace(startTimeText) ? "—" : startTimeText,
                StartsInMinutesText = startsInText,
                DriversText = $"{ind + dbl}",
                IndividualText = ind.ToString(),
                DoubleText = dbl.ToString(),
                DetailsText = $"({ind}+{dbl})",
                Drivers = driverRows
            });
        }

        return mapped.Take(4).ToList();
    }

    private static string NormalizeSessionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "—";
        }

        const string prefixEs = "Sesión ";
        const string prefixEn = "Session ";

        if (name.StartsWith(prefixEs, StringComparison.OrdinalIgnoreCase))
        {
            return name[prefixEs.Length..].TrimStart();
        }

        if (name.StartsWith(prefixEn, StringComparison.OrdinalIgnoreCase))
        {
            return name[prefixEn.Length..].TrimStart();
        }

        return name;
    }

    private static Color ParseKartColor(string? hexWithoutHash)
    {
        if (string.IsNullOrWhiteSpace(hexWithoutHash))
        {
            return Colors.LightGray;
        }

        var hex = hexWithoutHash.Trim();
        if (!hex.StartsWith('#'))
        {
            hex = "#" + hex;
        }

        try
        {
            return Color.FromArgb(hex);
        }
        catch
        {
            return Colors.LightGray;
        }
    }
}
