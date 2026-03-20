using System;

namespace PerTrac.Models;

public class SystemInfo
{
    public string OsName { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string ComputerName { get; set; } = "";
    public string UserName { get; set; } = "";
    public TimeSpan Uptime { get; set; }
    public string MotherboardName { get; set; } = "";
    public string BiosVersion { get; set; } = "";
}
