using Microsoft.AspNetCore.Mvc;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var simulationState = new SimulationState();

app.MapGet("/", () => Results.Json(new
{
    name = "FaceRacer API Emulator",
    endpoints = new[]
    {
        "/ajax/monitors/current-session-monitor",
        "/ajax/monitors/next-sessions?from=0&limit=5",
        "/ajax/user-ranking-by-time-box?track_configuration_id=1&kart_id=7&period=week",
        "/ajax/user-best-ranking-by-time?user_id=101&track_configuration_id=1&kart_id=7",
        "/ajax/session/chart-data?user_id=101&session_id=session-1001",
        "/ajax/sessions-boxes?user_id=101&track_configuration_id=1&period=all&start_from=0&only_victories=0&only_best_time_sessions=0&kart_id=7",
        "/es/monitors/monitor-ranking",
        "/es/monitors/monitor-current-sessions"
    },
    simulation = "optional query params: ?fail=true or ?timeout=true (default false)"
}));

app.MapGet("/ajax/monitors/current-session-monitor", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    var tick = simulationState.NextTick();
    var totalLaps = 12;
    var sessionPassedTime = 4_000 + ((tick * 700) % 48_000);
    var leaderLap = Math.Min(totalLaps, 1 + (tick / 4));

    var marioBestLapSeconds = 33.992;
    var lauraBestLapSeconds = 34.225;

    var marioLastLapSeconds = marioBestLapSeconds + 0.08 + ((tick % 5) * 0.03);
    var lauraLastLapSeconds = lauraBestLapSeconds + 0.40 + ((tick % 6) * 0.04);

    var marioLineGuess = 6_000 + ((tick % 4) * 250);
    var lauraLineGuess = 6_400 + ((tick % 4) * 250);

    var marioPassedTime = sessionPassedTime;
    var lauraPassedTime = Math.Max(0, sessionPassedTime - 550);

    var marioPercentage = Math.Clamp((int)Math.Round((double)marioPassedTime * 100 / marioLineGuess), 0, 130);
    var lauraPercentage = Math.Clamp((int)Math.Round((double)lauraPassedTime * 100 / lauraLineGuess), 0, 130);

    return Results.Json(new
    {
        runnin_session_title = "Session #231",
        show_checkered_flag = leaderLap >= totalLaps,
        footer_line = "Race in progress (simulated)",
        passed_time = sessionPassedTime,
        session_ranking = "time",
        current_high_lap = leaderLap,
        total_laps_for_all = totalLaps,
        body_data = new[]
        {
            new
            {
                position = "1",
                name = "Mario Rossi",
                kart = "7",
                kart_color = "ff5722",
                passed = leaderLap,
                total = totalLaps,
                last_time = marioLastLapSeconds.ToString("F3", CultureInfo.InvariantCulture),
                arrow = "green",
                best_time = marioBestLapSeconds.ToString("F3", CultureInfo.InvariantCulture),
                passed_time = marioPassedTime,
                percentage = marioPercentage.ToString(CultureInfo.InvariantCulture),
                avg_lap = 34510 + ((tick % 5) * 20),
                consistency_lap = 420 + ((tick % 4) * 10),
                number_of_pits = 0,
                checkered_flag_mode_at = "",
                checkered_flag = leaderLap >= totalLaps,
                front_flag = "",
                line_guess = marioLineGuess,
                line_percentage = marioPercentage,
                full_name = "Mario Rossi",
                diff = (marioLastLapSeconds - marioBestLapSeconds).ToString("F3", CultureInfo.InvariantCulture)
            },
            new
            {
                position = "2",
                name = "Laura Bianchi",
                kart = "12",
                kart_color = "3f51b5",
                passed = Math.Max(1, leaderLap - (tick % 2)),
                total = totalLaps,
                last_time = lauraLastLapSeconds.ToString("F3", CultureInfo.InvariantCulture),
                arrow = "red",
                best_time = lauraBestLapSeconds.ToString("F3", CultureInfo.InvariantCulture),
                passed_time = lauraPassedTime,
                percentage = lauraPercentage.ToString(CultureInfo.InvariantCulture),
                avg_lap = 34930 + ((tick % 5) * 22),
                consistency_lap = 510 + ((tick % 4) * 12),
                number_of_pits = 0,
                checkered_flag_mode_at = "",
                checkered_flag = leaderLap >= totalLaps,
                front_flag = "",
                line_guess = lauraLineGuess,
                line_percentage = lauraPercentage,
                full_name = "Laura Bianchi",
                diff = (lauraLastLapSeconds - lauraBestLapSeconds).ToString("F3", CultureInfo.InvariantCulture)
            }
        }
    });
});

app.MapGet("/ajax/monitors/next-sessions", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Json(new
    {
        success = true,
        errors = false,
        data = new
        {
            data = new[]
            {
                new
                {
                    id = 701,
                    name = "Heat 701",
                    plain_name = "Heat 701",
                    label = "Junior",
                    start_time = DateTime.UtcNow.AddMinutes(10).ToString("HH:mm"),
                    participants_text = "12 drivers",
                    start_after = "10 min",
                    checkin_time = DateTime.UtcNow.AddMinutes(5).ToString("HH:mm"),
                    time_left = "00:10:00",
                    runs = new
                    {
                        data = new[]
                        {
                            new
                            {
                                name = "Mario R.",
                                full_name = "Mario Rossi",
                                kart_color = "ff5722",
                                kart_name = "Kart 7",
                                kart = "7",
                                best_time = "33.992",
                                kart_id = 7,
                                uuid = "run-701-01"
                            }
                        }
                    }
                }
            }
        }
    });
});

