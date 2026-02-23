using System.Collections.ObjectModel;
using System.ComponentModel;
using FaceRacer.Shared.Dto;
using System.Windows.Input;

namespace FaceRacerLive.ViewModels;

internal sealed class TrackedRacerViewModel : INotifyPropertyChanged
{
    public ObservableCollection<LapTimeRowViewModel> Laps { get; } = new();

    public ICommand ClearLapsCommand { get; }

    public TrackedRacerViewModel()
    {
        ClearLapsCommand = new Command(ClearLaps);
    }

    public void ClearLaps()
    {
        Laps.Clear();
        _lastRecordedRacer = null;
        _lastRecordedPassed = null;

        _bestLapSeconds = null;
        OnChanged(nameof(Laps));
    }

    public string? SessionNumber
    {
        get;
        private set;
    }

    public bool HasRacer
    {
        get;
        private set;
    }

    public string PositionText
    {
        get;
        private set;
    } = "—";

    public string FullName
    {
        get;
        private set;
    } = "—";

    public string Kart
    {
        get;
        private set;
    } = "—";

    private string _kart_color = string.Empty;

    public Color KartColor => Color.TryParse("#" + _kart_color, out var c) ? c : Colors.Gray;

    public string LapText
    {
        get;
        private set;
    } = "—";

    public double LapProgress
    {
        get;
        private set;
    }

    public double SessionProgress
    {
        get;
        private set;
    }

    public Color SessionProgressColor
    {
        get;
        private set;
    } = Colors.LimeGreen;

    public string ArrowIcon
    {
        get;
        private set;
    } = "·";

    public Color ArrowColor
    {
        get;
        private set;
    } = Colors.Gray;

    public string BestTime
    {
        get;
        private set;
    } = "-";

    public int? BestLapNumber
    {
        get;
        private set;
    }

    public string LastTime
    {
        get;
        private set;
    } = "-";

    public int? LastLapNumber
    {
        get;
        private set;
    }

    public bool IsLastTimeHot
    {
        get;
        private set;
    }

    public bool IsBestTimeHot
    {
        get;
        private set;
    }

    private CancellationTokenSource? _lastTimeHotCts;
    private CancellationTokenSource? _bestTimeHotCts;

    private string? _lastRecordedSession;
    private string? _lastRecordedRacer;
    private int? _lastRecordedPassed;

    private double? _bestLapSeconds;

    public void UpdateFromSession(SessionData sessionData, string? trackedFullName)
    {
        UpdateSessionProgress(sessionData);

        if (sessionData.SessionNumber != null
            && _lastRecordedSession != null
            && !string.Equals(sessionData.SessionNumber, _lastRecordedSession, StringComparison.Ordinal))
        {
            ClearForNewSession(sessionData.SessionNumber);
        }

        SessionNumber = sessionData.SessionNumber;

        if (string.IsNullOrWhiteSpace(trackedFullName) || sessionData.body_data is null)
        {
            SetNoRacer();
            return;
        }

        var racer = sessionData.body_data.FirstOrDefault(r => string.Equals(r.full_name, trackedFullName, StringComparison.Ordinal));
        if (racer is null)
        {
            SetNoRacer();
            return;
        }

        HasRacer = true;
        PositionText = $"#{racer.position}";
        FullName = racer.full_name ?? "—";
        Kart = racer.kart ?? "—";
        LapText = $"{racer.passed}/{racer.total}";
        LapProgress = ComputeLapProgress(racer.percentage);
        var nextBest = racer.best_time ?? "-";
        var nextLast = racer.last_time ?? "-";

        var lastChanged = !string.Equals(LastTime, nextLast, StringComparison.Ordinal) && nextLast != "-";
        var bestChanged = !string.Equals(BestTime, nextBest, StringComparison.Ordinal) && nextBest != "-";

        BestTime = nextBest;
        LastTime = nextLast;
        _kart_color = racer.kart_color;

        ArrowIcon = racer.arrow == "green" ? "▲" : racer.arrow == "red" ? "▼" : "·";

        ArrowColor = string.Equals(racer.arrow, "green", StringComparison.OrdinalIgnoreCase) ? Colors.LimeGreen :
            string.Equals(racer.arrow, "red", StringComparison.OrdinalIgnoreCase) ? Colors.OrangeRed :
            Colors.Gray;

        OnChanged(nameof(HasRacer));
        OnChanged(nameof(PositionText));
        OnChanged(nameof(FullName));
        OnChanged(nameof(Kart));
        OnChanged(nameof(KartColor));
        OnChanged(nameof(LapText));
        OnChanged(nameof(LapProgress));
        OnChanged(nameof(SessionNumber));
        OnChanged(nameof(SessionProgress));
        OnChanged(nameof(SessionProgressColor));
        OnChanged(nameof(BestTime));
        OnChanged(nameof(LastTime));
        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
        OnChanged(nameof(ArrowIcon));
        OnChanged(nameof(ArrowColor));

        if (lastChanged)
        {
            StartHotFlag(ref _lastTimeHotCts, nameof(IsLastTimeHot), v => IsLastTimeHot = v);
        }

        if (bestChanged)
        {
            StartHotFlag(ref _bestTimeHotCts, nameof(IsBestTimeHot), v => IsBestTimeHot = v);
        }

        TryAppendLap(racer, sessionData.SessionNumber);

        // Derive lap numbers for labels from collected laps.
        LastLapNumber = Laps.LastOrDefault()?.LapNumber;
        BestLapNumber = Laps.FirstOrDefault(l => l.IsFastest)?.LapNumber;
        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
    }

