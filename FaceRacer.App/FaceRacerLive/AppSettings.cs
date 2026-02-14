namespace FaceRacerLive
{
    public class AppSettings
    {
        private const string IsSimulationKey = "AppSettings.IsSimulation";
        private const string IntervalMillisecondsLiveMonitorKey = "AppSettings.IntervalMillisecondsLiveMonitor";
        private const string CurrentSessionMonitorUrlKey = "AppSettings.CurrentSessionMonitorUrl";

        public const bool IsSimulationDefault = false;
        public const int IntervalMillisecondsLiveMonitorDefault = 700;
        public const int IntervalMillisecondsLiveMonitorMin = 1;
        public const int IntervalMillisecondsLiveMonitorMax = 10000;

        public const string CurrentSessionMonitorUrlDefault = "http://192.168.10.174/ajax/monitors/current-session-monitor";

        public static bool IsSimulation
        {
            get => Preferences.Default.Get(IsSimulationKey, IsSimulationDefault);
            set => Preferences.Default.Set(IsSimulationKey, value);
        }

        public static int IntervalMillisecondsLiveMonitor
        {
            get => Preferences.Default.Get(IntervalMillisecondsLiveMonitorKey, IntervalMillisecondsLiveMonitorDefault);
            set => Preferences.Default.Set(
                IntervalMillisecondsLiveMonitorKey,
                Math.Clamp(value, IntervalMillisecondsLiveMonitorMin, IntervalMillisecondsLiveMonitorMax));
        }

        public static TimeSpan TimeoutForLiveRequest = TimeSpan.FromSeconds(15);
        public static string DefaultAutoTrackFullName = "Adriano Colombo";

        public static string CurrentSessionMonitorUrl
        {
            get => Preferences.Default.Get(CurrentSessionMonitorUrlKey, CurrentSessionMonitorUrlDefault);
            set => Preferences.Default.Set(CurrentSessionMonitorUrlKey, value ?? string.Empty);
        }
    }
}
