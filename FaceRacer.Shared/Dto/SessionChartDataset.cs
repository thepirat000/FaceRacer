namespace FaceRacer.Shared.Dto;

public class SessionChartDataset
{
    public string label { get; set; }
    public List<double> data { get; set; }
    public string backgroundColor { get; set; }
    public string borderColor { get; set; }
    public string pointBorderColor { get; set; }
    public string pointBackgroundColor { get; set; }
    public int lineTension { get; set; }
    public int borderWidth { get; set; }
}