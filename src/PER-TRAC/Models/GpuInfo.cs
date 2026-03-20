namespace PerTrac.Models;

public class GpuInfo
{
    public string Name { get; set; } = "Unknown";
    public double Usage { get; set; }
    public double Temperature { get; set; }
    public double MemoryUsedMB { get; set; }
    public double MemoryTotalMB { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double CoreClock { get; set; }
    public double MemoryClock { get; set; }
    public double FanSpeed { get; set; }
    public double Power { get; set; }
}
