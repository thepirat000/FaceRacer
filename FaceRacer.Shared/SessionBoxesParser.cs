using HtmlAgilityPack;

using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack.CssSelectors.NetCore;
using FaceRacer.Shared.Dto;

namespace FaceRacer.Shared;

internal static class SessionBoxesParser
{
    private static readonly Regex ClockRegex = new Regex(@"\d{2}:\d{2}");
    private static readonly Regex ProfileNameMatchRegex = new Regex(@"/profile/([^/?#]+)");
    private static readonly Regex LapNumberRegex = new Regex(@"\d+");

    public static List<SessionInfo> Parse(string html, string? sessionUrlFormat, bool includeLapDetails)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return new List<SessionInfo>();
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var username = ExtractUsername(doc);

        var sessionNodes = doc.DocumentNode.QuerySelectorAll("div.session-result-container");

        return sessionNodes
            .Select(sessionNode =>
            {
                var userId = int.Parse(sessionNode.GetAttributeValue("data-user-id", "0").Trim());

                var sessionId = sessionNode.GetAttributeValue("data-session-uuid", "").Trim();

                var bestTime = sessionNode
                    .QuerySelector(".session-result-visible-section .minified-stat.time .minified-stat-value")
                    ?.InnerText.Trim() ?? "";

                var dateText = sessionNode
                    .QuerySelector(".session-result-visible-section .minified-stat.date span.date")
                    ?.InnerText.Trim() ?? "";

                var date = string.IsNullOrEmpty(dateText) ? DateOnly.MinValue : DateOnly.ParseExact(dateText, "dd.MM.yyyy", CultureInfo.InvariantCulture);

                var userFullName = sessionNode
                    .QuerySelector(".session-result-visible-section .minified-content .minified-name")
                    ?.InnerText.Trim() ?? "";

                var clockText = sessionNode
                    .QuerySelector(".session-result-visible-section .minified-stat.date span.clock")
                    ?.InnerText.Trim() ?? "";

                if (!string.IsNullOrEmpty(clockText))
                {
                    // Keep only the time part (HH:mm) using ClockRegex
                    var match = ClockRegex.Match(clockText);
                    if (match.Success)
                    {
                        clockText = match.Value;
                    }
                }

                var resultPos = sessionNode
                    .QuerySelector(".session-result-visible-section .top .position.inline")
                    ?.InnerText.Trim().Replace(" ", "") ?? "";

                if (resultPos == "SingleSession")
                {
                    resultPos = "#1/1";
                }

                var resultPosNumber = resultPos.StartsWith("#") && resultPos.Contains("/") ? int.Parse(resultPos.Substring(1).Split('/')[0]) : int.MaxValue;
                var racerCount = resultPos.Contains("/") ? int.Parse(resultPos.Split('/')[1]) : 0;

                var sessionUrl = sessionUrlFormat == null ? null : string.Format(sessionUrlFormat, username, sessionId);
                var lapDetails = includeLapDetails ? ExtractLapDetails(sessionNode) : null;

                return new SessionInfo(
                    SessionId: sessionId,
                    BestTime: bestTime,
                    Date: date,
                    ClockText: clockText,
                    ResultPositionText: resultPos,
                    Position: resultPosNumber,
                    RacerCount: racerCount,
                    SessionUrl: sessionUrl,
                    UserId: userId,
                    Username: username,
                    UserFullName: userFullName,
                    LapDetails: lapDetails
                );
            })
            .Where(s => !string.IsNullOrWhiteSpace(s.SessionId))
            .ToList();
    }

    private static List<SessionLapInfo> ExtractLapDetails(HtmlNode sessionNode)
    {
        return sessionNode
            .QuerySelectorAll(".tab_laps .table_content.session_content .row")
            .Select(row =>
            {
                var lapName = row.QuerySelector(".lap-name")?.InnerText.Trim() ?? string.Empty;
                var lapTimeNode = row.QuerySelector("a.time_laps.first");

                if (string.IsNullOrWhiteSpace(lapName) || lapTimeNode == null)
                {
                    return null;
                }

                var lapNumber = ParseLapNumber(
                    row.QuerySelector(".position")?.InnerText.Trim() ?? string.Empty,
                    lapName);

                return new SessionLapInfo(Lap: lapNumber, Time: lapTimeNode.InnerText.Trim());
            })
            .Where(lap => lap != null)
            .Cast<SessionLapInfo>()
            .ToList();
    }

    private static int ParseLapNumber(string positionText, string lapName)
    {
        if (int.TryParse(positionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lapNumber))
        {
            return lapNumber;
        }

        var match = LapNumberRegex.Match(lapName);
        if (match.Success && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lapNumber))
        {
            return lapNumber;
        }

        return 0;
    }

    // TODO: Review this, it's extracting the wrong username sometimes
    private static string? ExtractUsername(HtmlDocument doc)
    {
        // Expected: <a href="https://www.racefacer.com/en/profile/{USER_NAME}" class="first">
        var href = doc.DocumentNode
            .QuerySelector("a.first[href*='/profile/']")
            ?.GetAttributeValue("href", null);

        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        var m = ProfileNameMatchRegex.Match(href);
        return m.Success ? m.Groups[1].Value : null;
    }
}