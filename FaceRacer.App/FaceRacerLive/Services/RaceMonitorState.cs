using FaceRacerLive.ViewModels;
using FaceRacer.Shared.Dto;

namespace FaceRacerLive.Services;

internal sealed class RaceMonitorState
{
    public RaceMonitorPanelViewModel Panel { get; } = new();
    public TrackedRacerViewModel Tracked { get; }

    private bool _isLive;
    public bool IsLive
    {
        get => _isLive;
        set
        {
            if (_isLive != value)
            {
                _isLive = value;
                LiveChanged?.Invoke(value);
                if (value)
                {
                    _racerLaps.Clear();
                    Tracked.ClearLaps();
                }
            }
        }
    }
    public Action<bool>? LiveChanged { get; set; }
    private bool _isAutoTrackEnabled;
    public bool IsAutoTrackEnabled
    {
        get => _isAutoTrackEnabled;
        set
        {
            if (_isAutoTrackEnabled != value)
            {
                _isAutoTrackEnabled = value;
                AutoTrackChanged?.Invoke(value);
            }
        }
    }
    public Action<bool>? AutoTrackChanged { get; set; }

    // --- Centralized lap storage ---
    private string? _currentSessionNumber;
    private readonly Dictionary<string, List<LapTimeRowViewModel>> _racerLaps = new(StringComparer.Ordinal);

    public RaceMonitorState()
    {
        Tracked = new TrackedRacerViewModel(this);
    }

    public void UpdateSessionLaps(SessionData sessionData)
    {
        var sessionNumber = sessionData.SessionNumber;
        if (_currentSessionNumber != sessionNumber)
        {
            _racerLaps.Clear();
            _currentSessionNumber = sessionNumber;
            Tracked.ClearLaps();
        }

        if (!IsLive || sessionData.body_data == null)
        {
            return;
        }

        foreach (var racer in sessionData.body_data)
        {
            if (string.IsNullOrWhiteSpace(racer.full_name))
            {
                continue;
            }

            if (!_racerLaps.TryGetValue(racer.full_name, out var laps))
            {
                laps = new List<LapTimeRowViewModel>();
                _racerLaps[racer.full_name] = laps;
            }

            // Add new laps if any (avoid duplicates)
            int completedLapNumber = Math.Max(1, racer.passed - 1);
            if (laps.All(l => l.LapNumber != completedLapNumber))
            {
                var lastTime = racer.last_time;
                if (!string.IsNullOrWhiteSpace(lastTime) && lastTime != "-")
                {
                    string diffPreviousText = "";
                    if (laps.Count > 0 &&
                        TrackedRacerViewModel.TryParseLapTimeSeconds(laps[^1].Time, out var prev) &&
                        TrackedRacerViewModel.TryParseLapTimeSeconds(lastTime, out var curr))
                    {
                        diffPreviousText = (curr - prev).ToString("+0.000;-0.000", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    laps.Add(new LapTimeRowViewModel(completedLapNumber, lastTime, diffPreviousText,
                        racer.arrow == "green" ? "▲" : racer.arrow == "red" ? "▼" : "·",
                        racer.arrow == "green" ? Colors.LimeGreen : racer.arrow == "red" ? Colors.OrangeRed : Colors.Gray));
                }
            }
        }
    }

    public IReadOnlyList<LapTimeRowViewModel> GetLapsForRacer(string fullName)
    {
        return _racerLaps.TryGetValue(fullName, out var laps) ? laps : new List<LapTimeRowViewModel>();
    }
}
