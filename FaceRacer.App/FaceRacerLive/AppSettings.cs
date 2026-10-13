namespace FaceRacerLive
{
    public class AppSettings
    {
        private const string IntervalMillisecondsLiveMonitorKey = "AppSettings.IntervalMillisecondsLiveMonitor";
        private const string AutoTrackFullNameKey = "AppSettings.AutoTrackFullName";
        private const string ServerBaseUrlKey = "AppSettings.ServerBaseUrl";

        private const string CurrentSessionMonitorPath = "/ajax/monitors/current-session-monitor";
        private const string NextSessionsMonitorPath = "/ajax/monitors/next-sessions";
        private const string MonitorRankingPath = "/es/monitors/monitor-ranking";
        private const string MonitorCurrentSessionsPath = "/es/monitors/monitor-current-sessions";

        public const int IntervalMillisecondsLiveMonitorDefault = 700;
        public const int IntervalMillisecondsLiveMonitorMin = 0;
        public const int IntervalMillisecondsLiveMonitorMax = 10000;

        public const string ServerBaseUrlDefault = "http://192.168.10.174";

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

        public static TimeSpan TimeoutForLiveRequest = TimeSpan.FromSeconds(30);

        public static string DefaultAutoTrackFullName = "Adriano Colombo";

        public static string AutoTrackFullName
        {
            get => Preferences.Default.Get(AutoTrackFullNameKey, DefaultAutoTrackFullName);
            set => Preferences.Default.Set(AutoTrackFullNameKey, (value ?? string.Empty).Trim());
        }

        public static string ServerBaseUrl
        {
            get => NormalizeBaseUrl(Preferences.Default.Get(ServerBaseUrlKey, ServerBaseUrlDefault));
            set => Preferences.Default.Set(ServerBaseUrlKey, NormalizeBaseUrl(value));
        }

        public static string CurrentSessionMonitorUrl
        {
            get => BuildUrl(ServerBaseUrl, CurrentSessionMonitorPath);
            set => ServerBaseUrl = value;
        }

        public static string NextSessionsMonitorUrl
        {
            get => BuildUrl(ServerBaseUrl, NextSessionsMonitorPath);
            set => ServerBaseUrl = value;
        }

        public static string MonitorRankingUrl
        {
            get => BuildUrl(ServerBaseUrl, MonitorRankingPath);
            set => ServerBaseUrl = value;
        }

        public static string MonitorCurrentSessionsUrl => BuildUrl(ServerBaseUrl, MonitorCurrentSessionsPath);

        private static string NormalizeBaseUrl(string? serverBaseUrl)
        {
            var trimmed = (serverBaseUrl ?? string.Empty).Trim();

            if (trimmed.Length == 0)
            {
                return ServerBaseUrlDefault;
            }

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            return trimmed.TrimEnd('/');
        }

        private static string BuildUrl(string serverBaseUrl, string relativePath)
        {
            var baseUrl = NormalizeBaseUrl(serverBaseUrl);
            var path = relativePath.TrimStart('/');
            return $"{baseUrl}/{path}";
        }

    }
}
