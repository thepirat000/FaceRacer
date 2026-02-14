namespace FaceRacer.Shared.Dto;

public class SessionChartDataResult
{
    public bool tooltips { get; set; }
    public bool reverse { get; set; }
    public string step_size { get; set; }
    public string max_ticks_limit { get; set; }
    public string x_label { get; set; }
    public string y_label { get; set; }
    public SessionChartData data { get; set; }
}