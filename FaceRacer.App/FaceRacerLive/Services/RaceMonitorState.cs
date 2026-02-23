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
}
