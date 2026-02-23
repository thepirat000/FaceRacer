using FaceRacerLive.ViewModels;
using FaceRacerLive.Monitor;

namespace FaceRacerLive;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;
    private bool _handlingSimulationToggle;

    public SettingsPage()
    {
        InitializeComponent();

        _vm = new SettingsViewModel();
        _vm.LoadFromAppSettings();

        BindingContext = _vm;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        AppSettings.IsSimulation = false;
        AppSettings.IntervalMillisecondsLiveMonitor = AppSettings.IntervalMillisecondsLiveMonitorDefault;
        AppSettings.CurrentSessionMonitorUrl = AppSettings.CurrentSessionMonitorUrlDefault;
        AppSettings.AutoTrackFullName = AppSettings.DefaultAutoTrackFullName;
        AppSettings.MonitorRankingUrl = AppSettings.MonitorRankingUrlDefault;

        _vm.LoadFromAppSettings();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_vm.TrySaveToAppSettings())
        {
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private async void OnSimulationToggled(object? sender, ToggledEventArgs e)
    {
        if (_handlingSimulationToggle)
        {
            return;
        }

        if (!e.Value)
        {
            AppSettings.IsSimulation = false;
            _vm.SimulationZipStatus = null;
            SimulationSessionStore.Clear();
            return;
        }

        AppSettings.IsSimulation = true;

        var existingSessions = SimulationSessionStore.TryGetSessions();
        if (existingSessions is { Count: > 0 })
        {
            _vm.SimulationZipStatus = SimulationSessionStore.TryGetZipFileName() ?? "Simulation zip already loaded.";
            return;
        }

        await PickAndLoadSimulationZipAsync(turnOffSimulationOnCancelOrFailure: true);
    }

    private async void OnSelectSimulationZipClicked(object? sender, EventArgs e)
    {
        if (!_vm.IsSimulation)
        {
            return;
        }

        await PickAndLoadSimulationZipAsync(turnOffSimulationOnCancelOrFailure: false);
    }

    private async Task PickAndLoadSimulationZipAsync(bool turnOffSimulationOnCancelOrFailure)
    {
        if (_handlingSimulationToggle)
        {
            return;
        }

        _handlingSimulationToggle = true;

        try
        {
            _vm.SimulationZipStatus = "Loading zip file...";

            var zipTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/zip" } },
                { DevicePlatform.WinUI, new[] { ".zip" } },
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a simulation race zip",
                FileTypes = zipTypes
            });

            if (result is null)
            {
                _vm.SimulationZipStatus = null;

                if (turnOffSimulationOnCancelOrFailure)
                {
                    SimulationSessionStore.Clear();
                    AppSettings.IsSimulation = false;
                    _vm.IsSimulation = false;
                }

                return;
            }

            await using var stream = await result.OpenReadAsync();
            var sessions = await SampleRaceZipReader.ReadAllSessionsFromZipAsync(stream);

            SimulationSessionStore.SetSessions(sessions, result.FileName);

            _vm.SimulationZipStatus = $"Loaded {sessions.Count} steps from '{result.FileName}'.";

            _vm.TrySaveToAppSettings();
        }
        catch (Exception ex)
        {
            _vm.SimulationZipStatus = $"Failed to load zip: {ex.Message}";

            if (turnOffSimulationOnCancelOrFailure)
            {
                SimulationSessionStore.Clear();
                AppSettings.IsSimulation = false;
                _vm.IsSimulation = false;
            }
        }
        finally
        {
            _handlingSimulationToggle = false;
        }
    }
}