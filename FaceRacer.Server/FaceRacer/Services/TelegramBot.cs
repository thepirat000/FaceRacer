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

    private static readonly Regex ArgumentListRegex = new Regex(@"""([^""]*)""|([^,\s]+)"); // Matches either quoted strings or unquoted words separated by commas or whitespace

    private static readonly LinkPreviewOptions LinkPreviewOptions = new LinkPreviewOptions() { IsDisabled = true };
    private const string MessageTooLongText = "Message too long, cannot display sessions, remove the html argument to show as table, or reduce the number of sessions. For example: /sessions 1234567 20 html";

    private const int MessageMaxLength = 4150;

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
            new BotCommand { Command = "racer", Description = "Returns info about racer with given ID or name. Format: /racer {ID|Name} (e.g. racer 17111753 or racer John Doe)" },
            new BotCommand { Command = "sessions", Description = "Returns the last sessions for given user ID. Format: /sessions {UserID} [{MaxSessions}] [{OutputFormat}] (e.g. sessions 17111753 csv). OutputFormats: html, csv, table" },
            new BotCommand { Command = "session", Description = "Returns details about a session for given user ID and session position. Format: /session {UserID} [{SessionPosition}] (e.g. session 12345 1)" },
            new BotCommand { Command = "csv", Description = "Returns a csv report for the historic sessions of the given user IDs. Format: /csv {UserID1} {UserID2} ... {UserIDN}" }
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
        var msg = message.Text?.ToLowerInvariant() ?? "";

        Console.WriteLine($"Message received: {msg}");

        if (msg == "/start")
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "*Hello!*", parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
        }
        else if (msg == "ping")
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "*Pong!*", parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
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
            var data = message.Text!.Split(' ', 4);
            if (data.Length < 2 || !int.TryParse(data[1], out int userId))
            {
                await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /sessions {UserID} [{MaxSessions}] [{OutputFormat}]\nOutputFormat: html, csv or table", cancellationToken: cancellationToken);
                return;
            }

            var args = ParseArguments(message.Text);
            
            var isHtml = args.Count > 2 && args[2].Equals("html", StringComparison.OrdinalIgnoreCase);
            var isCsv = args.Count > 2 && args[2].Equals("csv", StringComparison.OrdinalIgnoreCase);
            int defaultMax = isCsv ? 500 : isHtml ? 50 : 100;
            var maxSessions = data.Length > 2 && int.TryParse(data[2], out int max) ? max : defaultMax;
            var format = isHtml ? SessionsMessageFormat.Html : isCsv ? SessionsMessageFormat.Csv : SessionsMessageFormat.Table;
            await HandleMessageSessions(bot, message.Chat.Id, userId, maxSessions, format, cancellationToken);
        }
        else if (msg.StartsWith("/csv", StringComparison.OrdinalIgnoreCase) || msg.StartsWith("csv", StringComparison.OrdinalIgnoreCase))
        {
            // Format: /csv <userId1> <userId2> ... <userIdN>
            var userIds = ParseArguments(message.Text);
            await HandleMessageCsvMultiUser(bot, message, cancellationToken);

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
            await HandleCommandSessions(args, bot, message, 100, SessionsMessageFormat.Table, cancellationToken);
        }
        else if (action == "LCSV") // Last sessions
        {
            await HandleCommandSessions(args, bot, message, 500, SessionsMessageFormat.Csv, cancellationToken);
        }
    }

    // Handle message: /top <number> [period]
    private async Task HandleMessageTop(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var args = ParseArguments(message.Text);

        if (args.Count < 1 || !int.TryParse(args[0], out int top) || top <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /top <number> [period]", cancellationToken: cancellationToken);
            return;
        }

        if (!Enum.TryParse(args.Count > 1 ? args[1] : "All", ignoreCase: true, out Period period))
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
            if (responseMessage.Length + line.Length > MessageMaxLength)
            {
                partial = true;
                break;
            }
            responseMessage.AppendLine(line);
        }

        await bot.SendMessage(message.Chat.Id, responseMessage + (partial ? "[...]" : ""), ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
    }

    // Handle message: /pos <number> [period]
    private async Task HandleMessagePos(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var args = ParseArguments(message.Text);

        if (args.Count < 1 || !int.TryParse(args[0], out int pos) || pos <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /pos <number> [period]", cancellationToken: cancellationToken);
            return;
        }

        if (!Enum.TryParse(args.Count > 1 ? args[1] : "All", ignoreCase: true, out Period period))
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

        await bot.SendMessage(chatId: message.Chat.Id, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
    }

    // Handle message: /racer <Id|Name>
    private async Task HandleMessageRacer(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var args = ParseArguments(message.Text);
        if (args.Count < 1)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /racer <Id|Name>", cancellationToken: cancellationToken);
            return;
        }
        var searchArg = args[0];
        if (int.TryParse(searchArg, out var userId))
        {
            // User ID provided
            await HandleMessageRacerInfo(bot, message.Chat.Id, userId, cancellationToken);
        }
        else
        {
            // Name provided
            await HandleMessageRacerSearch(bot, message.Chat.Id, searchArg, cancellationToken);

        }
    }

    // Handle message: /racer Id
    private async Task HandleMessageRacerInfo(ITelegramBotClient bot, long chatId, int userId, CancellationToken cancellationToken)
    {
        var best = await _raceFacerApi.GetUserBestRankingByTime(_appSettings.KartId, _appSettings.TrackId, userId);

        if (best == null)
        {
            await bot.SendMessage(chatId: chatId, text: $"Could not find user {userId}.", cancellationToken: cancellationToken);
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
                InlineKeyboardButton.WithCallbackData("Sessions", $"L:{userId}"),
                InlineKeyboardButton.WithCallbackData("CSV", $"LCSV:{userId}")
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
                    await bot.SendMessage(chatId: chatId, text: responseMessage.ToString(), replyMarkup: keyboard, parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
                }
            }
        }
        else
        {
            await bot.SendMessage(chatId: chatId, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
        }
    }

    // Handle message: /racer Name
    private async Task HandleMessageRacerSearch(ITelegramBotClient bot, long chatId, string searchArg, CancellationToken cancellationToken)
    {
        var users = await _dbContext.SearchUsers("%" + searchArg + "%").Take(50).ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            await bot.SendMessage(chatId: chatId, text: $"No users found matching '{searchArg}'.", cancellationToken: cancellationToken);
            return;
        }

        if (users.Count == 1)
        {
            await HandleMessageRacerInfo(bot, chatId, users[0].user_id, cancellationToken);
            return;
        }

        var responseMessage = new StringBuilder();
        responseMessage.AppendLine($"{users.Count} users found:");
        foreach (var user in users)
        {
            responseMessage.AppendLine($"{user.user_id}: *{user.full_name}* ({user.age}) - [{user.username}]({user.profile_url})");
        }
        
        await bot.SendMessage(chatId: chatId, text: responseMessage.ToString(), parseMode: ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
    }

    // Handle message: /sessions <userId> (responds with the user's sessions)
    private async Task HandleMessageSessions(ITelegramBotClient bot, long chatId, int userId, int maxSessions, SessionsMessageFormat format, CancellationToken cancellationToken)
    {
        var sessionsData = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, _appSettings.SessionUrl, userId, maxSessions, includeLapDetails: true, cancellationToken);

        if (sessionsData.Error || !sessionsData.Success)
        {
            await bot.SendMessage(chatId: chatId, text: $"Error when getting sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        if (sessionsData.Total <= 0)
        {
            await bot.SendMessage(chatId: chatId, text: $"No sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        var responseMessage = MakeUserSessionsMessage(sessionsData.Sessions, sessionsData.Total, format);

        if (format is SessionsMessageFormat.Csv)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseMessage));
            var userFullName = sessionsData.Sessions[0].UserFullName;
            
            await bot.SendDocument(
                chatId: chatId,
                caption: $"Last *{sessionsData.Sessions.Count}* sessions for *{userFullName}*",
                parseMode: ParseMode.Markdown,
                document: InputFile.FromStream(stream, $"{userFullName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv"), 
                cancellationToken: cancellationToken);
        }
        else
        {
            await bot.SendMessage(chatId: chatId, text: responseMessage, parseMode: format == SessionsMessageFormat.Table ? ParseMode.Markdown : ParseMode.Html, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
        }
    }
    
    private string MakeUserSessionsMessage(List<SessionInfo> allSessions, int total, SessionsMessageFormat format)
    {
        if (allSessions == null || allSessions.Count == 0)
        {
            return "No sessions found.";
        }

        allSessions = allSessions.FindAll(s => s.BestTime != "-" && s.LapDetails is { Count: > 0 });

        var boldStart = format == SessionsMessageFormat.Table ? "*" : "<b>";
        var boldEnd = format == SessionsMessageFormat.Table ? "*" : "</b>";

        var username = allSessions[0].Username;
        var userFullName = allSessions[0].UserFullName;

        // Get best times and average
        var times = allSessions
            .Select(s => TimeSpan.TryParseExact(s.BestTime, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue)
            .Where(t => Math.Abs(t - double.MaxValue) > 0.0001)
            .ToList();

        var bestTimeSeconds = times.DefaultIfEmpty(double.MaxValue).Min();
        var averageBestTimeSeconds = times.DefaultIfEmpty(0).Average();

        // Get the average position
        var positions = allSessions.Select(s => s.Position).Where(p => p != int.MaxValue).ToList();
        var averagePosition = positions.DefaultIfEmpty(0).Average();
        
        // Create a string with the following format:
        // Last {N} sessions for user {username}:
        // 1. {date} at {time} - [{bestTime}]({sessionUrl})
        var responseMessage = new StringBuilder();

        if (format is SessionsMessageFormat.Table or SessionsMessageFormat.Html)
        {
            responseMessage.AppendLine($"{boldStart}Last {allSessions.Count} of {total} sessions for {userFullName}:{boldEnd}");
        }

        if (format is SessionsMessageFormat.Table)
        {
            responseMessage.AppendLine("```");
            responseMessage.AppendLine("#   Date               Best     Pos");
            responseMessage.AppendLine("-------------------------------------");
        }
        else if (format is SessionsMessageFormat.Csv)
        {
            responseMessage.AppendLine("#,Date,Time,Best Time,Average Time,Laps,Position,Session URL");
        }

        for (int i = 0; i < allSessions.Count; i++)
        {
            var session = allSessions[i];
            var reducedBestTime = session.BestTime.StartsWith("00:") ? session.BestTime[3..] : session.BestTime;
            var isBest = Math.Abs((TimeSpan.TryParseExact(session.BestTime, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue) - bestTimeSeconds) < 0.0001;
            var isBestIcon = format == SessionsMessageFormat.Table ? "⏱️" : " ⏱️";
            var isBestText = isBest ? isBestIcon : "";

            var averageLapTimeSeconds = session.LapDetails?.Average(l => TimeSpan.TryParseExact(l.Time, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue) ?? 0;

            if (format == SessionsMessageFormat.Table)
            {
                responseMessage.AppendLine($"{(i + 1) + ".",-3} {session.Date:yyyy-MM-dd} {session.ClockText,-7} {reducedBestTime + isBestText,-8} {session.ResultPositionText}");
            }
            else if(format == SessionsMessageFormat.Html)
            {
                var sessionUrl = _appSettings.GetSessionUrl(username, session.SessionId);
                responseMessage.AppendLine($"{boldStart}{(i + 1) + "."}{(isBest ? "" : boldEnd)} {session.Date:yy-MM-dd} {session.ClockText,-7} <a href=\"{sessionUrl}\">{reducedBestTime}</a> {session.ResultPositionText}{isBestText}{(isBest ? boldEnd : "")}");
            }
            else if(format == SessionsMessageFormat.Csv)
            {
                var sessionUrl = _appSettings.GetSessionUrl(username, session.SessionId);
                responseMessage.AppendLine($"{i+1},{session.Date:yyyy-MM-dd},{session.ClockText},{reducedBestTime},{averageLapTimeSeconds:F3},{session.LapDetails?.Count ?? 0},{session.ResultPositionText},{sessionUrl}");
            }
        }

        if (format == SessionsMessageFormat.Table)
        {
            responseMessage.AppendLine("```");
        }
        else
        {
            responseMessage.AppendLine("");
        }

        if (format != SessionsMessageFormat.Csv)
        {
            // Add the Best time, Average time and Average position to the message
            responseMessage.AppendLine($"{boldStart}Sessions:{boldEnd} {allSessions.Count}");
            responseMessage.AppendLine($"{boldStart}Best Time:{boldEnd} {TimeSpan.FromSeconds(bestTimeSeconds):mm\\:ss\\.fff}");
            responseMessage.AppendLine($"{boldStart}Average Best Time:{boldEnd} {TimeSpan.FromSeconds(averageBestTimeSeconds):mm\\:ss\\.fff}");
            responseMessage.AppendLine($"{boldStart}Average Position:{boldEnd} {averagePosition:F2}");
        }

        if (format != SessionsMessageFormat.Csv && responseMessage.Length > MessageMaxLength)
        {
            return MessageTooLongText;
        }

        return responseMessage.ToString();
    }

    // Handle message: /session <userId> <sessionPosition> (responds with the user session at given session position. sessionPosition=1 means the latest, sessionPosition=2 the second latest, etc.)
    private async Task HandleMessageSessionDetails(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var args = ParseArguments(message.Text);
        if (args.Count < 1 || !int.TryParse(args[0], out int userId))
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /session <userId> [<sessionPosition>]", cancellationToken: cancellationToken);
            return;
        }

        int sessionPosition = 0;
        if (args.Count > 1 && int.TryParse(args[1], out int sp))
        {
            sessionPosition = Math.Max(0, sp - 1);
        }

        var sessionData = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, _appSettings.SessionUrl, userId, sessionPosition + 1, includeLapDetails: false, cancellationToken);

        if (sessionData.Error || !sessionData.Success)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"Error when getting sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        if (sessionData.Total <= 0)
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: $"No sessions for user '{userId}'.", cancellationToken: cancellationToken);
            return;
        }

        var session = sessionData.Sessions.Skip(sessionPosition).FirstOrDefault();

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

                var title = "*" + session.UserFullName + "*";

                var responseMessage = title + "\n" +
                                      $"*Session* [{date:yyyy-MM-dd} {session.ClockText}]({session.SessionUrl})\n" +
                                      $"Laps: {lapTimes.Count}\n" +
                                      laps +
                                      $"Average Time: {TimeSpan.FromSeconds(lapTimes.Average()):mm\\:ss\\.fff}\n";
            
                var chartTitle = $"{title} - {date:yyyy-MM-dd}";
                await using var chart = _chart.Graph(lapTimes.ToArray(), true, chartTitle);

                await bot.SendPhoto(chatId: message.Chat.Id, photo: InputFile.FromStream(chart, "laps_graph.png"), caption: responseMessage, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            else
            {
                await bot.SendMessage(message.Chat.Id, "Could not fetch session details.", cancellationToken: cancellationToken);
            }
        }
    }

    // Handle message: /csv <userId1> <userId2> ... <userIdN> (responds with a CSV report for the historic sessions of the given user IDs)
    private async Task HandleMessageCsvMultiUser(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var args = ParseArguments(message.Text);
        if (args.Count < 1 || args.Any(a => !int.TryParse(a, out _)))
        {
            await bot.SendMessage(chatId: message.Chat.Id, text: "Usage: /csv <userId1> <userId2> ... <userIdN>", cancellationToken: cancellationToken);
            return;
        }

        var allSessions = new List<SessionInfo>();

        foreach (var userIdStr in args)
        {
            var userId = int.Parse(userIdStr);

            var sessionData = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, _appSettings.SessionUrl, userId, 500, includeLapDetails: true, cancellationToken);

            if (sessionData.Error || !sessionData.Success)
            {
                await bot.SendMessage(chatId: message.Chat.Id, text: $"Error when getting sessions for user '{userId}'.", cancellationToken: cancellationToken);
                return;
            }

            if (sessionData.Sessions.Count == 0)
            {
                await bot.SendMessage(chatId: message.Chat.Id, text: $"No sessions for user '{userId}'.", cancellationToken: cancellationToken);
                return;
            }

            allSessions.AddRange(sessionData.Sessions);
        }

        var stream = MakeCsvStreamUserSessions(allSessions);

        // send csv as document
        await bot.SendDocument(
            chatId: message.Chat.Id,
            caption: $"CSV report for {allSessions.Count} sessions for {args.Count} users",
            parseMode: ParseMode.Markdown,
            document: InputFile.FromStream((Stream)stream, $"sessions_{DateTime.Now:yyyyMMdd}.csv"), 
            cancellationToken: cancellationToken);
    }

    private Stream MakeCsvStreamUserSessions(List<SessionInfo> allSessions)
    {
        // Return a memory stream with the CSV content
        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        writer.WriteLine("UserId,#,Name,DateTime,BestTime,AverageLapTime,Position,Racers,Laps,RunningAverageBestTime,RunningAverageAverageTime,PersonalBestTime,PersonalBestAverageLapTime,SessionUrl");
        var userCount = new Dictionary<int, RacerStats>(); // UserId -> RacerStats
        
        allSessions = allSessions
            .Where(s => s.BestTime != "-" && s.LapDetails is { Count: > 0 })
            .OrderBy(s => s.UserId)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.ClockText)
            .ToList();

        foreach (var session in allSessions)
        {
            var averageLapTimeSeconds = session.LapDetails?.Average(l => TimeSpan.TryParseExact(l.Time, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : double.MaxValue) ?? 0;
            var bestTimeSeconds = TimeSpan.TryParseExact(session.BestTime, "mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var tsBest) ? tsBest.TotalSeconds : double.MaxValue;

            userCount.TryAdd(session.UserId, new RacerStats());
            userCount[session.UserId].SessionCount++;
            var sessionNumber = userCount[session.UserId].SessionCount;

            // calculate the running values
            var runningAverageBestTime = (userCount[session.UserId].RunningAverageBestTime * (sessionNumber - 1) + bestTimeSeconds) / sessionNumber;
            userCount[session.UserId].RunningAverageBestTime = runningAverageBestTime;
            var runningAverageAverageTime = (userCount[session.UserId].RunningAverageAverageTime * (sessionNumber - 1) + averageLapTimeSeconds) / sessionNumber;
            userCount[session.UserId].RunningAverageAverageTime = runningAverageAverageTime;
            var personalBestTime = userCount[session.UserId].PersonalBestTime == 0 ? bestTimeSeconds : Math.Min(userCount[session.UserId].PersonalBestTime, bestTimeSeconds);
            userCount[session.UserId].PersonalBestTime = personalBestTime;
            var personalBestAverageLapTime = userCount[session.UserId].PersonalBestAverageLapTime == 0 ? averageLapTimeSeconds : Math.Min(userCount[session.UserId].PersonalBestAverageLapTime, averageLapTimeSeconds);
            userCount[session.UserId].PersonalBestAverageLapTime = personalBestAverageLapTime;
            
            writer.WriteLine($"{session.UserId},{sessionNumber},{session.UserFullName},{session.Date:yyyy-MM-dd} {session.ClockText},{bestTimeSeconds:F3},{averageLapTimeSeconds:F3},{session.Position},{session.RacerCount},{session.LapDetails?.Count ?? 0},{runningAverageBestTime:F3},{runningAverageAverageTime:F3},{personalBestTime:F3},{personalBestAverageLapTime:F3},{session.SessionUrl}");
        }
        writer.Close();
        memoryStream.Position = 0;
        return memoryStream;
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

            var title = GetFirstLine(message.Caption ?? message.Text);

            var responseMessage = title + "\n" +
                                  $"*Session Data for {date:yyyy-MM-dd}*\n" +
                                  $"Laps: {lapTimes.Count}\n" +
                                  laps +
                                  $"Average Time: {TimeSpan.FromSeconds(lapTimes.Average()):mm\\:ss\\.fff}\n";

            var chartTitle = title[2..] + $" - {date:yyyy-MM-dd}";
            await using var chart = _chart.Graph(lapTimes.ToArray(), true, chartTitle);

            await bot.SendPhoto(chatId: message.Chat.Id, photo: InputFile.FromStream(chart, "laps_graph.png"), caption: responseMessage, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
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

            await bot.SendMessage(message.Chat.Id, responseMessage, ParseMode.Markdown, linkPreviewOptions: LinkPreviewOptions, cancellationToken: cancellationToken);
        }
        else
        {
            await bot.SendMessage(message.Chat.Id, "Could not fetch racer details.", cancellationToken: cancellationToken);
        }
    }

    // Handle command: sessions
    private async Task HandleCommandSessions(string args, ITelegramBotClient bot, Message message, int maxSessions, SessionsMessageFormat format, CancellationToken cancellationToken)
    {
        var userId = int.Parse(args);

        await HandleMessageSessions(bot, message.Chat.Id, userId, maxSessions, format, cancellationToken);
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

    static List<string> ParseArguments(string input)
    {
        // Remove the command e.g. /csv
        var firstSpace = input.IndexOf(' ');
        if (firstSpace < 0)
            return [];

        var args = input[(firstSpace + 1)..];

        return ArgumentListRegex.Matches(args)
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            .ToList();
    }

    public enum SessionsMessageFormat
    {
        Table,
        Html,
        Csv
    }

    /// <summary>
    /// Represents the racer running stats, including session count, running averages, and personal bests.
    /// </summary>
    private sealed record RacerStats
    {
        /// <summary>Counter per user session, starting on 1 on the earliest session</summary>
        public int SessionCount { get; set; }

        /// <summary>Running average of the best times for the user</summary>
        public double RunningAverageBestTime { get; set; }

        /// <summary>Running average of the average lap times for the user</summary>
        public double RunningAverageAverageTime { get; set; }

        /// <summary>The personal best time for the user</summary>
        public double PersonalBestTime { get; set; }

        /// <summary>The personal best average lap time for the user</summary>
        public double PersonalBestAverageLapTime { get; set; }
    }
}