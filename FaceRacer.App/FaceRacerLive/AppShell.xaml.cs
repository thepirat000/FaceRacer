namespace FaceRacerLive
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(BigLandscapeTextPage), typeof(BigLandscapeTextPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(MonitorRankingPage), typeof(MonitorRankingPage));
        }

		private async void OnSettingsClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(SettingsPage));
		}

		private async void OnRankingClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(MonitorRankingPage));
		}
    }
}
