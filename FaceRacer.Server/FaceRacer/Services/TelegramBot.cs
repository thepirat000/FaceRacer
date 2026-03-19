using FaceRacer.DB;
using FaceRacer.Settings;
using FaceRacer.Shared;
using FaceRacer.Shared.Dto;

using Microsoft.EntityFrameworkCore;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FaceRacer.Services;

public class TelegramBot
{
    private static readonly Regex ProfileNameMatchRegex = new Regex(@"/profile/([^/?#]+)");

    private readonly RaceFacerApi _raceFacerApi;
    private readonly AppSettings _appSettings;
    private readonly ChartLaps _chart;
    private readonly FaceRacerDbContext _dbContext;
    private TelegramBotClient _botClient = null;

    public TelegramBot(AppSettings settings, RaceFacerApi raceFacerApi, ChartLaps chart)
    {
        _appSettings = settings;
        _raceFacerApi = raceFacerApi;
        _chart = chart;
        _dbContext = new FaceRacerDbContext();
    }

    public async Task SetupBot(CancellationToken cancellationToken)
    {
        if (_botClient != null)
        {
            return;
        }

        _botClient = new TelegramBotClient(_appSettings.TelegramBotToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        await _botClient.SetMyCommands(
        [
            new BotCommand { Command = "top", Description = "Returns the top X for given period Y. Format: /top {N} {Period} (e.g. top 10 day)" },
            new BotCommand { Command = "pos", Description = "Returns the racer at position X for given period Y. Format: /pos {N} {Period} (e.g. pos 5 week)" },
            new BotCommand { Command = "racer", Description = "Returns info about racer with given ID or name. Format: /racer {ID|Name} (e.g. racer 12345 or racer JohnDoe)" },
            new BotCommand { Command = "sessions", Description = "Returns the last sessions for given user ID. Format: /sessions {UserID} [{MaxSessions}] (e.g. sessions 12345 50)" },
            new BotCommand { Command = "session", Description = "Returns details about a session for given user ID and session position. Format: /session {UserID} [{SessionPosition}] (e.g. session 12345 1)" }
        ], cancellationToken: cancellationToken);

        _botClient.StartReceiving(updateHandler: HandleUpdateAsync, errorHandler: HandleErrorAsync, receiverOptions, cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text != null)
        {
            // Handle normal messages
            await HandleMessageAsync(bot, update.Message, cancellationToken);
        }
        else if (update.CallbackQuery != null)
        {
            // Handle inline button callback
            await HandleCallbackMessageAsync(bot, update.CallbackQuery, cancellationToken);
        }
    }

    // Handle normal messages
    private async Task HandleMessageAsync(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var msg = message.Text;

        Console.WriteLine($"Message received: {msg}");

        if (msg == "/start")
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "*Hello!*", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        else if (msg.StartsWith("/top", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("top", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /top <number> [period]
            await HandleMessageTop(bot, message, cancellationToken);
        }
        else if (msg.StartsWith("/pos", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("pos", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /pos <number> [period]
            await HandleMessagePos(bot, message, cancellationToken);
        }
        else if (msg.StartsWith("/racer", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("racer", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /racer <ID|Name>
            await HandleMessageRacer(bot, message, cancellationToken);
        }
        else if (msg.StartsWith("/sessions", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("sessions", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /sessions <userId>
            var data = message.Text!.Split(' ', 3);
            if (data.Length < 2 || !int.TryParse(data[1], out int userId))
            {
                await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /sessions <userId>", cancellationToken: cancellationToken);
                return;
            }
            var maxSessions = data.Length > 2 && int.TryParse(data[2], out int max) ? max : 100;
            await HandleMessageSessions(bot, message, userId, maxSessions, cancellationToken);
        }
        else if (msg.StartsWith("/session", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("session", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /session <userId> <sessionPosition>
            await HandleMessageSessionDetails(bot, message, cancellationToken);
        }
    }

    // Handle inline button callback
    private async Task HandleCallbackMessageAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var callbackData = callbackQuery.Data;
        var message = callbackQuery.Message;

        Console.WriteLine($"Callback received: {callbackData}");

        try
        {
            // Answer the callback so the client's loading spinner stops
            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ignoring error answering callback: {e}");
        }

        if (string.IsNullOrEmpty(callbackData) || message == null)
        {
            return;
        }

        var parts = callbackData.Split(':', 2);

        var action = parts[0];
        var args = parts.Length > 1 ? parts[1] : "";

        if (action == "S") // Session
        {
            await HandleCommandSession(args, bot, message, cancellationToken);
        }
        else if (action == "R") // Racer
        {
            await HandleCommandRacer(args, bot, message, cancellationToken);
        }
        else if (action == "L") // Last sessions
        {
            await HandleCommandSessions(args, bot, message, cancellationToken);
        }
    }

    // Handle message: /top <number> [period]
    private async Task HandleMessageTop(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var data = message.Text!.Split(' ', 3);
        if (data.Length < 2 || !int.TryParse(data[1], out int top) || top <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /top <number> [period]", cancellationToken: cancellationToken);
            return;
        }

        if (!Enum.TryParse(data.Length > 2 ? data[2] : "All", ignoreCase: true, out Period period))
        {
            period = Period.All;
        }

        var rankings = (await _raceFacerApi.GetRankingByTimeAsync(_appSettings.KartId, _appSettings.TrackId, period, cancellationToken)).Take(top).ToList();

        if (rankings.Count == 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No rankings found for {period}.", cancellationToken: cancellationToken);
            return;
        }

        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"*Top {top} Racers for {period}*");
        var partial = false;
        foreach (var racer in rankings)
        {
            var date = DateTime.ParseExact(racer.date, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var line = $"{racer.pos}. {racer.best_time} [{racer.full_name}]({racer.profile_url}) [{date:yyyy-MM-dd}]({_appSettings.GetSessionUrl(racer.username, racer.session_uuid)})";
            if (responseMessage.Length + line.Length > 8300)
            {
                partial = true;
                break;
            }
            responseMessage.AppendLine(line);
        }

        await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage + (partial ? "[...]" : ""), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }

    // Handle message: /pos <number> [period]
    private async Task HandleMessagePos(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var data = message.Text!.Split(' ', 3);
        if (data.Length < 2 || !int.TryParse(data[1], out int pos) || pos <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /pos <number> [period]", cancellationToken: cancellationToken);
            return;
        }

        if (!Enum.TryParse(data.Length > 2 ? data[2] : "All", ignoreCase: true, out Period period))
        {
            period = Period.All;
        }

        var ranking = (await _raceFacerApi.GetRankingByTimeAsync(_appSettings.KartId, _appSettings.TrackId, period, cancellationToken)).FirstOrDefault(r => r.pos == pos);

        if (ranking == null)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No rankings found for pos {pos} in {period}.", cancellationToken: cancellationToken);
            return;
        }

        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"*Position {pos} for {period}*");
        
        var date = DateTime.ParseExact(ranking.date, "dd.MM.yyyy", CultureInfo.InvariantCulture);
        var line = $"{pos}. [{ranking.full_name}]({ranking.profile_url}) - {ranking.best_time} ([{date:yyyy-MM-dd}]({_appSettings.GetSessionUrl(ranking.username, ranking.session_uuid)}))";
        responseMessage.AppendLine(line);

        await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }

    // Handle message: /racer <Id|Name>
    private async Task HandleMessageRacer(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var data = message.Text!.Split(' ', 2);
        if (data.Length < 2)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /racer <Id|Name>", cancellationToken: cancellationToken);
            return;
        }
        var searchArg = data[1];
        if (int.TryParse(searchArg, out var userId))
        {
            // User ID provided
            await HandleMessageRacerInfo(bot, message, userId, cancellationToken);
        }
        else
        {
            // Name provided
            await HandleMessageRacerSearch(bot, message, searchArg, cancellationToken);

        }
    }

    // Handle message: /racer Id
    private async Task HandleMessageRacerInfo(ITelegramBotClient bot, Message message, int userId, CancellationToken cancellationToken)
    {
        var best = await _raceFacerApi.GetUserBestRankingByTime(_appSettings.KartId, _appSettings.TrackId, userId);

        if (best == null)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"Could not find user {userId}.", cancellationToken: cancellationToken);
            return;
        }

        var rankingsBySession = (await _dbContext.QueryRankingsForUser(userId).ToListAsync(cancellationToken))
            .GroupBy(r => r.session_uuid)
            .Select(g => new { SessionId = g.Key, Rankings = g.ToList() })
            .ToList();

        var age = rankingsBySession.FirstOrDefault()?.Rankings[0].age;
        var username = rankingsBySession.FirstOrDefault()?.Rankings[0].username;
        var profileUrl = rankingsBySession.FirstOrDefault()?.Rankings[0].profile_url;
        var profileImage = rankingsBySession.FirstOrDefault()?.Rankings[0].profile_image_normal;

        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"*{best.data.full_name}* ({age})");
        responseMessage.AppendLine($"ID: {userId}");
        responseMessage.AppendLine($"User: [{username}]({profileUrl})");
        responseMessage.AppendLine($"Best time: *{best.data.best_time}*");
        responseMessage.AppendLine($"All-Time Position: *{best.data.position}*");
        responseMessage.AppendLine("\nSessions:");
        foreach (var rankingBySession in rankingsBySession)
        {
            var first = rankingBySession.Rankings[0];
            var positionsString = string.Join(' ', rankingBySession.Rankings.Select(r => r.pos + r.Period[..1]));
            responseMessage.AppendLine($" - [{first.date:yyyy-MM-dd}]({_appSettings.GetSessionUrl(first.username, first.session_uuid)}): *{first.best_time}* ({positionsString})");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Sessions", $"L:{userId}")
            }
        });

        if (profileImage != null)
        {
            try
            {
                await bot.SendPhoto(chatId: _appSettings.TelegramChatId, photo: profileImage, caption: responseMessage.ToString(), replyMarkup: keyboard, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException e)
            {
                if (e.ErrorCode == 400 && e.Message.Contains("wrong type of the web page content"))
                {
                    // Problem with the image URL, send as text message instead
                    await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage.ToString(), replyMarkup: keyboard, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                }
            }
        }
        else
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }
    }

    // Handle message: /racer Name
    private async Task HandleMessageRacerSearch(ITelegramBotClient bot, Message message, string searchArg, CancellationToken cancellationToken)
    {
        var users = await _dbContext.SearchUsers("%" + searchArg + "%").Take(50).ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No users found matching '{searchArg}'.", cancellationToken: cancellationToken);
            return;
        }

        if (users.Count == 1)
        {
            await HandleMessageRacerInfo(bot, message, users[0].user_id, cancellationToken);
            return;
        }

        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"{users.Count} users found:");
        foreach (var user in users)
        {
            responseMessage.AppendLine($"{user.user_id}: *{user.full_name}* ({user.age}) - [{user.username}]({user.profile_url})");
        }
        
        await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }

