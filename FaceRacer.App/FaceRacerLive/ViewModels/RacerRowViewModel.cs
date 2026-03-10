using System.ComponentModel;
using FaceRacer.Shared.Dto;

#pragma warning disable S3358

namespace FaceRacerLive.ViewModels;

internal sealed class RacerRowViewModel : INotifyPropertyChanged
{
    private SessionRacerData _model;
    private readonly RaceMonitorPanelViewModel _parentModel;

    public RacerRowViewModel(SessionRacerData racerData, RaceMonitorPanelViewModel parentModel)
    {
        _model = racerData;
        _parentModel = parentModel;
    }

    public void UpdateModel(SessionRacerData racerData)
    {
        // Keep the same VM instance so CollectionView can reuse the row visuals.
        // FullName is used as identity (assumed stable/unique per race).
        _model = racerData;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PositionText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kart)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KartColor)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lap)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalLaps)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastTime)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BestTime)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArrowIcon)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArrowColor)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LapProgress)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
    }

    public bool IsHighlighted =>
        !string.IsNullOrWhiteSpace(_parentModel.TrackedRacerFullName) &&
        string.Equals(_model.full_name, _parentModel.TrackedRacerFullName, StringComparison.Ordinal);

    public void RefreshHighlight()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
    }

    public string PositionText => _model.position;
    public string FullName => _model.full_name;
    public string Kart => _model.kart;
    public Color KartColor => Color.TryParse("#" + _model.kart_color, out var c) ? c : Colors.Gray;
    public int Lap => _model.passed;
    public int TotalLaps => _model.total;
    public string LastTime => _model.last_time ?? "-";
    public string BestTime => _model.best_time ?? "-";

    public string ArrowIcon => string.Equals(_model.arrow, "green", StringComparison.OrdinalIgnoreCase) ? "▲" :
        string.Equals(_model.arrow, "red", StringComparison.OrdinalIgnoreCase) ? "▼" : "·";

    public Color ArrowColor => string.Equals(_model.arrow, "green", StringComparison.OrdinalIgnoreCase) ? Colors.LimeGreen :
        string.Equals(_model.arrow, "red", StringComparison.OrdinalIgnoreCase) ? Colors.OrangeRed :
        Colors.Gray;

    public double LapProgress
    {
        get
        {
            var p = double.TryParse(_model.percentage, out var percentage) ? percentage : 0;
            if (double.IsNaN(p) || double.IsInfinity(p))
            {
                return 0;
            }
            p = Math.Clamp(p, 0, 100);
            return p / 100.0;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}