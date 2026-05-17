using FaceRacer.Shared.Dto;

using System.Text;
using System.Text.Json;

namespace FaceRacer.Shared;

/// <summary>
/// Race Facer "public" API client.
/// </summary>
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

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var origin = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly93d3cucmFjZWZhY2VyLmNvbQ=="));
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Referer", origin);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome");
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.racefacer.com/ajax/user-best-ranking-by-time?user_id={userId}&track_configuration_id={trackId}&kart_id={kartId}");

        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<UserBestRankingByTimeResult>(json, SerializerOptions);

        return result;
    }

    public async Task<SessionChartData?> GetSessionChartData(int userId, string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.racefacer.com/ajax/session/chart-data?user_id={userId}&session_id={sessionId}");

        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<SessionChartDataResult>(json, SerializerOptions);
        
        return result?.data;
    }

    public async Task<SessionBoxResponse> GetUserSessionsAsync(int kartId, int trackId, int userId, int startFrom = 0)
    {
        var url = $"https://www.racefacer.com/ajax/sessions-boxes?user_id={userId}&track_configuration_id={trackId}&period=all&start_from={startFrom}&only_victories=0&only_best_time_sessions=0&kart_id={kartId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();

        var responseObject = JsonSerializer.Deserialize<SessionBoxResponse>(responseJson, SerializerOptions);
        
        return responseObject;
    }
}
