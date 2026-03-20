namespace PerTrac.Models;

public class NetworkInfo
{
    public string AdapterName { get; set; } = "";
    public double DownloadSpeedMBs { get; set; }
    public double UploadSpeedMBs { get; set; }
    public double TotalDownloadedGB { get; set; }
    public double TotalUploadedGB { get; set; }
}
