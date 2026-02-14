namespace FaceRacer.DB.Entities;

public class RecordChange
{
    public Guid Id { get; set; }
    public int TrackId { get; set; }
    public string Period { get; set; }
    public DateOnly PeriodDate { get; set; }
    public DateTime NotificationDate { get; set; }
    public bool Processed { get; set; }

    public List<RankingDataDetails> RankingChanges { get; set; }
}