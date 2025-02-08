namespace HomeAutomation.Web.Data;

public class BoilerOptions
{
    public Uri RtspUrl { get; set; }
    public int GaugeRadius { get; set; }
    public int MinAngle { get; set; }
    public int MaxAngle { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int MinNeedleSize { get; set; }
}
