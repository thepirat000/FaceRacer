using FaceRacerLive.Services;

namespace FaceRacerLive;

public partial class TrackRacerPage : ContentPage
{
    private readonly RaceMonitorState _state;
    private bool _syncingLive;
    private bool _syncingAuto;

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

        _syncingAuto = true;
        AutoTrackCheckBox.IsChecked = _state.IsAutoTrackEnabled;
        _syncingAuto = false;

        _state.AutoTrackChanged += OnAutoTrackChangedFromState;

        _state.Tracked.UpdateFromSession(_state.Panel.LastSessionData ?? new FaceRacer.Shared.Dto.SessionData(), _state.Panel.TrackedRacerFullName);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.AutoTrackChanged -= OnAutoTrackChangedFromState;
    }

    private void OnAutoTrackChangedFromState(bool value)
    {
        if (_syncingAuto)
        {
            return;
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _syncingAuto = true;
            AutoTrackCheckBox.IsChecked = value;
            _syncingAuto = false;
        });
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

    private void OnLiveLabelTapped(object? sender, TappedEventArgs e)
    {
        LiveCheckBox.IsChecked = !LiveCheckBox.IsChecked;
    }

    private void OnAutoTrackCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_syncingAuto)
        {
            return;
        }
        _state.IsAutoTrackEnabled = e.Value;
    }

    private void OnAutoTrackLabelTapped(object? sender, TappedEventArgs e)
    {
        AutoTrackCheckBox.IsChecked = !AutoTrackCheckBox.IsChecked;
    }
}
