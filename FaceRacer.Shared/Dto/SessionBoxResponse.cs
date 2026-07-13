using System.Text.Json.Serialization;

namespace FaceRacer.Shared.Dto;

// Convert to class:
public class SessionBoxResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("html")]
    public string Html { get; set; }

    public List<SessionInfo> Sessions { get; set; }
}