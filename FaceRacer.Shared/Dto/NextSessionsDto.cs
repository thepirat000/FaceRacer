namespace FaceRacer.Shared.Dto;

public sealed class NextSessionsResponse
{
    public bool success { get; set; }

    public bool errors { get; set; }

    public NextSessionsOuterData? data { get; set; }
}

public sealed class NextSessionsOuterData
{
    public List<NextSession>? data { get; set; }
}

public sealed class NextSession
{
    public int? id { get; set; }

    public string? name { get; set; }

    public string? plain_name { get; set; }

    public string? label { get; set; }

    public string? start_time { get; set; }

    public string? participants_text { get; set; }

    public string? start_after { get; set; }

    public string? checkin_time { get; set; }

    public string? time_left { get; set; }

    public NextSessionRuns? runs { get; set; }
}

public sealed class NextSessionRuns
{
    public List<NextSessionRun>? data { get; set; }
}

public sealed class NextSessionRun
{
    public string? name { get; set; }

    public string? full_name { get; set; }

    public string? kart_color { get; set; }

    public string? kart_name { get; set; }

    public string? kart { get; set; }

    public string? best_time { get; set; }

    public int? kart_id { get; set; }

    public string? uuid { get; set; }
}