app.MapGet("/ajax/user-ranking-by-time-box", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Json(new
    {
        data = new
        {
            ranking = new
            {
                first = new
                {
                    user_uuid = "user-101",
                    user_id = 101,
                    pos = 1,
                    full_name = "Mario Rossi",
                    first_name = "Mario",
                    last_name = "Rossi",
                    age = 31,
                    profile_image = "https://cdn.fake/profile_101.png",
                    profile_image_normal = "https://cdn.fake/profile_101_n.png",
                    profile_image_medium = "https://cdn.fake/profile_101_m.png",
                    profile_image_small = "https://cdn.fake/profile_101_s.png",
                    profile_url = "/profile/mario.rossi",
                    rank = "A",
                    user_rank_id = 3,
                    age_group = "adult",
                    best_time = "33.992",
                    date = "2026-02-15",
                    username = "mario.rossi",
                    run_id = "run-888",
                    session_uuid = "session-1001",
                    kart_id = 7,
                    best_time_ms = 33992
                },
                second = new
                {
                    user_uuid = "user-102",
                    user_id = 102,
                    pos = 2,
                    full_name = "Laura Bianchi",
                    first_name = "Laura",
                    last_name = "Bianchi",
                    age = 28,
                    profile_image = "https://cdn.fake/profile_102.png",
                    profile_image_normal = "https://cdn.fake/profile_102_n.png",
                    profile_image_medium = "https://cdn.fake/profile_102_m.png",
                    profile_image_small = "https://cdn.fake/profile_102_s.png",
                    profile_url = "/profile/laura.bianchi",
                    rank = "A",
                    user_rank_id = 3,
                    age_group = "adult",
                    best_time = "34.225",
                    date = "2026-02-15",
                    username = "laura.bianchi",
                    run_id = "run-889",
                    session_uuid = "session-1002",
                    kart_id = 12,
                    best_time_ms = 34225
                }
            }
        }
    });
});

app.MapGet("/ajax/user-best-ranking-by-time", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Json(new
    {
        success = true,
        error = false,
        data = new
        {
            position = 1,
            avatar = "https://cdn.fake/profile_101.png",
            full_name = "Mario Rossi",
            best_time = "33.992",
            date = "2026-02-15",
            profile_url = "/profile/mario.rossi",
            uuid = "best-time-101",
            weather_track_temp = "24",
            weather_air_temp = "19",
            weather_humidity = "46",
            weather_air_pressure = "1013"
        }
    });
});

app.MapGet("/ajax/session/chart-data", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Json(new
    {
        tooltips = true,
        reverse = false,
        step_size = "1",
        max_ticks_limit = "12",
        x_label = "Lap",
        y_label = "Seconds",
        data = new
        {
            labels = new[] { "1", "2", "3", "4", "5" },
            min = 33,
            max = 37,
            datasets = new[]
            {
                new
                {
                    label = "Mario Rossi",
                    data = new[] { 34.8, 34.2, 33.9, 34.1, 34.0 },
                    backgroundColor = "rgba(255,87,34,0.3)",
                    borderColor = "rgba(255,87,34,1)",
                    pointBorderColor = "rgba(255,87,34,1)",
                    pointBackgroundColor = "rgba(255,87,34,1)",
                    lineTension = 0,
                    borderWidth = 2
                }
            }
        }
    });
});

app.MapGet("/ajax/sessions-boxes", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    const string html = """
<div class='session-result-container' data-user-id='101' data-session-uuid='session-1001'>
  <div class='session-result-visible-section'>
    <div class='top'><span class='position inline'>#1/12</span></div>
    <div class='minified-content'><span class='minified-name'>Mario Rossi</span></div>
    <div class='minified-stat time'><span class='minified-stat-value'>33.992</span></div>
    <div class='minified-stat date'><span class='date'>15.02.2026</span><span class='clock'>at 18:30</span></div>
  </div>
  <div class='tab_ranking_container'>
    <div class='table_content session_content'>
      <div class='row active'><a class='name_content' href='/profile/mario.rossi'>Mario</a></div>
    </div>
  </div>
  <div class='tab_laps'>
    <div class='table_content session_content'>
      <div class='row'><span class='position'>1</span><span class='lap-name'>Lap 1</span><a class='time_laps first'>34.800</a></div>
      <div class='row'><span class='position'>2</span><span class='lap-name'>Lap 2</span><a class='time_laps first'>33.992</a></div>
    </div>
  </div>
</div>
""";

    return Results.Json(new
    {
        success = true,
        error = false,
        total = 1,
        html
    });
});

app.MapGet("/es/monitors/monitor-ranking", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Content("<html><body><h1>Monitor Ranking Emulator</h1><p>Use /ajax/user-ranking-by-time-box for data.</p></body></html>", "text/html");
});

app.MapGet("/es/monitors/monitor-current-sessions", async ([FromQuery] bool fail = false, [FromQuery] bool timeout = false, CancellationToken cancellationToken = default) =>
{
    var simulatedFailure = await TrySimulateFaultAsync(fail, timeout, cancellationToken);

    if (simulatedFailure is not null)
    {
        return simulatedFailure;
    }

    return Results.Content("<html><body><h1>Monitor Current Sessions Emulator</h1><p>Use /ajax/monitors/current-session-monitor for data.</p></body></html>", "text/html");
});

app.Run();

static async Task<IResult?> TrySimulateFaultAsync(bool fail, bool timeout, CancellationToken cancellationToken)
{
    if (timeout)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        return Results.Problem(
            title: "Simulated timeout",
            detail: "The emulator intentionally delayed the response.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }

    if (fail)
    {
        return Results.Problem(
            title: "Simulated API failure",
            detail: "The emulator intentionally returned an error.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    return null;
}

sealed class SimulationState
{
    private int _tick;

    public int NextTick()
    {
        return Interlocked.Increment(ref _tick) - 1;
    }
}