    // Handle message: /sessions <userId> (responds with the user's sessions)
    private async Task HandleMessageSessions(ITelegramBotClient bot, Message message, int userId, int maxSessions, CancellationToken cancellationToken)
    {
        var firstPage = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, userId, 0);

        if (firstPage.error || !firstPage.success)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"Error when getting sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        if (firstPage.total <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        var total = Math.Min(maxSessions, firstPage.total);

        var firstSessions = SessionBoxesParser.Parse(firstPage.html, _appSettings);
        var allSessions = firstSessions.ToList();

        if (allSessions.Count < total)
        {
            // Fetch remaining pages in parallel.
            var pageSize = firstSessions.Count;

            if (pageSize > 0)
            {
                var offsets = Enumerable
                    .Range(1, (int)Math.Ceiling((total - pageSize) / (double)pageSize))
                    .Select(i => i * pageSize)
                    .Where(offset => offset < total)
                    .ToArray();

                var tasks = offsets.Select(async startFrom =>
                {
                    var page = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, userId, startFrom);
                    if (page.error || !page.success)
                    {
                        return (StartFrom: startFrom, Sessions: (IReadOnlyList<SessionInfo>)Array.Empty<SessionInfo>());
                    }

                    var sessions = SessionBoxesParser.Parse(page.html, _appSettings).ToList();
                    return (StartFrom: startFrom, Sessions: (IReadOnlyList<SessionInfo>)sessions);
                });

                var results = await Task.WhenAll(tasks);

                foreach (var result in results.OrderBy(r => r.StartFrom))
                {
                    allSessions.AddRange(result.Sessions);

                    if (allSessions.Count >= total)
                    {
                        break;
                    }
                }

                if (allSessions.Count > total)
                {
                    allSessions = allSessions.Take(total).ToList();
                }
            }
        }

        var responseMessage = MakeUserSessionsMessage(allSessions, firstPage.total, true);

        await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }
    
    private string MakeUserSessionsMessage(List<SessionInfo> allSessions, int total, bool showAsTable)
    {
        if (allSessions == null || allSessions.Count == 0)
        {
            return "No sessions found.";
        }

        var username = allSessions[0].Username;
        var userFullName = allSessions[0].UserFullName;

        // Get best times and average
        var times = allSessions
            .Select(s => TimeSpan.TryParseExact(s.BestTime, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue)
            .Where(t => Math.Abs(t - double.MaxValue) > 0.0001)
            .ToList();

        var bestTimeSeconds = times.DefaultIfEmpty(double.MaxValue).Min();
        var averageTimeSeconds = times.DefaultIfEmpty(0).Average();

        // Get the average position
        var positions = allSessions.Select(s => s.Position).Where(p => p != int.MaxValue).ToList();
        var averagePosition = positions.DefaultIfEmpty(0).Average();


        // Create a string with the following format:
        // Last {N} sessions for user {username}:
        // 1. {date} at {time} - [{bestTime}]({sessionUrl})
        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"*Last {allSessions.Count} of {total} sessions for {userFullName}:*");

        if (showAsTable)
        {
            responseMessage.AppendLine("```");
            responseMessage.AppendLine("#   Date               Best     Pos");
            responseMessage.AppendLine("-------------------------------------");
        }
        for (int i = 0; i < allSessions.Count; i++)
        {
            var session = allSessions[i];
            var reducedBestTime = session.BestTime.StartsWith("00:") ? session.BestTime[3..] : session.BestTime;
            var isBest = Math.Abs((TimeSpan.TryParseExact(session.BestTime, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue) - bestTimeSeconds) < 0.0001;
            var isBestText = isBest ? "⏱️" : "";

            if (showAsTable)
            {
                responseMessage.AppendLine($"{(i + 1) + ".",-3} {session.Date:yyyy-MM-dd} {session.ClockText,-7} {reducedBestTime + isBestText,-8} {session.ResultPositionText}");
            }
            else
            {
                var sessionUrl = _appSettings.GetSessionUrl(username, session.SessionId);
                responseMessage.AppendLine($"{session.Date:yy-MM-dd} {session.ClockText,-7} <a href=\"{sessionUrl}\">{reducedBestTime + isBestText,-8}</a> {session.ResultPositionText}");
            }
        }
        if (showAsTable)
        {
            responseMessage.AppendLine("```");
        }

        // Add the Best time, Average time and Average position to the message
        responseMessage.AppendLine($"*Best Time:* {TimeSpan.FromSeconds(bestTimeSeconds):mm\\:ss\\.fff}");
        responseMessage.AppendLine($"*Average Time:* {TimeSpan.FromSeconds(averageTimeSeconds):mm\\:ss\\.fff}");
        responseMessage.AppendLine($"*Average Position:* {averagePosition:F2}");
        
        return responseMessage.ToString();
    }

    // Handle message: /session <userId> <sessionPosition> (responds with the user session at given session position. sessionPosition=1 means the latest, sessionPosition=2 the second latest, etc.)
    private async Task HandleMessageSessionDetails(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var data = message.Text!.Split(' ', 3);
        if (data.Length < 2 || !int.TryParse(data[1], out int userId))
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /session <userId> [<sessionPosition>]", cancellationToken: cancellationToken);
            return;
        }

        int sessionPosition = 0;
        if (data.Length > 2 && int.TryParse(data[2], out int sp))
        {
            sessionPosition = Math.Max(0, sp - 1);
        }

        var sessionData = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, userId, sessionPosition);

        if (sessionData.error || !sessionData.success)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"Error when getting sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        if (sessionData.total <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        var session = SessionBoxesParser.Parse(sessionData.html, _appSettings).FirstOrDefault();

        if (session != null)
        {
            // Similar to HandleCommandSession to show the session info
            var chartData = await _raceFacerApi.GetSessionChartData(userId, session.SessionId);
            var date = session.Date;

            if (chartData != null)
            {
                var lapTimes = chartData.datasets[0].data;

                var laps = string.Join('\n', lapTimes.Select((time, index) =>
                {
                    var isBest = Math.Abs(time - lapTimes.Min()) < 0.0001;
                    return $"{(isBest ? "*" : "")}{index + 1} - {TimeSpan.FromSeconds(time):mm\\:ss\\.fff}{(isBest ? "* ⏱️ (best)" : "")}";
                })) + '\n';

                var title = "*" + session.UserFullName + " - Session Details*";

                var responseMessage = title + "\n" +
                                      $"*Session Data for {date:yyyy-MM-dd} {session.ClockText}*\n" +
                                      $"Laps: {lapTimes.Count}\n" +
                                      laps +
                                      $"Average Time: {TimeSpan.FromSeconds(lapTimes.Average()):mm\\:ss\\.fff}\n";

                var chartTitle = title[2..] + $" - {date:yyyy-MM-dd}";
                await using var chart = _chart.Graph(lapTimes.ToArray(), true, chartTitle);

                await bot.SendPhoto(chatId: message.Chat.Id, photo: InputFile.FromStream(chart, "laps_graph.png"), caption: responseMessage, ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            else
            {
                await bot.SendMessage(message.Chat.Id, "Could not fetch session details.", cancellationToken: cancellationToken);
            }
        }
    }

    // Handle command: session
    private async Task HandleCommandSession(string args, ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var data = args.Split(',', 4);
        var userId = int.Parse(data[0]);
        var sessionId = data[1];
        var date = DateOnly.Parse(data[2], CultureInfo.InvariantCulture);
        var chartData = await _raceFacerApi.GetSessionChartData(userId, sessionId);

        if (chartData != null)
        {
            var lapTimes = chartData.datasets[0].data;

            var laps = string.Join('\n', lapTimes.Select((time, index) =>
            {
                var isBest = Math.Abs(time - lapTimes.Min()) < 0.0001;
                return $"{(isBest ? "*" : "")}{index + 1} - {TimeSpan.FromSeconds(time):mm\\:ss\\.fff}{(isBest ? "* ⏱️ (best)" : "")}";
            })) + '\n';

            var title = GetFirstLine(message.Caption);

            var responseMessage = title + "\n" +
                                  $"*Session Data for {date:yyyy-MM-dd}*\n" +
                                  $"Laps: {lapTimes.Count}\n" +
                                  laps +
                                  $"Average Time: {TimeSpan.FromSeconds(lapTimes.Average()):mm\\:ss\\.fff}\n";

            var chartTitle = title[2..] + $" - {date:yyyy-MM-dd}";
            await using var chart = _chart.Graph(lapTimes.ToArray(), true, chartTitle);

            await bot.SendPhoto(chatId: message.Chat.Id, photo: InputFile.FromStream(chart, "laps_graph.png"), caption: responseMessage, ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        else
        {
            await bot.SendMessage(message.Chat.Id, "Could not fetch session details.", cancellationToken: cancellationToken);
        }
    }

    // Handle command: racer
    private async Task HandleCommandRacer(string args, ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var userId = int.Parse(args);
        var best = await _raceFacerApi.GetUserBestRankingByTime(_appSettings.KartId, _appSettings.TrackId, userId);
        var profileNameMatch = ProfileNameMatchRegex.Match(best.data.profile_url);
        var profileName = profileNameMatch.Success ? profileNameMatch.Groups[1].Value : null;

        if (best.success)
        {
            var date = DateTime.ParseExact(best.data.date, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            var responseMessage = $"🏆 *Ranking for {best.data.full_name}*\n" +
                                  $"All-Time Position: *{best.data.position}*\n" +
                                  $"Best: *{best.data.best_time}*\n" +
                                  $"Date: {date:yyyy-MM-dd}\n" +
                                  $"[{profileName ?? "Profile link"}]({best.data.profile_url})";

            await bot.SendMessage(message.Chat.Id, responseMessage, ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        else
        {
            await bot.SendMessage(message.Chat.Id, "Could not fetch racer details.", cancellationToken: cancellationToken);
        }
    }

    // Handle command: sessions
    private async Task HandleCommandSessions(string args, ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var userId = int.Parse(args);

        await HandleMessageSessions(bot, message, userId, 50, cancellationToken);
    }

    private static string GetFirstLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var index = text.IndexOfAny(['\r', '\n']);

        var firstLine = index >= 0 ? text[..index] : text;

        return firstLine;
    }

    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}