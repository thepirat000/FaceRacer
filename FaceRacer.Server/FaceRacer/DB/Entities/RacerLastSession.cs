using Microsoft.EntityFrameworkCore;

namespace FaceRacer.DB.Entities;

[PrimaryKey(nameof(UserId))]
public class RacerLastSession
{
    public int UserId { get; set; } 
    public string SessionId { get; set; } 
    public DateOnly SessionDate { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
}