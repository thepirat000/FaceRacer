using FaceRacerLive.Services;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using System.Web;

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

        _state.IsTrackRacerPageVisible = true;

        _syncingLive = true;
        LiveCheckBox.IsToggled = _state.IsLive;
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
        _state.IsTrackRacerPageVisible = false;
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

    private void OnLiveCheckedChanged(object? sender, ToggledEventArgs e)
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
        LiveCheckBox.IsToggled = !LiveCheckBox.IsToggled;
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

    private async void OnBestTimeCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = BindingContext as ViewModels.TrackedRacerViewModel;
        var raw = vm?.BestTime;
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "-", StringComparison.Ordinal))
        {
            return;
        }

        var text = FormatTimeToOneDecimal(raw);

        if (text is null)
        {
            return;
        }

        var encoded = HttpUtility.UrlEncode(text);
        await Shell.Current.GoToAsync($"{nameof(BigLandscapeTextPage)}?text={encoded}&followBestTime=1");
    }

    private static string? FormatTimeToOneDecimal(string raw)
    {
        var trimmed = raw.Trim();

        if (double.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            var truncated = AppSettings.TruncateForBigTextTime(seconds);
            return truncated.ToString(AppSettings.BigTextTimeFormat);
        }

        return null;
    }
}
