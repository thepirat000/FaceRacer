using System.Collections.ObjectModel;
using System.ComponentModel;
using FaceRacer.Shared.Dto;
using System.Windows.Input;
using System.Collections.Specialized;
#pragma warning disable S3358

namespace FaceRacerLive.ViewModels;

internal sealed class TrackedRacerViewModel : INotifyPropertyChanged
{

    public ObservableCollection<LapTimeRowViewModel> Laps { get; } = new();

    private readonly Services.RaceMonitorState _raceMonitorState;

    private readonly ObservableCollection<RacerRowViewModel> _miniRacers = new();
    public ObservableCollection<RacerRowViewModel> MiniRacers => _miniRacers;

    public ICommand ClearLapsCommand { get; }

    private string _bestDeltaNextText = "-";
    private string _bestDeltaPrevText = "-";
    public string BestDeltaNextText
    {
        get => _bestDeltaNextText;
        private set
        {
            if (_bestDeltaNextText != value)
            {
                _bestDeltaNextText = value;
                OnChanged(nameof(BestDeltaNextText));
            }
        }
    }

    private string? _nextFullName;
    public string? NextFullName
    {
        get => _nextFullName;
        private set
        {
            if (_nextFullName != value)
            {
                _nextFullName = value;
                OnChanged(nameof(NextFullName));
            }
        }
    }

    public string BestDeltaPrevText
    {
        get => _bestDeltaPrevText;
        private set
        {
            if (_bestDeltaPrevText != value)
            {
                _bestDeltaPrevText = value;
                OnChanged(nameof(BestDeltaPrevText));
            }
        }
    }

    private string? _prevFullName;
    public string? PrevFullName
    {
        get => _prevFullName;
        private set
        {
            if (_prevFullName != value)
            {
                _prevFullName = value;
                OnChanged(nameof(PrevFullName));
            }
        }
    }

    private bool isLiveEnabled;
    public bool IsLiveEnabled
    {
        get => isLiveEnabled;
        set
        {
            if (isLiveEnabled != value)
            {
                isLiveEnabled = value;
                OnChanged(nameof(IsLiveEnabled));
            }
        }
    }

    public TrackedRacerViewModel(Services.RaceMonitorState raceMonitorState)
    {
        _raceMonitorState = raceMonitorState;
        ClearLapsCommand = new Command(ClearLaps);

        _raceMonitorState.Panel.Racers.CollectionChanged += OnPanelRacersChanged;
        RefreshMiniRacers();
    }

