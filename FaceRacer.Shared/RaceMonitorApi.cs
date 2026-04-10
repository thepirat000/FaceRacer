using FaceRacer.Shared.Dto;

using System.Net.Http.Headers;
using System.Text.Json;

namespace FaceRacer.Shared;

/// <summary>
/// Race Facer internal Monitors API client.
/// </summary>
public sealed class RaceMonitorApi
{
    private const string RequestedWithHeaderName = "X-Requested-With";
    private const string RequestedWithHeaderValue = "XMLHttpRequest";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome";

    private readonly HttpClient _httpClient;
    private Uri _currentSessionMonitorUri;

    public string CurrentSessionMonitorUrl => _currentSessionMonitorUri.ToString();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void Reset(string monitorUrl)
    {
        _currentSessionMonitorUri = new Uri(monitorUrl, UriKind.Absolute);
    }

    public RaceMonitorApi(HttpClient httpClient, string monitorUrl)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _currentSessionMonitorUri = new Uri(monitorUrl, UriKind.Absolute);

        if (!_httpClient.DefaultRequestHeaders.Contains(RequestedWithHeaderName))
        {
            _httpClient.DefaultRequestHeaders.Add(RequestedWithHeaderName, RequestedWithHeaderValue);
        }

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        _httpClient.DefaultRequestHeaders.CacheControl ??= new CacheControlHeaderValue { NoCache = true };
    }

    public async Task<SessionData?> GetCurrentSession(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _currentSessionMonitorUri);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<SessionData>(stream, SerializerOptions, cancellationToken);
    }

    public async Task<NextSessionsResponse?> GetNextSessionsAsync(string nextSessionsMonitorUrl, CancellationToken cancellationToken)
    {
        var url = $"{nextSessionsMonitorUrl}?from=0&limit=5";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<NextSessionsResponse?>(stream, SerializerOptions, cancellationToken);
    }
}