namespace FaceRacer.Shared.Dto;

public class SessionChartData
{
    public List<string> labels { get; set; }
    public int min { get; set; }
    public int max { get; set; }
    public List<SessionChartDataset> datasets { get; set; }
}