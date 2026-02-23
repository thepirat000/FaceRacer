using FaceRacerLive.Services;

namespace FaceRacerLive;

public partial class TrackRacerPage : ContentPage
{
    private readonly RaceMonitorState _state;

    private bool _syncingLive;

    public TrackRacerPage()
    {
        InitializeComponent();

        _state = Application.Current?.Handler?.MauiContext?.Services.GetService<RaceMonitorState>()
                 ?? new RaceMonitorState();

        BindingContext = _state.Tracked;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _syncingLive = true;
        LiveCheckBox.IsChecked = _state.IsLive;
        _syncingLive = false;

        _state.Tracked.UpdateFromSession(_state.Panel.LastSessionData ?? new FaceRacer.Shared.Dto.SessionData(), _state.Panel.TrackedRacerFullName);
    }

    private void OnLiveCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_syncingLive)
        {
            return;
        }

        _state.IsLive = e.Value;
        _state.LiveChanged?.Invoke(e.Value);
    }
}
