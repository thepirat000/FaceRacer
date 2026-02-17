using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FaceRacerLive.ViewModels;

internal sealed class SettingsViewModel : INotifyPropertyChanged
{
    private bool _isSimulation;
    public bool IsSimulation
    {
        get => _isSimulation;
        set => Set(ref _isSimulation, value);
    }

    private string _intervalMillisecondsLiveMonitorText = string.Empty;
    public string IntervalMillisecondsLiveMonitorText
    {
        get => _intervalMillisecondsLiveMonitorText;
        set => Set(ref _intervalMillisecondsLiveMonitorText, value);
    }

    private string _currentSessionMonitorUrl = string.Empty;
    public string CurrentSessionMonitorUrl
    {
        get => _currentSessionMonitorUrl;
        set => Set(ref _currentSessionMonitorUrl, value);
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

    public void LoadFromAppSettings()
    {
        IsSimulation = AppSettings.IsSimulation;
        IntervalMillisecondsLiveMonitorText = AppSettings.IntervalMillisecondsLiveMonitor.ToString();
        CurrentSessionMonitorUrl = AppSettings.CurrentSessionMonitorUrl;
        AutoTrackFullName = AppSettings.AutoTrackFullName;
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
        AppSettings.IntervalMillisecondsLiveMonitor = ms;
        AppSettings.CurrentSessionMonitorUrl = (CurrentSessionMonitorUrl ?? string.Empty).Trim();
        AppSettings.AutoTrackFullName = (AutoTrackFullName ?? string.Empty).Trim();

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
