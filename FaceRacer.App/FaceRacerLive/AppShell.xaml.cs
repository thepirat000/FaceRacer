namespace FaceRacerLive
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(BigLandscapeTextPage), typeof(BigLandscapeTextPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(TrackRacerPage), typeof(TrackRacerPage));
            Routing.RegisterRoute(nameof(NextSessionsPage), typeof(NextSessionsPage));
        }

		private async void OnSettingsClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(SettingsPage));
		}

		private async void OnRankingClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;

            var url = AppSettings.MonitorRankingUrl;

            await Launcher.Default.OpenAsync(url);
        }

        private async void OnCurrentClicked(object? sender, EventArgs e)
        {
            FlyoutIsPresented = false;

            var url = AppSettings.MonitorCurrentSessionsDefault;

            await Launcher.Default.OpenAsync(url);
        }

        private async void OnTrackedRacerClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(TrackRacerPage));
		}

        private async void OnNextSessionsClicked(object? sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(NextSessionsPage));
        }

        private void OnExitMenuItemClicked(object sender, EventArgs e)
        {
#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif WINDOWS
            Application.Current.Quit();
#endif
        }
    }
}
