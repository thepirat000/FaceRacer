namespace FaceRacerLive
{
    public class AppSettings
    {
        private const string IntervalMillisecondsLiveMonitorKey = "AppSettings.IntervalMillisecondsLiveMonitor";
        private const string CurrentSessionMonitorUrlKey = "AppSettings.CurrentSessionMonitorUrl";
        private const string AutoTrackFullNameKey = "AppSettings.AutoTrackFullName";
        private const string MonitorRankingUrlKey = "AppSettings.MonitorRankingUrl";

        public const int IntervalMillisecondsLiveMonitorDefault = 700;
        public const int IntervalMillisecondsLiveMonitorMin = 1;
        public const int IntervalMillisecondsLiveMonitorMax = 10000;

        public const string CurrentSessionMonitorUrlDefault = "http://192.168.10.174/ajax/monitors/current-session-monitor";
        public const string MonitorRankingUrlDefault = "http://192.168.10.174/es/monitors/monitor-ranking";
        public const string MonitorCurrentSessionsDefault = "http://192.168.10.174/es/monitors/monitor-current-sessions";

        private static string _bigTextTimeFormat = "0.0";

        public static string BigTextTimeFormat
        {
            get => _bigTextTimeFormat;
            set => _bigTextTimeFormat = value;
        }

        public static int BigTextTimeDecimals => GetDecimalsFromFormat(BigTextTimeFormat);

        public static double TruncateForBigTextTime(double value)
        {
            var decimals = BigTextTimeDecimals;
            var factor = Math.Pow(10, decimals);
            return Math.Truncate(value * factor) / factor;
        }

        private static int GetDecimalsFromFormat(string? format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return 1;
            }

            var dot = format.IndexOf('.', StringComparison.Ordinal);
            var decimals = dot >= 0 ? format.Length - dot - 1 : 0;
            return Math.Clamp(decimals, 1, 3);
        }

        private static bool _isSimulationRuntime;

        public static bool IsSimulation
        {
            get => _isSimulationRuntime;
            set => _isSimulationRuntime = value;
        }

        private static bool _moveTrackedRacerToTheTop = false;

        public static bool MoveTrackedRacerToTheTop
        {
            get => _moveTrackedRacerToTheTop;
            set => _moveTrackedRacerToTheTop = value;
        }

        public static int IntervalMillisecondsLiveMonitor
        {
            get => Preferences.Default.Get(IntervalMillisecondsLiveMonitorKey, IntervalMillisecondsLiveMonitorDefault);
            set => Preferences.Default.Set(
                IntervalMillisecondsLiveMonitorKey,
                Math.Clamp(value, IntervalMillisecondsLiveMonitorMin, IntervalMillisecondsLiveMonitorMax));
        }

        public static TimeSpan TimeoutForLiveRequest = TimeSpan.FromSeconds(120);
        public static string DefaultAutoTrackFullName = "Adriano Colombo";

        public static string AutoTrackFullName
        {
            get => Preferences.Default.Get(AutoTrackFullNameKey, DefaultAutoTrackFullName);
            set => Preferences.Default.Set(AutoTrackFullNameKey, (value ?? string.Empty).Trim());
        }

        public static string CurrentSessionMonitorUrl
        {
            get => Preferences.Default.Get(CurrentSessionMonitorUrlKey, CurrentSessionMonitorUrlDefault);
            set => Preferences.Default.Set(CurrentSessionMonitorUrlKey, value ?? string.Empty);
        }

        public static string MonitorRankingUrl
        {
            get => Preferences.Default.Get(MonitorRankingUrlKey, MonitorRankingUrlDefault);
            set => Preferences.Default.Set(MonitorRankingUrlKey, value ?? string.Empty);
        }
    }
}
