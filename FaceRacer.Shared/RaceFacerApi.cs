using FaceRacer.Shared.Dto;

using System.Text.Json;

namespace FaceRacer.Shared;

public class RaceFacerApi
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public RaceFacerApi(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<Ranking>> GetRankingByTimeAsync(int kartId, int trackId, Period period, CancellationToken cancellationToken)
    {
        var url = $"https://www.racefacer.com/ajax/user-ranking-by-time-box?track_configuration_id={trackId}&kart_id={kartId}&period={period.ToString().ToLowerInvariant()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome");
        
        var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var jsonDoc = JsonDocument.Parse(json);

        // Check for empty ranking array.
        if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
        {
            if (dataElement.TryGetProperty("ranking", out var rankingElement))
            {
                if (rankingElement.ValueKind == JsonValueKind.Array && rankingElement.GetArrayLength() == 0)
                {
                    return [];
                }
            }
        }

        var result = JsonSerializer.Deserialize<RankingByTimeResult>(json, SerializerOptions);

        if (result.data?.ranking == null)
        {
            return [];
        }

        return result.data.ranking.Values.OrderBy(r => r.pos).ToList();
    }

    public async Task<UserBestRankingByTimeResult> GetUserBestRankingByTime(int kartId, int trackId, int userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.racefacer.com/ajax/user-best-ranking-by-time?user_id={userId}&track_configuration_id={trackId}&kart_id={kartId}");

        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<UserBestRankingByTimeResult>(json, SerializerOptions);

        return result;
    }

    public async Task<SessionChartData?> GetSessionChartData(int userId, string sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.racefacer.com/ajax/session/chart-data?user_id={userId}&session_id={sessionId}");

        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<SessionChartDataResult>(json, SerializerOptions);
        
        return result?.data;
    }

    public async Task<SessionBoxResponse> GetUserSessionsAsync(int kartId, int trackId, int userId, int startFrom = 0)
    {
        var url = $"https://www.racefacer.com/ajax/sessions-boxes?user_id={userId}&track_configuration_id={trackId}&period=all&start_from={startFrom}&only_victories=0&only_best_time_sessions=0&kart_id={kartId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();

        var responseObject = JsonSerializer.Deserialize<SessionBoxResponse>(responseJson, SerializerOptions);
        
        return responseObject;
    }
}
