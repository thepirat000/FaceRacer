using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace FaceRacer.Shared.Dto;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class SessionRacerData
{
    /// <summary>
    /// Racer position in the current session ranking (1 = leader).
    /// </summary>
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string position { get; set; }

    /// <summary>
    /// Display name of the racer (may be abbreviated depending on server settings/UI).
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// Kart identifier/number as displayed in the monitor UI.
    /// </summary>
    public string? kart { get; set; }

    /// <summary>
    /// Kart color in hex without the leading '#', used for UI coloring (e.g. "99242c").
    /// </summary>
    public string kart_color { get; set; }

    /// <summary>
    /// Current lap count completed (or current lap number shown for the racer).
    /// </summary>
    public int passed { get; set; }

    /// <summary>
    /// Total laps for this racer/session (when lap-based sessions are used).
    /// </summary>
    public int total { get; set; }

    /// <summary>
    /// Last lap time formatted as seconds.milliseconds (e.g. "34.804") or "-" when not available.
    /// </summary>
    public string? last_time { get; set; }

    /// <summary>
    /// UI hint for a performance trend arrow. Typically "red" when <see cref="last_time"/> is slower than <see cref="best_time"/>,
    /// and "green" when the last lap matches or improves the best (server-defined rules).
    /// </summary>
    public string arrow { get; set; }

    /// <summary>
    /// Best lap time formatted as seconds.milliseconds (e.g. "33.822") or "-" when not available.
    /// </summary>
    public string? best_time { get; set; }

    /// <summary>
    /// Elapsed time (milliseconds) since the racer last crossed the start/finish line (time into the current lap).
    /// </summary>
    public int? passed_time { get; set; }

    /// <summary>
    /// Current lap progress as percentage (0..100+), derived from <see cref="passed_time"/> relative to an estimated lap duration.
    /// Can exceed 100 when the lap is taking longer than the estimate.
    /// </summary>
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string percentage { get; set; }

    /// <summary>
    /// Average lap time for the racer in the session, in milliseconds.
    /// </summary>
    public int? avg_lap { get; set; }

    /// <summary>
    /// Lap-time consistency metric in milliseconds (lower typically means more consistent laps).
    /// </summary>
    public int? consistency_lap { get; set; }

    /// <summary>
    /// Number of pit events/stops recorded for the racer (used by certain monitor layouts).
    /// </summary>
    public int? number_of_pits { get; set; }

    /// <summary>
    /// Checkered-flag mode start timestamp/marker for the racer, or null when not in that mode.
    /// </summary>
    public string checkered_flag_mode_at { get; set; }

    /// <summary>
    /// Whether this racer is currently considered under checkered-flag state (finished).
    /// </summary>
    public bool checkered_flag { get; set; }

    /// <summary>
    /// Indicates a racer-specific front flag state (if any); null when not present.
    /// </summary>
    public string front_flag { get; set; }

    /// <summary>
    /// Progress bar animation duration (milliseconds) used by the monitor UI to animate the bar from
    /// <see cref="line_percentage"/> to 100%.
    /// </summary>
    public int? line_guess { get; set; }

    /// <summary>
    /// Progress bar starting percentage (0..100+). This value is applied as CSS width before animating to 100%.
    /// Typically matches <see cref="percentage"/>.
    /// </summary>
    public double? line_percentage { get; set; }

    /// <summary>
    /// Full racer name (when the server indicates <c>should_use_full_name</c>).
    /// </summary>
    public string? full_name { get; set; }

    /// <summary>
    /// Difference between <see cref="last_time"/> and <see cref="best_time"/> for the racer, in seconds (string, typically with 3 decimals).
    /// The server may return "-" when the difference is zero or not applicable.
    /// Example: last_time "34.804" and best_time "33.822" => diff "0.982".
    /// </summary>
    public string diff { get; set; }
}