using FaceRacerLive.ViewModels;

namespace FaceRacerLive.Services;

internal sealed class RaceMonitorState
{
    public RaceMonitorPanelViewModel Panel { get; } = new();
    public TrackedRacerViewModel Tracked { get; } = new();

    public bool IsLive
    {
        get;
        set;
    }

    public Action<bool>? LiveChanged
    {
        get;
        set;
    }
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
}
