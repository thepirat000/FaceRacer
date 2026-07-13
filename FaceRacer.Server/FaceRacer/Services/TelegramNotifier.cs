using FaceRacer.DB.Entities;
using FaceRacer.Shared.Dto;

using System.Text;
using FaceRacer.Settings;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FaceRacer.Services.Notifiers;

public class TelegramNotifier : INotifier
{
    private readonly TelegramBotClient _botClient;
    private readonly AppSettings _appSettings;

    public TelegramNotifier(AppSettings appSettings)
    {
        _appSettings = appSettings;
        _botClient = new TelegramBotClient(_appSettings.TelegramBotToken);
    }

    public async Task NotifyUpdateAsync(List<RecordChange> changes, CancellationToken cancellationToken)
    {
        // Only notify about sessions in the last 7 days. The RaceFacer API often returns old data...
        var minDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

        // Flatten changes with period context, filter, then group by user_id
        var recentTopChanges = changes
            .SelectMany(change => change.RankingChanges.Select(diff => new
            {
                change.Period,
                Diff = diff
            }))
            .Where(x => x.Diff.pos <= _appSettings.TelegramMaxPosition && x.Diff.date >= minDate)
            .GroupBy(x => x.Diff.user_id);

        foreach (var userGroup in recentTopChanges)
        {
            // Choose a representative for shared details like name, image, url, best_time
            var last = userGroup.Last();

            var name = last.Diff.full_name;
            var profileUrl = last.Diff.profile_url;
            var age = last.Diff.age;
            var bestTime = last.Diff.best_time;
            var profileImage = last.Diff.profile_image_normal;
            
            // Build a compact list of new records by period
            var recordsLines = userGroup.Select(x => $"🏆 TOP {x.Diff.pos} record for {x.Period}");

            var title = $"Top {last.Diff.pos} for {last.Period}";
                
            var sb = new StringBuilder();
            sb.AppendLine($"🏁 *{title}* - [{name}]({profileUrl}) ({age})");
            sb.AppendLine($"Best lap: *{bestTime}*");
            sb.AppendLine($"Session: [{last.Diff.date:yyyy-MM-dd}]({_appSettings.GetSessionUrl(last.Diff.username, last.Diff.session_uuid)}");
            sb.AppendLine();
            sb.AppendLine("Achievements:");
            sb.AppendLine(string.Join("\n", recordsLines));

            var message = sb.ToString();

            // If multiple sessions exist, include the most recent session as Details
            var mostRecentSessionUuid = last.Diff.session_uuid;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Session", $"S:{last.Diff.user_id},{mostRecentSessionUuid},{last.Diff.date}"),
                    InlineKeyboardButton.WithCallbackData("Racer", $"R:{last.Diff.user_id}"),
                    InlineKeyboardButton.WithCallbackData("Sessions", $"L:{last.Diff.user_id}")
                }
            });

            try
            {
                await _botClient.SendPhoto(chatId: _appSettings.TelegramChatId, photo: profileImage, caption: message, replyMarkup: keyboard, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                if (ex.Message.Contains("failed to get HTTP URL content") || ex.Message.Contains("wrong type of the web page content"))
                {
                    // profileImage not found, send just the text message
                    await _botClient.SendMessage(chatId: _appSettings.TelegramChatId, text: message, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
                }
                else
                {
                    throw;
                }
            }
            
        }
    }

    public async Task NotifyLastSessionAsync(SessionInfo session, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Session", $"S:{session.UserId},{session.SessionId},{session.Date}"),
                InlineKeyboardButton.WithCallbackData("Racer", $"R:{session.UserId}"),
                InlineKeyboardButton.WithCallbackData("Sessions", $"L:{session.UserId}")
            }
        });

        var message = $"🏁 New session for [{session.UserFullName}] !!!\n" +
                      $"Best lap: *{session.BestTime}*\n" +
                      $"Session: [{session.Date:yyyy-MM-dd} {session.ClockText}]({_appSettings.GetSessionUrl(session.Username, session.SessionId)})\n" +
                      $"Pos: {session.ResultPositionText}";

        await _botClient.SendMessage(chatId: _appSettings.TelegramChatId, text: message, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
    }
}