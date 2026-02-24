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
            Routing.RegisterRoute(nameof(TrackRacerPage), typeof(TrackRacerPage));
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

		private async void OnTrackedRacerClicked(object? sender, EventArgs e)
		{
			FlyoutIsPresented = false;
			await GoToAsync(nameof(TrackRacerPage));
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
