using System.Text.Json;
using FaceRacer.Shared.Dto;

namespace FaceRacer.Shared;

public sealed class RaceMonitorApi
{
    private readonly HttpClient _httpClient;
    private string _currentSessionMonitorUrl;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void Reset(string monitorUrl)
    {
        _currentSessionMonitorUrl = monitorUrl;
    }

    public RaceMonitorApi(HttpClient httpClient, string monitorUrl)
    {
        _httpClient = httpClient;
        _currentSessionMonitorUrl = monitorUrl;
    }

    public async Task<SessionData?> GetCurrentSession(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _currentSessionMonitorUrl);
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