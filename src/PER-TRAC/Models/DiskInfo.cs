namespace PerTrac.Models;

public class DiskInfo
{
    public string Name { get; set; } = "";
    public string DriveLetter { get; set; } = "";
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double FreeGB { get; set; }
    public double UsagePercent { get; set; }
    public double Temperature { get; set; }
    public double ReadSpeedMBs { get; set; }
    public double WriteSpeedMBs { get; set; }
}
