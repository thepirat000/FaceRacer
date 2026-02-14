using System.Text.Json.Serialization;

namespace FaceRacerLive.Dto
{
    public class SessionData
    {
        /// <summary>
        /// Session title/identifier displayed to users (e.g., "Sesión #129").
        /// </summary>
        public string? runnin_session_title { get; set; }

        [JsonIgnore]
        public string? SessionNumber => string.IsNullOrEmpty(runnin_session_title) ? null : runnin_session_title[(runnin_session_title.IndexOf('#') + 1)..];

        /// <summary>
        /// List of racers/rows to render in the current monitor table.
        /// </summary>
        public List<SessionRacerData> body_data { get; set; }

        /// <summary>
        /// When true, the client should display a checkered-flag indicator for the session (session finished).
        /// </summary>
        public bool show_checkered_flag { get; set; }

        /// <summary>
        /// Footer HTML content (server-rendered) displayed in the monitor footer.
        /// </summary>
        public string footer_line { get; set; }

        /// <summary>
        /// Ranking mode used by the server for ordering and gaps (e.g., "time").
        /// </summary>
        public string session_ranking { get; set; }

        /// <summary>
        /// Highest lap count reached by any racer in the session (used for "laps to go"/overall progress displays).
        /// </summary>
        public int? current_high_lap { get; set; }

        /// <summary>
        /// Total laps for the session shared by all racers (when lap-based sessions are used).
        /// </summary>
        public int? total_laps_for_all { get; set; }
    }
}
