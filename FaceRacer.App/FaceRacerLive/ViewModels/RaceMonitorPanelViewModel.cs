using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FaceRacer.Shared.Dto;

namespace FaceRacerLive.ViewModels
{
    internal sealed class RaceMonitorPanelViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RacerRowViewModel> Racers { get; } = new();

        private string _autoTrackByName = AppSettings.AutoTrackFullName;
        public string AutoTrackByName
        {
            get => _autoTrackByName;
            set
            {
                if (!Set(ref _autoTrackByName, value))
                {
                    return;
                }

                RefreshTrackedRacerAndUi();
            }
        }

        public string? TrackedRacerFullName
        {
            get;
            private set => Set(ref field, value);
        }

        private string _sessionTitle = "Session #—";
        public string SessionTitle
        {
            get => _sessionTitle;
            set => Set(ref _sessionTitle, value);
        }

        private string _kartsText = "Karts: —";
        public string KartsText
        {
            get => _kartsText;
            set => Set(ref _kartsText, value);
        }

        private string _sessionStatusText = "N/A";
        public string SessionStatusText
        {
            get => _sessionStatusText;
            set => Set(ref _sessionStatusText, value);
        }

        public Color SessionStatusColor
        {
            get;
            set => Set(ref field, value);
        } = Colors.Gray;

        public double SessionProgress
        {
            get;
            set => Set(ref field, value);
        } = 0f;

        public Color SessionProgressColor
        {
            get;
            set => Set(ref field, value);
        } = Colors.LimeGreen;

        public void ClearRace()
        {
            Racers.Clear();

            TrackedRacerFullName = null;

            SessionTitle = "Session #—";
            KartsText = "Karts: —";
            SessionStatusText = "N/A";
            SessionStatusColor = Colors.Gray;
            SessionProgress = 0f;
        }

        public void UpdateFromSession(SessionData sessionData)
        {
            var racerCount = sessionData.body_data?.Count ?? 0;

            KartsText = $"Karts: {racerCount}";

            var completed = sessionData.show_checkered_flag;
            SessionStatusText = completed ? "Completed" : racerCount == 0 ? "N/A" : "Racing";
            SessionStatusColor = completed ? Colors.Gold : racerCount == 0 ? Colors.Gray : Colors.LimeGreen;

            var sessionProgress = Math.Clamp(sessionData.current_high_lap.GetValueOrDefault() / (double)sessionData.total_laps_for_all.GetValueOrDefault(sessionData.current_high_lap.GetValueOrDefault(1)), 0, 100);

            SessionProgress = sessionProgress;
            SessionProgressColor = sessionProgress < 0.5 ? Colors.LimeGreen : sessionProgress < 0.85 ? Colors.Gold : Colors.Red;

            if (racerCount == 0)
            {
                // Do not clear the list until next race
                return;
            }

            SessionTitle = "Session #" + (sessionData!.SessionNumber ?? "—");

            // Baseline ordering (defines what "first match" means)
            var byPosition = (sessionData.body_data ?? new List<SessionRacerData>())
                .OrderBy(r =>
                {
                    var orderBy = int.TryParse(r.position, out var pos) ? pos : 99;
                    return orderBy;
                })
                .ThenBy(r => r.full_name)
                .ToList();

            Racers.Clear();
            foreach (var r in byPosition)
            {
                Racers.Add(new RacerRowViewModel(r, this));
            }

            // Pick ONE tracked match (or none), and refresh highlight flags
            RefreshTrackedRacerAndUi();

            // Move tracked racer to the top (only that one), keep the rest by position
            if (!string.IsNullOrWhiteSpace(TrackedRacerFullName))
            {
                var sorted = Racers
                    .OrderByDescending(r => string.Equals(r.FullName, TrackedRacerFullName, StringComparison.Ordinal))
                    .ThenBy(r => int.TryParse(r.PositionText.TrimStart('#'), out var pos) ? pos : int.MaxValue)
                    .ToList();

                for (var i = 0; i < sorted.Count; i++)
                {
                    var item = sorted[i];
                    var currentIndex = Racers.IndexOf(item);
                    if (currentIndex != i && currentIndex >= 0)
                    {
                        Racers.Move(currentIndex, i);
                    }
                }
            }
        }

        private void RefreshTrackedRacerAndUi()
        {
            TrackedRacerFullName = FindFirstMatchFullName(AutoTrackByName);

            foreach (var r in Racers)
            {
                r.RefreshHighlight();
            }
        }

        private string? FindFirstMatchFullName(string? needle)
        {
            if (string.IsNullOrWhiteSpace(needle))
            {
                return null; // "none" when empty (change to return first racer if you want a default)
            }

            foreach (var r in Racers)
            {
                if (r.FullName.Contains(needle, StringComparison.OrdinalIgnoreCase))
                {
                    return r.FullName;
                }
            }

            return null;
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
}