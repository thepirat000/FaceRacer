using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FaceRacer.Shared.Dto;

namespace FaceRacerLive.ViewModels
{
    internal sealed class RaceMonitorPanelViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RacerRowViewModel> Racers { get; } = new();

        private readonly Dictionary<string, RacerRowViewModel> _racerByFullName = new(StringComparer.Ordinal);

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

            _racerByFullName.Clear();

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

            // Incremental update to minimize UI churn (prevents blinking on Windows, reduces work on Android)
            ApplyIncrementalRacerUpdate(byPosition);

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

        private void ApplyIncrementalRacerUpdate(List<SessionRacerData> byPosition)
        {
            // Add/update in dictionary
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var racerData in byPosition)
            {
                var fullName = racerData.full_name;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                seen.Add(fullName);

                if (_racerByFullName.TryGetValue(fullName, out var existingVm))
                {
                    existingVm.UpdateModel(racerData);
                    continue;
                }

                var vm = new RacerRowViewModel(racerData, this);
                _racerByFullName[fullName] = vm;
                Racers.Add(vm);
            }

            // Remove racers that are no longer present
            for (var i = Racers.Count - 1; i >= 0; i--)
            {
                var vm = Racers[i];
                if (!seen.Contains(vm.FullName))
                {
                    Racers.RemoveAt(i);
                    _racerByFullName.Remove(vm.FullName);
                }
            }

            // Reorder to match byPosition (tracked racer will be re-pinned afterwards)
            var expectedCount = Math.Min(byPosition.Count, Racers.Count);
            var orderMatches = true;
            for (var i = 0; i < expectedCount; i++)
            {
                var expectedName = byPosition[i].full_name;
                if (!string.Equals(Racers[i].FullName, expectedName, StringComparison.Ordinal))
                {
                    orderMatches = false;
                    break;
                }
            }

            if (orderMatches && Racers.Count == byPosition.Count)
            {
                return;
            }

            // Build index map once, then keep it updated as we move items.
            var indexByName = new Dictionary<string, int>(Racers.Count, StringComparer.Ordinal);
            for (var i = 0; i < Racers.Count; i++)
            {
                indexByName[Racers[i].FullName] = i;
            }

            for (var targetIndex = 0; targetIndex < byPosition.Count && targetIndex < Racers.Count; targetIndex++)
            {
                var fullName = byPosition[targetIndex].full_name;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                if (!indexByName.TryGetValue(fullName, out var currentIndex))
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    continue;
                }

                var item = Racers[currentIndex];
                Racers.Move(currentIndex, targetIndex);

                // Update the shifted range in the index map.
                if (currentIndex > targetIndex)
                {
                    for (var i = targetIndex; i <= currentIndex; i++)
                    {
                        indexByName[Racers[i].FullName] = i;
                    }
                }
                else
                {
                    for (var i = currentIndex; i <= targetIndex; i++)
                    {
                        indexByName[Racers[i].FullName] = i;
                    }
                }

                // Ensure moved item points to its new index
                indexByName[item.FullName] = targetIndex;
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