    private void UpdateSessionProgress(SessionData sessionData)
    {
        var sessionProgress = Math.Clamp(
            sessionData.current_high_lap.GetValueOrDefault() /
            (double)sessionData.total_laps_for_all.GetValueOrDefault(sessionData.current_high_lap.GetValueOrDefault(1)),
            0,
            100);

        SessionProgress = sessionProgress;
        SessionProgressColor = sessionProgress < 0.5 ? Colors.LimeGreen : sessionProgress < 0.85 ? Colors.Gold : Colors.Red;
    }

    private static double ComputeLapProgress(string? percentage)
    {
        var p = double.TryParse(percentage, out var percentageNum) ? percentageNum : 0;
        if (double.IsNaN(p) || double.IsInfinity(p))
        {
            return 0;
        }

        p = Math.Clamp(p, 0, 100);
        return p / 100.0;
    }

    private void SetNoRacer()
    {
        HasRacer = false;
        OnChanged(nameof(HasRacer));
    }

    private void ClearForNewSession(string? sessionNumber)
    {
        Laps.Clear();
        _lastRecordedSession = sessionNumber;
        _lastRecordedRacer = null;
        _lastRecordedPassed = null;
        _bestLapSeconds = null;
        OnChanged(nameof(Laps));
    }

    private void TryAppendLap(SessionRacerData racer, string? sessionNumber)
    {
        var passed = racer.passed;
        if (passed <= 0)
        {
            return;
        }

        var completedLapNumber = Math.Max(1, passed - 1);

        if (_lastRecordedSession is not null && sessionNumber is not null && !string.Equals(_lastRecordedSession, sessionNumber, StringComparison.Ordinal))
        {
            ClearForNewSession(sessionNumber);
        }

        if (_lastRecordedRacer is not null && !string.Equals(_lastRecordedRacer, racer.full_name, StringComparison.Ordinal))
        {
            // Tracked racer changed within same session: keep info panel, reset laps list.
            Laps.Clear();
            _bestLapSeconds = null;
        }

        var lastTime = racer.last_time;
        if (string.IsNullOrWhiteSpace(lastTime) || lastTime == "-")
        {
            return;
        }

        ClearLapsFromLapNumber(completedLapNumber);

        // If we already recorded a lap equal to this number, do not duplicate.
        if (Laps.Any(l => l.LapNumber == completedLapNumber))
        {
            _lastRecordedSession = sessionNumber;
            _lastRecordedRacer = racer.full_name;
            _lastRecordedPassed = passed;
            return;
        }

        Laps.Add(new LapTimeRowViewModel(completedLapNumber, lastTime, ArrowIcon, ArrowColor));

        RefreshFastestLapHighlight();

        _lastRecordedSession = sessionNumber;
        _lastRecordedRacer = racer.full_name;
        _lastRecordedPassed = passed;
    }

    private void ClearLapsFromLapNumber(int fromLapNumber)
    {
        if (Laps.Count == 0)
        {
            return;
        }

        var maxLap = Laps[^1].LapNumber;
        if (maxLap < fromLapNumber)
        {
            return;
        }

        var removedAny = false;
        for (var i = Laps.Count - 1; i >= 0; i--)
        {
            if (Laps[i].LapNumber > fromLapNumber)
            {
                Laps.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            RefreshFastestLapHighlight();
        }
    }

    private static bool TryParseLapTimeSeconds(string time, out double seconds)
    {
        seconds = 0;

        if (string.IsNullOrWhiteSpace(time) || time == "-")
        {
            return false;
        }

        var normalized = time.Trim().Replace(':', '.');
        return double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
    }

    private void RefreshFastestLapHighlight()
    {
        double? best = null;

        foreach (var lap in Laps)
        {
            if (TryParseLapTimeSeconds(lap.Time, out var s))
            {
                best = best is null ? s : Math.Min(best.Value, s);
            }
        }

        _bestLapSeconds = best;

        foreach (var lap in Laps)
        {
            lap.IsFastest = best is not null && TryParseLapTimeSeconds(lap.Time, out var s) && Math.Abs(s - best.Value) < 0.0005;
        }

        BestLapNumber = Laps.FirstOrDefault(l => l.IsFastest)?.LapNumber;
        LastLapNumber = Laps.LastOrDefault()?.LapNumber;
        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
    }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void StartHotFlag(ref CancellationTokenSource? cts, string propertyName, Action<bool> setter)
    {
        cts?.Cancel();
        cts?.Dispose();

        cts = new CancellationTokenSource();
        var token = cts.Token;

        setter(true);
        OnChanged(propertyName);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            setter(false);
            MainThread.BeginInvokeOnMainThread(() => OnChanged(propertyName));
        }, token);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
