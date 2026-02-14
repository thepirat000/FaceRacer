using FaceRacer.DB.Entities;

namespace FaceRacer.Services.Notifiers;

public class ConsoleNotifier : INotifier
{
    public Task NotifyAsync(List<RecordChange> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var period = change.Period;
            var periodDate = change.PeriodDate;
            foreach (var diff in change.RankingChanges)
            {
                Console.WriteLine($"---> New TOP {diff.pos} for {period} ({periodDate}): Pos {diff.pos} - {diff.full_name} ({diff.best_time}) - Session: {diff.date} - {diff.session_uuid}");
            }
        }

        return Task.CompletedTask;
    }
}