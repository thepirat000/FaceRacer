namespace FaceRacer.DB.Entities;

public class UserInfo
{
    public string user_uuid { get; set; }
    public int user_id { get; set; }
    public string username { get; set; }
    public string full_name { get; set; }
    public int age { get; set; }
    public string profile_image_normal { get; set; }
    public string profile_url { get; set; }
}