    private void OnPanelRacersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshMiniRacers();
    }

    private void RefreshMiniRacers()
    {
        var top = _raceMonitorState.Panel.Racers
            .OrderBy(r => int.TryParse(r.PositionText.TrimStart('#'), out var pos) ? pos : int.MaxValue)
            .ThenBy(r => r.FullName)
            .Take(3)
            .ToList();

        _miniRacers.Clear();
        foreach (var racer in top)
        {
            _miniRacers.Add(racer);
        }

        OnChanged(nameof(MiniRacers));
    }

    public void ClearLaps()
    {
        Laps.Clear();
        _lastRecordedRacer = null;
        AverageLapTime = "";
        BestLapNumber = null;
        LastLapNumber = null;
        LastTime = "";
        BestTime = "";
        Lap = "";
        BestDeltaNextText = "";
        BestDeltaPrevText = "";
        PrevFullName = "";
        NextFullName = "";
        SessionNumber = "";
        AverageLapTime = "";
        TotalKarts = null;
        PositionText = "-";
        TotalLaps = "";
        OnChanged(nameof(AverageLapTime));
        OnChanged(nameof(HasRacer));
        OnChanged(nameof(PositionText));
        OnChanged(nameof(PositionColor));
        OnChanged(nameof(FullName));
        OnChanged(nameof(Kart));
        OnChanged(nameof(KartColor));
        OnChanged(nameof(Lap));
        OnChanged(nameof(TotalLaps));
        OnChanged(nameof(LapProgress));
        OnChanged(nameof(SessionNumber));
        OnChanged(nameof(TotalKarts));
        OnChanged(nameof(SessionProgress));
        OnChanged(nameof(SessionProgressColor));
        OnChanged(nameof(BestTime));
        OnChanged(nameof(LastTime));
        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
        OnChanged(nameof(ArrowIcon));
        OnChanged(nameof(ArrowColor));
        OnChanged(nameof(PassedTimeText));
        OnChanged(nameof(BestDeltaPrevText));
        OnChanged(nameof(PrevFullName));
        OnChanged(nameof(BestDeltaNextText));
        OnChanged(nameof(NextFullName));
    }

    public void LoadLapsForRacer(string fullName)
    {
        Laps.Clear();
        foreach (var lap in _raceMonitorState.GetLapsForRacer(fullName))
        {
            Laps.Add(lap);
        }
        RefreshLap();
    }

    public string? SessionNumber
    {
        get;
        private set;
    }

    public int? TotalKarts
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

    public Color? PositionColor
    {
        get;
        private set;
    } = Colors.Gray;

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

    private string _kartColor = string.Empty;

    public Color KartColor => Color.TryParse("#" + _kartColor, out var c) ? c : Colors.Gray;

    public string Lap
    {
        get;
        private set;
    } = "—";

    public string TotalLaps
    {
        get;
        private set;
    }

    public string PassedTimeText
    {
        get;
        private set;
    } = "";

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

    public string? AverageLapTime
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

    public LapTimesGraphDrawable LapTimesGraphDrawable
    {
        get
        {
            var lapTimes = Laps?.Select(l => TryGetLapTimeSeconds(l.Time, 0)).ToList() ?? [];
            return new LapTimesGraphDrawable(lapTimes);
        }
    }

    public void UpdateFromSession(SessionData sessionData, string? trackedFullName)
    {
        UpdateSessionProgress(sessionData);

        if (sessionData.SessionNumber == null)
        {
            ClearForNewSession(null);
            return;
        }

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

        var karts = sessionData.body_data.Count;
        TotalKarts = karts;

        var racer = sessionData.body_data.FirstOrDefault(r => string.Equals(r.full_name, trackedFullName, StringComparison.Ordinal));
        if (racer is null)
        {
            SetNoRacer();
            return;
        }

        // Load laps for this racer from the shared state
        LoadLapsForRacer(racer.full_name ?? "-");

        // Compute best time deltas for next and previous racer
        BestDeltaNextText = "-";
        BestDeltaPrevText = "-";
        if (sessionData.body_data != null)
        {
            // Order by position (numeric)
            var ordered = sessionData.body_data
                .Where(r => !string.IsNullOrWhiteSpace(r.position))
                .OrderBy(r => int.TryParse(r.position, out var pos) ? pos : 999)
                .ToList();

            var trackedIndex = ordered.FindIndex(r => string.Equals(r.full_name, trackedFullName, StringComparison.Ordinal));
            bool trackedValid = TryParseLapTimeSeconds(racer.best_time ?? "", out var trackedBest);

            // Next racer (position+1)
            if (trackedIndex >= 0 && trackedIndex + 1 < ordered.Count)
            {
                var next = ordered[trackedIndex + 1];
                bool nextValid = TryParseLapTimeSeconds(next.best_time ?? "", out var nextBestSeconds);
                if (nextValid)
                {
                    string diff = (trackedValid && nextValid) ? (nextBestSeconds - trackedBest).ToString("+0.000;-0.000", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    BestDeltaNextText = $"{diff} | #{next.position}";
                    NextFullName = next.full_name;
                }
                else
                {
                    BestDeltaNextText = "-";
                    NextFullName = "-";
                }
            }
            else
            {
                BestDeltaNextText = "-";
                NextFullName = "-";
            }

            // Previous racer (position-1)
            if (trackedIndex > 0)
            {
                var prev = ordered[trackedIndex - 1];
                bool prevValid = TryParseLapTimeSeconds(prev.best_time ?? "", out var prevBestSeconds);
                if (prevValid)
                {
                    string diff = (trackedValid && prevValid) ? (prevBestSeconds - trackedBest).ToString("+0.000;-0.000", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    BestDeltaPrevText = $"{diff} | #{prev.position}";
                    PrevFullName = prev.full_name;
                }
                else
                {
                    BestDeltaPrevText = "-";
                    PrevFullName = "-";
                }
            }
            else
            {
                BestDeltaPrevText = "-";
                PrevFullName = "-";
            }
        }
        HasRacer = true;
        PositionText = $"#{racer.position}";
        if (!string.IsNullOrWhiteSpace(racer.position) && int.TryParse(racer.position, out var pos))
        {
            PositionColor = pos switch
            {
                1 => Colors.LimeGreen,
                2 => Colors.Gold,
                3 => Colors.Gold,
                _ => Colors.OrangeRed
            };
        }
        FullName = racer.full_name ?? "-";
        Kart = racer.kart ?? "—";
        Lap = $"{racer.passed}";
        TotalLaps = $"{racer.total}";
        LapProgress = ComputeLapProgress(racer.percentage);
        PassedTimeText = racer.passed_time.HasValue ? TimeSpan.FromMilliseconds(racer.passed_time.Value).ToString("m\\:ss") : "";
        var best = racer.best_time ?? "-";
        var last = racer.last_time ?? "-";

        var lastChanged = !string.Equals(LastTime, last, StringComparison.Ordinal) && last != "-";
        var bestChanged = !string.Equals(BestTime, best, StringComparison.Ordinal) && best != "-";
        
        BestTime = TryParseLapTimeSeconds(best, out var bestSeconds) ? bestSeconds.ToString("0.000") : "-";
        LastTime = TryParseLapTimeSeconds(last, out var lastSeconds) ? lastSeconds.ToString("0.000") : "-";
        _kartColor = racer.kart_color;

        ArrowIcon = racer.arrow == "green" ? "▲" : racer.arrow == "red" ? "▼" : "·";
        ArrowColor = racer.arrow == "green" ? Colors.LimeGreen : racer.arrow == "red" ? Colors.OrangeRed : Colors.Gray;

        OnChanged(nameof(AverageLapTime));
        OnChanged(nameof(HasRacer));
        OnChanged(nameof(PositionText));
        OnChanged(nameof(PositionColor));
        OnChanged(nameof(FullName));
        OnChanged(nameof(Kart));
        OnChanged(nameof(KartColor));
        OnChanged(nameof(Lap));
        OnChanged(nameof(TotalLaps));
        OnChanged(nameof(LapProgress));
        OnChanged(nameof(SessionNumber));
        OnChanged(nameof(TotalKarts));
        OnChanged(nameof(SessionProgress));
        OnChanged(nameof(SessionProgressColor));
        OnChanged(nameof(BestTime));
        OnChanged(nameof(LastTime));
        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
        OnChanged(nameof(ArrowIcon));
        OnChanged(nameof(ArrowColor));
        OnChanged(nameof(PassedTimeText));
        OnChanged(nameof(BestDeltaPrevText));
        OnChanged(nameof(PrevFullName));
        OnChanged(nameof(BestDeltaNextText));
        OnChanged(nameof(NextFullName));

        if (lastChanged)
        {
            StartHotFlag(ref _lastTimeHotCts, nameof(IsLastTimeHot), v => IsLastTimeHot = v, 1);
        }

        if (bestChanged)
        {
            StartHotFlag(ref _bestTimeHotCts, nameof(IsBestTimeHot), v => IsBestTimeHot = v, 3);
        }

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
        SessionProgressColor = sessionProgress < 0.5 ? Colors.LimeGreen : sessionProgress < 0.85 ? Color.FromArgb("#FBBF24") : Color.FromArgb("#FB7185");
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
        OnChanged(nameof(Laps));
    }

    private static double TryGetLapTimeSeconds(string time, double defaultValue = 0)
    {
        if (TryParseLapTimeSeconds(time, out var seconds))
        {
            return seconds;
        }

        return defaultValue;
    }

    public static bool TryParseLapTimeSeconds(string time, out double seconds)
    {
        seconds = 0;

        if (string.IsNullOrWhiteSpace(time) || time == "-")
        {
            return false;
        }

        var normalized = time.Trim();

        // Try "ss.fff" format
        if (double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
        {
            return true;
        }

        // Try "m:ss.fff" format
        var parts = normalized.Split(':');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out var min) &&
                double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sec))
            {
                seconds = min * 60 + sec;
                return true;
            }
        }

        return false;
    }

    private void RefreshLap()
    {
        double? best = null;

        foreach (var lap in Laps)
        {
            if (TryParseLapTimeSeconds(lap.Time, out var s))
            {
                best = best is null ? s : Math.Min(best.Value, s);
            }
        }

        foreach (var lap in Laps)
        {
            lap.IsFastest = best is not null && TryParseLapTimeSeconds(lap.Time, out var s) && Math.Abs(s - best.Value) < 0.0005;
        }

        BestLapNumber = Laps.FirstOrDefault(l => l.IsFastest)?.LapNumber;
        LastLapNumber = Laps.LastOrDefault()?.LapNumber;

        if (Laps.Count > 1)
        {
            var avg = Laps.Average(l =>
            {
                if (TryParseLapTimeSeconds(l.Time, out var s))
                {
                    return s;
                }
                else
                {
                    return 0;
                }
            });

            AverageLapTime = Math.Abs(avg) < 0.0005 ? "-" : avg.ToString("F3");

            OnChanged(nameof(AverageLapTime));
        }

        OnChanged(nameof(BestLapNumber));
        OnChanged(nameof(LastLapNumber));
        OnChanged(nameof(LapTimesGraphDrawable));
    }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void StartHotFlag(ref CancellationTokenSource? cts, string propertyName, Action<bool> setter, int seconds)
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
                await Task.Delay(TimeSpan.FromSeconds(seconds), token);
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
