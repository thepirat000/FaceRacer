using FaceRacer.DB.Entities;
using FaceRacer.Services.Notifiers;
using FaceRacer.Settings;
using FaceRacer.Shared;
using FaceRacer.Shared.Dto;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

using ScottPlot.Statistics;

using System.Collections.Concurrent;
using System.Globalization;

namespace FaceRacer.Services;

public class FaceRacerService
{
    public readonly RaceFacerApi _raceFacerApi;
    private AppSettings _appSettings;

    private static readonly RetryStrategyOptions RetryOptions = new()
    {
        Delay = TimeSpan.Zero,
        MaxRetryAttempts = 4,
        OnRetry = args =>
        {
            var period = args.Context.Properties.GetValue(PeriodPropertyKey, default);
            Console.WriteLine($"Error when getting {period}: {args.Outcome.Exception?.Message} - Retrying #{args.AttemptNumber + 1}...");
            return default;
        }
    };
    private static readonly ResiliencePropertyKey<Period> PeriodPropertyKey = new(nameof(Period));

    public FaceRacerService(AppSettings appSettings, RaceFacerApi raceFacerApi)
    {
        _raceFacerApi = raceFacerApi;
        _appSettings = appSettings;
    }

    public async Task<DateTime?> RunUpdateAsync(CancellationToken cancellationToken = default)
    {
        // Get the 5 periods data from the API
        var rankings = await GetRankingsFromApi(cancellationToken);

        var currentPeriodDates = GetCurrentPeriodDates();

        await using var dbContext = new DB.FaceRacerDbContext();
            
        var now = DateTime.Now;
        bool hasDiffs = false;

        foreach (var ranking in rankings)
        {
            var period = ranking.Key;
            var rankingFromApi = ranking.Value;
            var periodDate = currentPeriodDates[period];

            if (rankingFromApi.Count == 0)
            {
                Log($"No TOP rankings data from API for {period} ({periodDate})");
                continue;
            }

            var existing = await dbContext.Rankings.FirstOrDefaultAsync(r => r.TrackId == _appSettings.TrackId && r.Period == period.ToString() && r.PeriodDate == periodDate, cancellationToken);

            if (existing == null)
            {
                var rankingData = CreateRankingDataEntity(_appSettings.TrackId, period, periodDate, rankingFromApi);

                await dbContext.Rankings.AddAsync(rankingData, cancellationToken);
                    
                Log($"Creating TOP rankings for {period} ({periodDate})");
            }
            else
            {
                var diffs = GetRankingDifferences(existing.RankingDetails, rankingFromApi).ToList();

                existing.UpdatedDate = now;

                if (diffs.Count == 0)
                {
                    Log($"No new TOP rankings for {period} ({periodDate})");

                    continue;
                }

                // Diffs !
                hasDiffs = true;

                var recordChange = new RecordChange()
                {
                    Id = Guid.CreateVersion7(),
                    Period = period.ToString(),
                    PeriodDate = periodDate,
                    TrackId = _appSettings.TrackId,
                    NotificationDate = now,
                    RankingChanges = CreateRankingDataDetails(diffs)
                };

                await dbContext.RecordChanges.AddAsync(recordChange, cancellationToken);

                existing.RankingDetails = CreateRankingDataDetails(rankingFromApi);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return hasDiffs ? now : null;
    }

    public async Task NotifyUpdateAsync(DateTime firstChangeDate, List<INotifier> notifiers, CancellationToken cancellationToken)
    {
        List<RecordChange> changes;

        await using (var dbContext = new DB.FaceRacerDbContext())
        {
            changes = await dbContext.RecordChanges
                .Where(ch => ch.NotificationDate >= firstChangeDate && !ch.Processed)
                .OrderBy(ch => ch.Id)
                .ToListAsync(cancellationToken);

            if (changes.Count == 0)
            {
                return;
            }

            foreach (var change in changes)
            {
                change.Processed = true;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var notifier in notifiers)
        {
            await notifier.NotifyUpdateAsync(changes, cancellationToken);
        }
    }

    private static Dictionary<Period, DateOnly> GetCurrentPeriodDates()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = new Dictionary<Period, DateOnly>
        {
            { Period.Day, today },
            { Period.Week, today.AddDays(-(int)today.DayOfWeek) },
            { Period.Month, new DateOnly(today.Year, today.Month, 1) },
            { Period.Year, new DateOnly(today.Year, 1, 1) },
            { Period.All, new DateOnly(1, 1, 1) }
        };

        return result;
    }

    // This ill return only the new TOP rankings, if there are any differences, otherwise it will return an empty list
    private static IEnumerable<Ranking> GetRankingDifferences(List<RankingDataDetails> rankingFromDb, List<Ranking> rankingFromApi)
    {
        for (int i = 0; i < rankingFromApi.Count; i++)
        {
            var apiItem = rankingFromApi[i];
            var existsUp = rankingFromDb.Take(i + 1).Any(r => r.user_id == apiItem.user_id);
            if (!existsUp)
            {
                yield return apiItem;
            }
        }
    }

    public async Task<Dictionary<Period, List<Ranking>>> GetRankingsFromApi(CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<Period, List<Ranking>>();

        var options = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = cancellationToken };

        var periods = Enum.GetValues<Period>();

        await Parallel.ForEachAsync(periods, options,
            async (period, ct) =>
            {
                await TryGetRanking(results, period, ct);
            });

        return results.ToDictionary();
    }

    private async Task TryGetRanking(IDictionary<Period, List<Ranking>>  results, Period period, CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder().AddRetry(RetryOptions).Build();
        try
        {
            Log($"Getting ranking for {period} from API...");

            var context = ResilienceContextPool.Shared.Get(cancellationToken);

            results[period] = await pipeline.ExecuteAsync(
                async ctx =>
                {
                    ctx.Properties.Set(PeriodPropertyKey, period);

                    return await _raceFacerApi.GetRankingByTimeAsync(_appSettings.KartId, _appSettings.TrackId, period, ctx.CancellationToken);
                },
                context);
        }
        catch (Exception ex)
        {
            Log($"Error getting ranking for {period}: {ex.Message}");
        }
    }

    private RankingData CreateRankingDataEntity(int trackId, Period period, DateOnly periodDate, List<Ranking> ranking)
    {
        var now = DateTime.Now;

        var rankingData = new RankingData
        {
            Id = Guid.CreateVersion7(),
            TrackId = trackId,
            Period = period.ToString(),
            PeriodDate = periodDate,
            RankingDetails = CreateRankingDataDetails(ranking),
            InsertedDate = now
        };
            
        return rankingData;
    }

    private List<RankingDataDetails> CreateRankingDataDetails(List<Ranking> ranking)
    {
        return ranking.ConvertAll(r => new RankingDataDetails
        {
            pos = r.pos,
            age = r.age,
            best_time = r.best_time,
            best_time_ms = r.best_time_ms,
            date = DateOnly.ParseExact(r.date, "dd.MM.yyyy", CultureInfo.InvariantCulture),
            first_name = r.first_name,
            last_name = r.last_name,
            full_name = r.full_name,
            kart_id = r.kart_id,
            profile_image = r.profile_image,
            profile_image_medium = r.profile_image_medium,
            profile_image_normal = r.profile_image_normal,
            profile_image_small = r.profile_image_small,
            profile_url = r.profile_url,
            rank = r.rank,
            run_id = r.run_id,
            session_uuid = r.session_uuid,
            user_id = r.user_id,
            user_rank_id = r.user_rank_id,
            user_uuid = r.user_uuid,
            username = r.username,
            age_group = r.age_group
        });
    }

    public async Task<Dictionary<int, SessionInfo>> GetUsersLastSessionAndNotifyAsync(List<int> userIds, List<INotifier> notifiers, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = cancellationToken };

        var usersLastSession = new ConcurrentDictionary<int, SessionInfo>();

        await Parallel.ForEachAsync(userIds, options,
            async (userId, ct) =>
            {
                var userSessionsResponse = await _raceFacerApi.GetUserSessionsAsync(_appSettings.KartId, _appSettings.TrackId, userId, 0, ct);

                if (userSessionsResponse.error || userSessionsResponse.total == 0)
                {
                    return;
                }

                var lastUserSession = (SessionBoxesParser.Parse(userSessionsResponse.html, _appSettings)).FirstOrDefault();

                if (lastUserSession != null)
                {
                    usersLastSession[userId] = lastUserSession;
                }
            });

        if (!usersLastSession.IsEmpty)
        {
            Log("Running racer alerts...");

            foreach (var userId in userIds)
            {
                if (usersLastSession.TryGetValue(userId, out var session))
                {
                    var daysAgo = Math.Abs((DateTime.Today - session.Date.ToDateTime(TimeOnly.MinValue)).Days);
                    
                    if (daysAgo < _appSettings.WatchRacers.MaxDays)
                    {
                        // TODO: Notify new sessions. Blocked by RaceFacer API not returning new sessions since May 2026 
                        //notifiers.ForEach(notifier => notifier.NotifyLastSessionAsync(session, cancellationToken));
                        // Log the data
                        Log($"User {session.UserFullName} ({session.UserId}) - Last session: {session.Date} - Best time: {session.BestTime} - Session ID: {session.SessionId}");
                    }
                }
            }
        }
        
        return usersLastSession.ToDictionary();
    }

    private static void Log(string log)
    {
        Console.WriteLine(log);
    }
}