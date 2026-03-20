namespace PerTrac.Models;

public class CpuInfo
{
    public string Name { get; set; } = "Unknown";
    public double TotalUsage { get; set; }
    public double Temperature { get; set; }
    public double[] CoreUsages { get; set; } = [];
    public double[] CoreTemperatures { get; set; } = [];
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double ClockSpeed { get; set; }
    public double Power { get; set; }
}
