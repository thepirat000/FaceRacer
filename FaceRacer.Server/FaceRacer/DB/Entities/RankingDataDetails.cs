namespace FaceRacer.DB.Entities;

public class RankingDataDetails
{
    public string user_uuid { get; set; }
    public int user_id { get; set; }
    public int pos { get; set; }
    public string full_name { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public int age { get; set; }
    public string profile_image { get; set; }
    public string profile_image_normal { get; set; }
    public string profile_image_medium { get; set; }
    public string profile_image_small { get; set; }
    public string profile_url { get; set; }
    public string rank { get; set; }
    public int user_rank_id { get; set; }
    public string age_group { get; set; }
    public string best_time { get; set; }
    public int best_time_ms { get; set; }
    public DateOnly date { get; set; }
    public string username { get; set; }
    public string run_id { get; set; }
    public string session_uuid { get; set; }
    public int kart_id { get; set; }
}