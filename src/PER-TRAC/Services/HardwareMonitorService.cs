using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using PerTrac.Models;

namespace PerTrac.Services;

public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private bool _disposed;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true
        };

        _computer.Open();
    }

    public void Update()
    {
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Update();
        }
    }

    public CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null) return info;

        cpu.Update();
        info.Name = cpu.Name;

        var coreUsages = new List<double>();
        var coreTemps = new List<double>();

        foreach (var sensor in cpu.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Load when sensor.Name == "CPU Total":
                    info.TotalUsage = sensor.Value ?? 0;
                    break;
                case SensorType.Load when sensor.Name.StartsWith("CPU Core"):
                    coreUsages.Add(sensor.Value ?? 0);
                    break;
                case SensorType.Temperature when sensor.Name == "CPU Package" || sensor.Name == "Core Average":
                    info.Temperature = sensor.Value ?? 0;
                    break;
                case SensorType.Temperature when sensor.Name.StartsWith("CPU Core"):
                    coreTemps.Add(sensor.Value ?? 0);
                    break;
                case SensorType.Clock when sensor.Name.Contains("Core") && !sensor.Name.Contains("Bus"):
                    if (info.ClockSpeed == 0)
                        info.ClockSpeed = sensor.Value ?? 0;
                    break;
                case SensorType.Power when sensor.Name == "CPU Package":
                    info.Power = sensor.Value ?? 0;
                    break;
            }
        }

        info.CoreUsages = coreUsages.ToArray();
        info.CoreTemperatures = coreTemps.ToArray();
        info.CoreCount = coreUsages.Count > 0 ? coreUsages.Count : Environment.ProcessorCount;
        info.ThreadCount = Environment.ProcessorCount;

        if (info.Temperature == 0 && coreTemps.Count > 0)
            info.Temperature = coreTemps.Average();

        return info;
    }

    public RamInfo GetRamInfo()
    {
        var info = new RamInfo();
        var memory = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (memory == null) return info;

        memory.Update();

        foreach (var sensor in memory.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Load when sensor.Name == "Memory":
                    info.UsagePercent = sensor.Value ?? 0;
                    break;
                case SensorType.Data when sensor.Name == "Memory Used":
                    info.UsedGB = sensor.Value ?? 0;
                    break;
                case SensorType.Data when sensor.Name == "Memory Available":
                    info.AvailableGB = sensor.Value ?? 0;
                    break;
            }
        }

        info.TotalGB = info.UsedGB + info.AvailableGB;
        return info;
    }

    public GpuInfo GetGpuInfo()
    {
        var info = new GpuInfo();
        var gpu = _computer.Hardware.FirstOrDefault(h =>
            h.HardwareType == HardwareType.GpuNvidia ||
            h.HardwareType == HardwareType.GpuAmd ||
            h.HardwareType == HardwareType.GpuIntel);

        if (gpu == null) return info;

        gpu.Update();
        info.Name = gpu.Name;

        foreach (var sensor in gpu.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Load when sensor.Name == "GPU Core":
                    info.Usage = sensor.Value ?? 0;
                    break;
                case SensorType.Temperature when sensor.Name == "GPU Core":
                    info.Temperature = sensor.Value ?? 0;
                    break;
                case SensorType.SmallData when sensor.Name == "GPU Memory Used":
                    info.MemoryUsedMB = sensor.Value ?? 0;
                    break;
                case SensorType.SmallData when sensor.Name == "GPU Memory Total":
                    info.MemoryTotalMB = sensor.Value ?? 0;
                    break;
                case SensorType.Load when sensor.Name == "GPU Memory":
                    info.MemoryUsagePercent = sensor.Value ?? 0;
                    break;
                case SensorType.Clock when sensor.Name == "GPU Core":
                    info.CoreClock = sensor.Value ?? 0;
                    break;
                case SensorType.Clock when sensor.Name == "GPU Memory":
                    info.MemoryClock = sensor.Value ?? 0;
                    break;
                case SensorType.Fan when sensor.Name.Contains("GPU"):
                    info.FanSpeed = sensor.Value ?? 0;
                    break;
                case SensorType.Power when sensor.Name.Contains("GPU"):
                    info.Power = sensor.Value ?? 0;
                    break;
            }
        }

        return info;
    }

    public List<DiskInfo> GetDiskInfo()
    {
        var disks = new List<DiskInfo>();
        var storageDevices = _computer.Hardware.Where(h => h.HardwareType == HardwareType.Storage);

        foreach (var storage in storageDevices)
        {
            storage.Update();
            var disk = new DiskInfo { Name = storage.Name };

            foreach (var sensor in storage.Sensors)
            {
                switch (sensor.SensorType)
                {
                    case SensorType.Temperature:
                        disk.Temperature = sensor.Value ?? 0;
                        break;
                    case SensorType.Throughput when sensor.Name.Contains("Read"):
                        disk.ReadSpeedMBs = (sensor.Value ?? 0) / (1024 * 1024);
                        break;
                    case SensorType.Throughput when sensor.Name.Contains("Write"):
                        disk.WriteSpeedMBs = (sensor.Value ?? 0) / (1024 * 1024);
                        break;
                }
            }

            disks.Add(disk);
        }

        // Add logical drive info
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var existing = disks.FirstOrDefault();
            if (existing != null && string.IsNullOrEmpty(existing.DriveLetter))
            {
                existing.DriveLetter = drive.Name;
                existing.TotalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                existing.FreeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                existing.UsedGB = existing.TotalGB - existing.FreeGB;
                existing.UsagePercent = existing.TotalGB > 0 ? (existing.UsedGB / existing.TotalGB) * 100 : 0;
            }
            else
            {
                disks.Add(new DiskInfo
                {
                    Name = drive.VolumeLabel,
                    DriveLetter = drive.Name,
                    TotalGB = drive.TotalSize / (1024.0 * 1024 * 1024),
                    FreeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                    UsedGB = (drive.TotalSize - drive.AvailableFreeSpace) / (1024.0 * 1024 * 1024),
                    UsagePercent = drive.TotalSize > 0
                        ? ((drive.TotalSize - drive.AvailableFreeSpace) / (double)drive.TotalSize) * 100
                        : 0
                });
            }
        }

        if (disks.Count == 0)
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                disks.Add(new DiskInfo
                {
                    Name = drive.VolumeLabel,
                    DriveLetter = drive.Name,
                    TotalGB = drive.TotalSize / (1024.0 * 1024 * 1024),
                    FreeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                    UsedGB = (drive.TotalSize - drive.AvailableFreeSpace) / (1024.0 * 1024 * 1024),
                    UsagePercent = drive.TotalSize > 0
                        ? ((drive.TotalSize - drive.AvailableFreeSpace) / (double)drive.TotalSize) * 100
                        : 0
                });
            }
        }

        return disks;
    }

    public List<NetworkInfo> GetNetworkInfo()
    {
        var networks = new List<NetworkInfo>();
        var networkHardware = _computer.Hardware.Where(h => h.HardwareType == HardwareType.Network);

        foreach (var adapter in networkHardware)
        {
            adapter.Update();
            var net = new NetworkInfo { AdapterName = adapter.Name };

            foreach (var sensor in adapter.Sensors)
            {
                switch (sensor.SensorType)
                {
                    case SensorType.Throughput when sensor.Name.Contains("Download"):
                        net.DownloadSpeedMBs = (sensor.Value ?? 0) / (1024 * 1024);
                        break;
                    case SensorType.Throughput when sensor.Name.Contains("Upload"):
                        net.UploadSpeedMBs = (sensor.Value ?? 0) / (1024 * 1024);
                        break;
                    case SensorType.Data when sensor.Name.Contains("Download"):
                        net.TotalDownloadedGB = sensor.Value ?? 0;
                        break;
                    case SensorType.Data when sensor.Name.Contains("Upload"):
                        net.TotalUploadedGB = sensor.Value ?? 0;
                        break;
                }
            }

            networks.Add(net);
        }

        return networks;
    }

    public Models.SystemInfo GetSystemInfo()
    {
        var info = new Models.SystemInfo
        {
            OsName = Environment.OSVersion.Platform.ToString(),
            OsVersion = Environment.OSVersion.VersionString,
            ComputerName = Environment.MachineName,
            UserName = Environment.UserName,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };

        var motherboard = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard);
        if (motherboard != null)
        {
            info.MotherboardName = motherboard.Name;
            var bios = motherboard.SubHardware.FirstOrDefault();
            if (bios != null)
                info.BiosVersion = bios.Name;
        }

        return info;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _computer.Close();
        GC.SuppressFinalize(this);
    }
}
