using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FaceRacerLive.ViewModels;

internal sealed class NextSessionsPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<NextSessionSummaryRowViewModel> Sessions { get; } = new();

    public ObservableCollection<NextSessionDriverRowViewModel> Drivers { get; } = new();

    public ICommand SelectSessionCommand { get; }

    public NextSessionsPageViewModel()
    {
        SelectSessionCommand = new Command<NextSessionSummaryRowViewModel>(SelectSession);
    }

    public string SelectedSessionTitle
    {
        get;
        private set => Set(ref field, value);
    } = "Drivers for —";

    public NextSessionSummaryRowViewModel? SelectedSession
    {
        get;
        private set => Set(ref field, value);
    }

    public void UpdateSessions(List<NextSessionSummaryRowViewModel> sessions)
    {
        var selectedId = SelectedSession?.Id;

        Sessions.Clear();
        foreach (var s in sessions)
        {
            Sessions.Add(s);
        }

        var keepSelected = selectedId.HasValue
            ? Sessions.FirstOrDefault(s => s.Id == selectedId.Value)
            : null;

        SelectSession(keepSelected ?? Sessions.FirstOrDefault());
    }

    private void SelectSession(NextSessionSummaryRowViewModel? session)
    {
        if (session is null)
        {
            SelectedSession = null;
            SelectedSessionTitle = "Drivers for —";
            Drivers.Clear();
            return;
        }

        if (SelectedSession is not null)
        {
            SelectedSession.IsSelected = false;
        }

        SelectedSession = session;
        SelectedSession.IsSelected = true;
        
        SelectedSessionTitle = $"{session.Drivers.Count} drivers for {session.SessionName} at {session.StartsAtText}";

        Drivers.Clear();
        foreach (var d in session.Drivers)
        {
            Drivers.Add(d);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

internal sealed class NextSessionSummaryRowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int? Id { get; init; }

    public string SessionName { get; init; } = "—";

    public string StartsAtText { get; init; } = "—";

    public string StartsInMinutesText { get; init; } = "—";

    public string DriversText { get; init; } = "-";

    public string IndividualText { get; init; } = "-";

    public string DoubleText { get; init; } = "-";

    public List<NextSessionDriverRowViewModel> Drivers { get; init; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public string DetailsText { get; set; }

    private bool _isSelected;
}

internal sealed class NextSessionDriverRowViewModel
{
    public string FullName { get; init; } = "—";

    public string BestTime { get; init; } = "-";

    public string Kart { get; init; } = "-";

    public Color KartColor { get; init; } = Colors.LightGray;
}
