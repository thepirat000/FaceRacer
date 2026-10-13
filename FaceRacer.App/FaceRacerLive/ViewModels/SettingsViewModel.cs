using System.ComponentModel;
using System.Runtime.CompilerServices;
using FaceRacerLive.Services;

namespace FaceRacerLive.ViewModels;

internal sealed class SettingsViewModel : INotifyPropertyChanged
{
    private bool _isSimulation;
    public bool IsSimulation
    {
        get => _isSimulation;
        set => Set(ref _isSimulation, value);
    }

    private bool _moveTrackedRacerToTheTop;
    public bool MoveTrackedRacerToTheTop
    {
        get => _moveTrackedRacerToTheTop;
        set => Set(ref _moveTrackedRacerToTheTop, value);
    }

    private string _bigTextTimeFormat = "0.0";
    public string BigTextTimeFormat
    {
        get => _bigTextTimeFormat;
        set => Set(ref _bigTextTimeFormat, value);
    }

    private string _intervalMillisecondsLiveMonitorText = string.Empty;
    public string IntervalMillisecondsLiveMonitorText
    {
        get => _intervalMillisecondsLiveMonitorText;
        set => Set(ref _intervalMillisecondsLiveMonitorText, value);
    }

    private string _serverBaseUrl = string.Empty;
    public string ServerBaseUrl
    {
        get => _serverBaseUrl;
        set => Set(ref _serverBaseUrl, value);
    }

    private string _autoTrackFullName = string.Empty;
    public string AutoTrackFullName
    {
        get => _autoTrackFullName;
        set => Set(ref _autoTrackFullName, value);
    }

    private string? _validationError;
    public string? ValidationError
    {
        get => _validationError;
        private set => Set(ref _validationError, value);
    }

    private string? _simulationZipStatus;
    public string? SimulationZipStatus
    {
        get => _simulationZipStatus;
        set => Set(ref _simulationZipStatus, value);
    }

    public void LoadFromAppSettings()
    {
        IsSimulation = AppSettings.IsSimulation;
        MoveTrackedRacerToTheTop = AppSettings.MoveTrackedRacerToTheTop;
        IntervalMillisecondsLiveMonitorText = AppSettings.IntervalMillisecondsLiveMonitor.ToString();
        BigTextTimeFormat = AppSettings.BigTextTimeFormat;
        ServerBaseUrl = AppSettings.ServerBaseUrl;
        AutoTrackFullName = AppSettings.AutoTrackFullName;
        SimulationZipStatus = IsSimulation
            ? FaceRacerLive.Monitor.SimulationSessionStore.TryGetZipFileName()
            : null;
        ValidationError = null;
    }

    public bool TrySaveToAppSettings()
    {
        ValidationError = null;

        if (!int.TryParse(IntervalMillisecondsLiveMonitorText?.Trim(), out var ms))
        {
            ValidationError = "Interval must be a number.";
            return false;
        }

        if (ms < AppSettings.IntervalMillisecondsLiveMonitorMin || ms > AppSettings.IntervalMillisecondsLiveMonitorMax)
        {
            ValidationError = $"Interval must be between {AppSettings.IntervalMillisecondsLiveMonitorMin} and {AppSettings.IntervalMillisecondsLiveMonitorMax} ms.";
            return false;
        }

        AppSettings.IsSimulation = IsSimulation;
        AppSettings.MoveTrackedRacerToTheTop = MoveTrackedRacerToTheTop;
        AppSettings.IntervalMillisecondsLiveMonitor = ms;
        AppSettings.BigTextTimeFormat = BigTextTimeFormat?.Trim() ?? "0.0";
        AppSettings.ServerBaseUrl = (ServerBaseUrl ?? string.Empty).Trim();
        AppSettings.AutoTrackFullName = (AutoTrackFullName ?? string.Empty).Trim();

        // Update the shared RaceMonitorPanelViewModel
        var state = Application.Current!.Handler!.MauiContext!.Services.GetService<RaceMonitorState>();
        state!.Panel.AutoTrackByName = AppSettings.AutoTrackFullName;

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
