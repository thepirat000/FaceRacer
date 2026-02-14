using FaceRacerLive.ViewModels;

namespace FaceRacerLive;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

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

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_vm.TrySaveToAppSettings())
        {
            return;
        }

		await Shell.Current.GoToAsync("..");
    }
}
