namespace FaceRacer.DB.Entities;

public class RankingDataQueryResult
{
    public Guid RankingId { get; set; }
    public string Period { get; set; }
    public DateOnly PeriodDate { get; set; }

    public string user_uuid { get; set; }
    public int user_id { get; set; }
    public int pos { get; set; }
    public string full_name { get; set; }
    public int age { get; set; }
    public string profile_image_normal { get; set; }
    public string profile_url { get; set; }
    public string best_time { get; set; }
    public int best_time_ms { get; set; }
    public DateOnly date { get; set; }
    public string username { get; set; }
    public string session_uuid { get; set; }
}