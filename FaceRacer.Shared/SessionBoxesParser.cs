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

    public static List<SessionInfo> Parse(string html, string? sessionUrlFormat)
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

                var sessionUrl = sessionUrlFormat == null ? null : string.Format(sessionUrlFormat, username, sessionId);

                return new SessionInfo(
                    SessionId: sessionId,
                    BestTime: bestTime,
                    Date: date,
                    ClockText: clockText,
                    ResultPositionText: resultPos,
                    Position: resultPosNumber,
                    SessionUrl: sessionUrl,
                    UserId: userId,
                    Username: username,
                    UserFullName: userFullName
                );
            })
            .Where(s => !string.IsNullOrWhiteSpace(s.SessionId))
            .ToList();
    }

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