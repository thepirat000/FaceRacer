#if ANDROID
using Android.Content;
using Android.Views.InputMethods;

#endif
using FaceRacerLive.Monitor;
using FaceRacerLive.Services;
using FaceRacerLive.ViewModels;

using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;

using FaceRacer.Shared;
using FaceRacer.Shared.Dto;
// ReSharper disable AsyncVoidLambda

#pragma warning disable S2325

// ReSharper disable AsyncVoidEventHandlerMethod

namespace FaceRacerLive
{
    public partial class MainPage
    {
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly IMicToSpeakerService? _micToSpeaker;
        private bool _isToggling;

        private bool _consoleAutoScroll = true;
        private const double ConsoleBottomEpsilon = 8.0;

        private readonly RaceMonitorState _state;
        private readonly RaceMonitorPanelViewModel _vm;

        private RaceMonitorApi? _raceMonitorApi;

        private SampleRaceSimulator? _raceMonitorSimulator;

        private CancellationTokenSource? _raceLoopCts;

        private string? _snifferRunFolder;
        private bool _snifferExportInProgress;

        public MainPage()
        {
            InitializeComponent();

            var serviceCollection = Application.Current?.Handler?.MauiContext?.Services;

            _state = serviceCollection?.GetService<RaceMonitorState>() ?? new RaceMonitorState();

            _state.LiveChanged = isLive =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LiveRaceCheckBox.IsChecked = isLive;
                });
            };
            _state.AutoTrackChanged = isAuto =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (AutoTrackCheckBox.IsChecked != isAuto)
                    {
                        AutoTrackCheckBox.IsChecked = isAuto;
                    }
                });
            };

            _micToSpeaker = serviceCollection?.GetService<IMicToSpeakerService>();

            if (_micToSpeaker is null)
            {
                MicStatusLabel.Text = "Mic → Speaker not available on this platform.";
                MicToggleBtn.IsEnabled = false;
                PttBtn.IsEnabled = false;
            }

            _vm = _state.Panel;
            BindingContext = _vm;
            AutoTrackCheckBox.IsChecked = _state.IsAutoTrackEnabled;

            MainThread.InvokeOnMainThreadAsync(() =>
            {
                AppendConsole("Face Racer By ThePirat © 2026", Colors.LightGray, true);
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Settings can change while this page is not visible.
            // Refresh tracked racer target (used by RaceMonitorPanelViewModel) when coming back.
            _vm.AutoTrackByName = AppSettings.AutoTrackFullName;

            InitializeRaceMonitorApi();
        }

        private void InitializeRaceMonitorApi()
        {
            if (_raceMonitorSimulator == null)
            {
                _raceMonitorSimulator = new SampleRaceSimulator();
            }

            if (_raceMonitorApi == null || _raceMonitorApi.CurrentSessionMonitorUrl != AppSettings.CurrentSessionMonitorUrl)
            {
                try
                {
                    var services = Application.Current?.Handler?.MauiContext?.Services;

                    var httpClient = services?.GetService<HttpClient>()!;

                    _raceMonitorApi = new RaceMonitorApi(httpClient, AppSettings.CurrentSessionMonitorUrl);

                    AppendConsole($"Race monitor initialized", Colors.LightGreen);
                }
                catch (Exception ex)
                {
                    AppendConsole($"Race monitor init failed: {ex.Message}", Colors.OrangeRed);
                }
            }
        }
        
        private void OnInputTextEditorUnfocused(object? sender, FocusEventArgs e)
        {
            HideKeyboard();
        }

        private static void HideKeyboard()
        {
#if ANDROID
            try
            {
                var activity = Platform.CurrentActivity;
                if (activity is null)
                {
                    return;
                }

                var imm = (InputMethodManager?)activity.GetSystemService(Context.InputMethodService);
                var token = activity.CurrentFocus?.WindowToken ?? activity.Window?.DecorView.WindowToken;

                if (imm is not null && token is not null)
                {
                    imm.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
                    activity.Window?.DecorView.ClearFocus();
                }
            }
            catch
            {
                // ignore (best-effort)
            }
#endif
        }

        #region Audio 

        private async void OnSendTextClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.InputTextEditor.Text))
            {
                return;
            }

            SendTextBtn.IsEnabled = false;

            try
            {
                await SpeakHelper.Speak(this.InputTextEditor.Text);
                AppendConsole(this.InputTextEditor.Text, Colors.LightGray);
                InputTextEditor.Text = "";
            }
            finally
            {
                SendTextBtn.IsEnabled = true;
            }
        }

        private async void OnMicToggleClicked(object? sender, EventArgs e)
        {
            if (_micToSpeaker is null || _isToggling)
                return;

            _isToggling = true;
            MicToggleBtn.IsEnabled = false;
            
            PttBtn.IsEnabled = false;

            try
            {
                if (_micToSpeaker.IsRunning)
                {
                    MicStatusLabel.Text = "Stop";
                    MicToggleBtn.BackgroundColor = Color.FromArgb("#ac99ea");
                    await _micToSpeaker.StopAsync();
                    MicStatusLabel.Text = "Idle";
                }
                else
                {
                    MicStatusLabel.Text = "Start";
                    MicToggleBtn.BackgroundColor = Colors.LightCoral;
                    await _micToSpeaker.StartAsync(CancellationToken.None);
                    MicStatusLabel.Text = "Running";
                }

                UpdateMicButtons();
            }
            catch (Exception ex)
            {
                MicStatusLabel.Text = ex.Message;
                UpdateMicButtons();
            }
            finally
            {
                MicToggleBtn.IsEnabled = true;
                PttBtn.IsEnabled = true;
                _isToggling = false;
            }
        }

        private async void OnPttPressed(object? sender, EventArgs e)
        {
            if (_micToSpeaker is null || _isToggling)
            {
                return;
            }

            // If toggle-mode is already running, do nothing; PTT is "momentary" start/stop.
            if (_micToSpeaker.IsRunning)
            {
                return;
            }

            MicToggleBtn.IsEnabled = false;
            MicStatusLabel.Text = "PTT: Running...";

            try
            {
                await _micToSpeaker.StartAsync(CancellationToken.None);
                UpdateMicButtons();
            }
            catch (Exception ex)
            {
                MicStatusLabel.Text = ex.Message;
                UpdateMicButtons();
                MicToggleBtn.IsEnabled = true;
            }
        }

        private async void OnPttReleased(object? sender, EventArgs e)
        {
            if (_micToSpeaker is null || _isToggling)
            {
                return;
            }

            // Only stop if PTT started it (i.e., we're running and user released).
            if (!_micToSpeaker.IsRunning)
            {
                return;
            }

            MicStatusLabel.Text = "PTT: Stopping...";

            try
            {
                await _micToSpeaker.StopAsync();
                MicStatusLabel.Text = "Idle";
                UpdateMicButtons();
            }
            finally
            {
                MicToggleBtn.IsEnabled = true;
            }
        }

        private void UpdateMicButtons()
        {
            if (_micToSpeaker is null)
            {
                return;
            }

            MicToggleBtn.Text = _micToSpeaker.IsRunning ? "Mic Off" : "Mic On";
            PttBtn.BackgroundColor = _micToSpeaker.IsRunning ? Colors.LightCoral : Color.FromArgb("#ac99ea");
        }

        private async void OnPresetClicked(object? sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string preset } || string.IsNullOrWhiteSpace(preset))
            {
                return;
            }

            await SpeakTextAndClearAsync(preset);
        }

        private async Task SpeakTextAndClearAsync(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            SendTextBtn.IsEnabled = false;

            try
            {
                await SpeakHelper.Speak(text);
                AppendConsole(text, Colors.LightGray);
                InputTextEditor.Text = "";
            }
            finally
            {
                SendTextBtn.IsEnabled = true;
            }
        }

        #endregion

        private void OnConsoleClearClicked(object? sender, EventArgs e)
        {
            ConsoleStack.Clear();
            _consoleAutoScroll = true;
            _vm.ClearRace();
        }

        private void OnConsoleScrolled(object? sender, ScrolledEventArgs e)
        {
            // If user is at (or very near) bottom => keep auto-scroll enabled (newest-at-bottom mode).
            var maxScrollY = Math.Max(0, ConsoleScrollView.ContentSize.Height - ConsoleScrollView.Height);

            _consoleAutoScroll = e.ScrollY >= (maxScrollY - ConsoleBottomEpsilon);
        }

        public void AppendConsole(string message, Color? color = null, bool noDateTime = false)
        {
            color ??= Colors.White;

            var label = new Label
            {
                Text = noDateTime ? message : $"[{DateTime.Now:HH\\:mm\\:ss}]: {message}",
                TextColor = color,
                FontSize = 10,
                LineBreakMode = LineBreakMode.CharacterWrap
            };

            // Newest at bottom
            ConsoleStack.Add(label);

            if (_consoleAutoScroll)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Yield();

                    if (ConsoleStack.Count > 0)
                    {
                        await ConsoleScrollView.ScrollToAsync(ConsoleStack[^1] as Label, ScrollToPosition.End, false);
                    }
                });
            }
        }

        private void OnAutoTrackLabelTapped(object? sender, TappedEventArgs e)
        {
            AutoTrackCheckBox.IsChecked = !AutoTrackCheckBox.IsChecked;
        }

        #region LiveRace

        private void OnLiveLabelTapped(object? sender, TappedEventArgs e)
        {
            LiveRaceCheckBox.IsChecked = !LiveRaceCheckBox.IsChecked;
        }

        private async void OnLiveRaceCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            await Task.Yield();

            _state.IsLive = e.Value;

            if (e.Value)
            {
                if (_raceMonitorApi is null)
                {
                    AppendConsole("Live race: cannot start (race monitor not initialized).", Colors.OrangeRed);
                    LiveRaceCheckBox.IsChecked = false;
                    return;
                }

                StartRaceLoop();
            }
            else
            {
                StopRaceLoop();
            }
        }

        private void StartRaceLoop()
        {
            // Already running
            if (_raceLoopCts is { IsCancellationRequested: false })
            {
                return;
            }

            AppendConsole("Live race: started.", Colors.LightGreen);

            StopRaceLoop(); // cleanup any previous CTS

            _raceLoopCts = new CancellationTokenSource();
            _ = RunRaceLoopAsync(_raceLoopCts.Token);
        }

        private void StopRaceLoop()
        {
            _previousSession = null;

            _raceMonitorSimulator?.ResetSimulation();

            if (_raceLoopCts is null)
            {
                return;
            }

            _raceLoopCts.Cancel();
            _raceLoopCts.Dispose();
            _raceLoopCts = null;

            AppendConsole("Live race: stopped.", Colors.LightGray);
        }

        // Main loop for fetching and updating live race date
        private async Task RunRaceLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var sessions = SimulationSessionStore.TryGetSessions();
                    var hasSimulation = sessions is { Count: > 0 };
                    var session = hasSimulation ? _raceMonitorSimulator!.GetCurrentSession() : await _raceMonitorApi!.GetCurrentSession(ct);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _vm.UpdateFromSession(session!);

                        // Keep tracked racer page state in sync with the same session updates.
                        _state.Tracked.UpdateFromSession(session!, _vm.TrackedRacerFullName);
                    });

                    if (SnifferCheckBox.IsChecked)
                    {
                        await SaveResponse(session!);
                    }

                    if (_state.IsAutoTrackEnabled)
                    {
                        await AutoCommandSession(session!);
                    }
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AppendConsole($"Race monitor error: {ex.Message}", Colors.OrangeRed);
                    });

                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }

                try
                {
                    await Task.Yield();

                    if (AppSettings.IntervalMillisecondsLiveMonitor > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(AppSettings.IntervalMillisecondsLiveMonitor), ct);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        #endregion

        #region Sniffer

        private async void OnSnifferCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            await Task.Yield();

            if (e.Value)
            {
                StartSnifferRun();
            }
            else
            {
                await StopSnifferRunAndExportAsync();
            }
        }

        private void StartSnifferRun()
        {
            var runName = $"fr_rec_{DateTime.Now:yyyyMMdd_HHmmss}";
            _snifferRunFolder = Path.Combine(FileSystem.AppDataDirectory, "Record", runName);

            Directory.CreateDirectory(_snifferRunFolder);

            AppendConsole($"Rec: recording → {_snifferRunFolder}", Colors.LightGreen);
        }
        
        private void OnSnifferLabelTapped(object? sender, TappedEventArgs e)
        {
            SnifferCheckBox.IsChecked = !SnifferCheckBox.IsChecked;
        }

        private async Task SaveResponse(SessionData sessionData)
        {
            var folder = _snifferRunFolder;
            if (string.IsNullOrWhiteSpace(folder) || sessionData.runnin_session_title == null)
            {
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(sessionData, _jsonSerializerOptions);

                var sessionNumber = sessionData.SessionNumber ?? "unknown";
                var fileName = $"session_{sessionNumber}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
                var fullPath = Path.Combine(folder, fileName);

                await File.WriteAllTextAsync(fullPath, json);

                AppendConsole($"Rec: saved state for session {sessionNumber}", Colors.LightGray);
            }
            catch (Exception ex)
            {
                AppendConsole($"Rec save failed: {ex.Message}", Colors.OrangeRed);
            }
        }

        private async Task StopSnifferRunAndExportAsync()
        {
            if (_snifferExportInProgress)
            {
                return;
            }

            var folder = _snifferRunFolder;
            _snifferRunFolder = null;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder) || !Directory.EnumerateFiles(folder).Any())
            {
                AppendConsole("Rec: nothing to export.", Colors.LightGray);
                return;
            }

            _snifferExportInProgress = true;

            try
            {
                var zipPath = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".zip";

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                await ZipFile.CreateFromDirectoryAsync(folder, zipPath, CompressionLevel.Fastest, includeBaseDirectory: false);

                AppendConsole($"Rec: exported {Path.GetFileName(zipPath)}", Colors.LightGreen);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Export record capture",
                    File = new ShareFile(zipPath)
                });

                Directory.Delete(folder, true);

                AppendConsole($"Rec: cleaned up temporary files.", Colors.LightGray);
            }
            catch (Exception ex)
            {
                AppendConsole($"Rec export failed: {ex.Message}", Colors.OrangeRed);
            }
            finally
            {
                _snifferExportInProgress = false;
            }
        }

        #endregion

        #region AutoCommandTrack

        private SessionData? _previousSession;

        private async Task AutoCommandSession(SessionData sessionData)
        {
            if (sessionData.runnin_session_title == null
                || (sessionData.SessionNumber != null && _previousSession?.SessionNumber != null && sessionData.SessionNumber != _previousSession.SessionNumber))
            {
                _previousSession = null;
                return;
            }

            // Commands for the single tracked racer (or none)
            var trackedFullName = _vm.TrackedRacerFullName;

            if (!string.IsNullOrWhiteSpace(trackedFullName))
            {
                var trackedRacer = sessionData.body_data
                    .FirstOrDefault(r => string.Equals(r.full_name, trackedFullName, StringComparison.Ordinal));

                if (trackedRacer is not null)
                {
                    await AutoCommandTrackedRacer(sessionData, trackedRacer);
                    _previousSession = sessionData;
                    return;
                }
            }

            // No tracked racer match => fallback logic (any racer) if/when you implement it
            _previousSession = sessionData;
        }

        private async Task AutoCommandTrackedRacer(SessionData sessionData, SessionRacerData racer)
        {
            var previousRacerData = _previousSession?.body_data.FirstOrDefault(r => r.full_name == racer.full_name);

            if ((racer.last_time != "-" && previousRacerData == null) || (sessionData.SessionNumber == _previousSession?.SessionNumber && previousRacerData?.passed < racer.passed))
            {
                // New lap completed by this racer since last check
                var message = $"Racer {racer.full_name} completed lap {racer.passed} in {racer.last_time} (best: {racer.best_time}).";
                AppendConsole($"Auto: {message}", Colors.LightBlue);
                var isBestLap = racer.best_time != "-" && racer.last_time == racer.best_time;
                var command = $"{racer.last_time.Replace(".", ":")}; {(racer.passed == racer.total ? "última" : "")} {(isBestLap ? "mejor vuelta." : "")} vuelta {racer.passed}";
                InputTextEditor.Text = command;
                await SpeakTextAndClearAsync(command);
            }

            if ((racer.position != "-" && previousRacerData == null) || (sessionData.SessionNumber == _previousSession?.SessionNumber && previousRacerData?.position != racer.position))
            {
                // Position change for this racer since last check
                if (int.TryParse(racer.position, out var currentPosition))
                {
                    var previousPosition = int.TryParse(previousRacerData?.position, out var positionInt) ? positionInt : 0;
                    var positionChange = currentPosition - previousPosition;
                    var direction = positionChange < 0 || previousPosition == 0 ? "up" : "down";
                    var message = $"Racer {racer.full_name} moved {direction} to position #{currentPosition}.";
                    AppendConsole($"Auto: {message}", Colors.LightBlue);
                    var command = $"Posición {currentPosition}";
                    InputTextEditor.Text = command;
                    await SpeakTextAndClearAsync(command);
                }
            }
        }

        #endregion

        private async void OnTrackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(TrackRacerPage));
        }

        private async void OnShowTextClicked(object sender, EventArgs e)
        {
            string? text;
            if (SendTextBtn.IsEnabled && !string.IsNullOrWhiteSpace(InputTextEditor.Text))
            {
                text = InputTextEditor.Text;
                InputTextEditor.Text = "";
            }
            else
            {
                var bestTime = GetCurrentTrackedRacerBestTimeText();
                text = bestTime?.Length >= 4 ? bestTime[0..4] : null;
            }

            if (text == null)
            {
                return;
            }

            var encoded = HttpUtility.UrlEncode(text);
            await Shell.Current.GoToAsync($"{nameof(BigLandscapeTextPage)}?text={encoded}");

        }

        private string? GetCurrentTrackedRacerBestTimeText() => _vm.Racers.FirstOrDefault(r => r.IsHighlighted)?.BestTime;

        private void OnAutoTrackCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_state.IsAutoTrackEnabled != e.Value)
            {
                _state.IsAutoTrackEnabled = e.Value;
            }
        }
    }
}