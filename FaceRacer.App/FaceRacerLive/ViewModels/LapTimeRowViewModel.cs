using System.ComponentModel;

namespace FaceRacerLive.ViewModels;

internal sealed class LapTimeRowViewModel : INotifyPropertyChanged
{
    public LapTimeRowViewModel(int lapNumber, string time, string arrowIcon, Color arrowColor)
    {
        LapNumber = lapNumber;
        Time = time;
        ArrowIcon = arrowIcon;
        ArrowColor = arrowColor;
    }

    public int LapNumber { get; }
    public string Time { get; }

    public string ArrowIcon { get; }
    public Color ArrowColor { get; }

    public bool IsFastest
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFastest)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
