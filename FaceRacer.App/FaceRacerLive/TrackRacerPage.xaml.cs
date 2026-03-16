
using Microsoft.Maui.Controls;
using FaceRacerLive.Services;
using System.Windows.Input;

namespace FaceRacerLive;

[QueryProperty(nameof(CustomTrackByName), "trackByName")]
public partial class TrackRacerPage : ContentPage
{
    private readonly RaceMonitorState _state;
    private bool _syncingLive;
    private bool _syncingAuto;
    private string? _customTrackByName;

    public ICommand BindingContextMiniRacerDoubleTappedCommand { get; }

    public string? CustomTrackByName
    {
        get { return _customTrackByName; }
        set { _customTrackByName = value; }
    }

    public TrackRacerPage()
    {
        InitializeComponent();

        _state = Application.Current?.Handler?.MauiContext?.Services.GetService<RaceMonitorState>()
                 ?? new RaceMonitorState();

        BindingContext = _state.Tracked;

        BindingContextMiniRacerDoubleTappedCommand = new Command<string>(OnMiniRacerDoubleTapped);

        if (BindingContext is System.ComponentModel.INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged += OnTrackedPropertyChanged;
        }
    }

    private void OnMiniRacerDoubleTapped(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        _customTrackByName = fullName;
        _state.Panel.AutoTrackByName = fullName;

        _state.Tracked.UpdateFromSession(_state.Panel.LastSessionData ?? new FaceRacer.Shared.Dto.SessionData(), fullName);
        _state.Tracked.LoadLapsForRacer(fullName);

        MainThread.BeginInvokeOnMainThread(() => LapTimesGraph?.Invalidate());
    }

    private void OnTrackedPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ViewModels.TrackedRacerViewModel.LapTimesGraphDrawable), StringComparison.Ordinal))
        {
            MainThread.BeginInvokeOnMainThread(() => LapTimesGraph?.Invalidate());
        }
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

        // Use custom track name if provided, else use global tracked racer
        var trackedName = _customTrackByName ?? _state.Panel.TrackedRacerFullName;
        _state.Tracked.UpdateFromSession(_state.Panel.LastSessionData ?? new FaceRacer.Shared.Dto.SessionData(), trackedName);

        LapTimesGraph?.Invalidate();
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
