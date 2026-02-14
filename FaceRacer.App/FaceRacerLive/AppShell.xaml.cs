namespace FaceRacerLive
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(BigLandscapeTextPage), typeof(BigLandscapeTextPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }

		private async void OnSettingsClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(SettingsPage));
		}
    }
}
