using FaceRacerLive.Dto;

using System.Text.Json;

namespace FaceRacerLive.Monitor;

public sealed class RaceMonitorApi : IRaceMonitorApi
{
    private static int _index;
    private readonly HttpClient _httpClient;
    private IReadOnlyList<SessionData>? _simulationArray;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void ResetSimulation()
    {
        _index = 0;
    }

    public RaceMonitorApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SessionData?> GetCurrentSession(CancellationToken cancellationToken)
    {
        if (AppSettings.IsSimulation)
        {
            _simulationArray ??= await SampleRaceZipReader.ReadAllSessionsFromZipAsync("sample_races/session_130_2026_02_08.zip", cancellationToken);

            if (_simulationArray is null || _simulationArray.Count == 0)
            {
                return null;
            }

            var sessionData = _simulationArray[_index];

            _index = (_index + 1) % _simulationArray.Count;

            return sessionData;
        }

        var url = AppSettings.CurrentSessionMonitorUrl;
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<SessionData>(json, SerializerOptions);
    }
}