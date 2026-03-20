using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PerTrac.Models;
using PerTrac.Services;
using SkiaSharp;

namespace PerTrac.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly HardwareMonitorService _monitor;
    private readonly DispatcherTimer _timer;
    private readonly int _maxDataPoints = 60;
    private bool _disposed;

    // CPU
    [ObservableProperty] private string _cpuName = "Loading...";
    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _cpuTemperature;
    [ObservableProperty] private double _cpuClockSpeed;
    [ObservableProperty] private double _cpuPower;
    [ObservableProperty] private int _cpuCoreCount;
    [ObservableProperty] private int _cpuThreadCount;
    [ObservableProperty] private string _cpuUsageText = "0%";
    [ObservableProperty] private string _cpuTempText = "0°C";

    // RAM
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private double _ramUsedGB;
    [ObservableProperty] private double _ramTotalGB;
    [ObservableProperty] private double _ramAvailableGB;
    [ObservableProperty] private string _ramUsageText = "0%";
    [ObservableProperty] private string _ramDetailText = "0 / 0 GB";

    // GPU
    [ObservableProperty] private string _gpuName = "Loading...";
    [ObservableProperty] private double _gpuUsage;
    [ObservableProperty] private double _gpuTemperature;
    [ObservableProperty] private double _gpuMemoryUsedMB;
    [ObservableProperty] private double _gpuMemoryTotalMB;
    [ObservableProperty] private double _gpuMemoryUsagePercent;
    [ObservableProperty] private double _gpuCoreClock;
    [ObservableProperty] private double _gpuMemoryClock;
    [ObservableProperty] private double _gpuFanSpeed;
    [ObservableProperty] private double _gpuPower;
    [ObservableProperty] private string _gpuUsageText = "0%";
    [ObservableProperty] private string _gpuTempText = "0°C";

    // Network
    [ObservableProperty] private string _networkAdapterName = "";
    [ObservableProperty] private double _downloadSpeedMBs;
    [ObservableProperty] private double _uploadSpeedMBs;
    [ObservableProperty] private string _downloadSpeedText = "0 MB/s";
    [ObservableProperty] private string _uploadSpeedText = "0 MB/s";
    [ObservableProperty] private string _totalDownloadedText = "0 GB";
    [ObservableProperty] private string _totalUploadedText = "0 GB";

    // System
    [ObservableProperty] private string _osInfo = "";
    [ObservableProperty] private string _computerName = "";
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _uptimeText = "";
    [ObservableProperty] private string _motherboardName = "";
    [ObservableProperty] private string _biosVersion = "";

    // Disks
    [ObservableProperty] private ObservableCollection<DiskViewModel> _disks = [];

    // Core usages
    [ObservableProperty] private ObservableCollection<CoreUsageViewModel> _coreUsages = [];

    // Settings
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private string _customColorHex = "#FF1A1A2E";
    [ObservableProperty] private SolidColorBrush _previewBrush = new(Color.FromRgb(0x1A, 0x1A, 0x2E));

    public bool IsDashboardVisible => !IsSettingsOpen;

    partial void OnIsSettingsOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDashboardVisible));
    }

    // Charts data
    private readonly ObservableCollection<ObservableValue> _cpuChartValues = [];
    private readonly ObservableCollection<ObservableValue> _ramChartValues = [];
    private readonly ObservableCollection<ObservableValue> _gpuChartValues = [];
    private readonly ObservableCollection<ObservableValue> _downloadChartValues = [];
    private readonly ObservableCollection<ObservableValue> _uploadChartValues = [];

    public ISeries[] CpuSeries { get; }
    public ISeries[] RamSeries { get; }
    public ISeries[] GpuSeries { get; }
    public ISeries[] NetworkSeries { get; }

    public Axis[] HiddenXAxes { get; } =
    [
        new Axis { ShowSeparatorLines = false, IsVisible = false }
    ];

    public Axis[] PercentYAxes { get; } =
    [
        new Axis { MinLimit = 0, MaxLimit = 100, ShowSeparatorLines = false, IsVisible = false }
    ];

    public Axis[] NetworkYAxes { get; } =
    [
        new Axis { MinLimit = 0, ShowSeparatorLines = false, IsVisible = false }
    ];

    public MainViewModel()
    {
        _monitor = new HardwareMonitorService();

        // Initialize chart data
        for (int i = 0; i < _maxDataPoints; i++)
        {
            _cpuChartValues.Add(new ObservableValue(0));
            _ramChartValues.Add(new ObservableValue(0));
            _gpuChartValues.Add(new ObservableValue(0));
            _downloadChartValues.Add(new ObservableValue(0));
            _uploadChartValues.Add(new ObservableValue(0));
        }

        var cpuColor = new SKColor(0, 180, 255);
        var ramColor = new SKColor(180, 0, 255);
        var gpuColor = new SKColor(0, 255, 128);
        var downloadColor = new SKColor(0, 200, 100);
        var uploadColor = new SKColor(255, 100, 50);

        CpuSeries = CreateLineSeries(_cpuChartValues, cpuColor);
        RamSeries = CreateLineSeries(_ramChartValues, ramColor);
        GpuSeries = CreateLineSeries(_gpuChartValues, gpuColor);
        NetworkSeries =
        [
            CreateLineSeries(_downloadChartValues, downloadColor)[0],
            CreateLineSeries(_uploadChartValues, uploadColor)[0]
        ];

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateAll();
        _timer.Start();

        UpdateAll();

        // Load saved settings
        var settings = SettingsService.Load();
        ApplyBackgroundColor(settings.BackgroundColor);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void ApplyBackgroundColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor)) return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            var brush = new SolidColorBrush(color);
            Application.Current.Resources["BackgroundBrush"] = brush;
            CustomColorHex = hexColor;
            PreviewBrush = new SolidColorBrush(color);
            SettingsService.Save(new AppSettings { BackgroundColor = hexColor });
        }
        catch { }
    }

    [RelayCommand]
    private void ApplyCustomColor()
    {
        ApplyBackgroundColor(CustomColorHex);
    }

    private static ISeries[] CreateLineSeries(ObservableCollection<ObservableValue> values, SKColor color)
    {
        return
        [
            new LineSeries<ObservableValue>
            {
                Values = values,
                Fill = new SolidColorPaint(color.WithAlpha(40)),
                Stroke = new SolidColorPaint(color, 2),
                GeometryFill = null,
                GeometryStroke = null,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                AnimationsSpeed = TimeSpan.FromMilliseconds(150)
            }
        ];
    }

    private void UpdateAll()
    {
        try
        {
            _monitor.Update();
            UpdateCpu();
            UpdateRam();
            UpdateGpu();
            UpdateNetwork();
            UpdateDisks();
            UpdateSystem();
        }
        catch
        {
            // Silently handle hardware read failures
        }
    }

    private void UpdateCpu()
    {
        var cpu = _monitor.GetCpuInfo();
        CpuName = cpu.Name;
        CpuUsage = cpu.TotalUsage;
        CpuTemperature = cpu.Temperature;
        CpuClockSpeed = cpu.ClockSpeed;
        CpuPower = cpu.Power;
        CpuCoreCount = cpu.CoreCount;
        CpuThreadCount = cpu.ThreadCount;
        CpuUsageText = $"{cpu.TotalUsage:F1}%";
        CpuTempText = $"{cpu.Temperature:F0}°C";

        AddChartValue(_cpuChartValues, cpu.TotalUsage);

        // Update core usages
        if (cpu.CoreUsages.Length > 0)
        {
            while (CoreUsages.Count < cpu.CoreUsages.Length)
                CoreUsages.Add(new CoreUsageViewModel());
            while (CoreUsages.Count > cpu.CoreUsages.Length)
                CoreUsages.RemoveAt(CoreUsages.Count - 1);

            for (int i = 0; i < cpu.CoreUsages.Length; i++)
            {
                CoreUsages[i].CoreIndex = i;
                CoreUsages[i].Usage = cpu.CoreUsages[i];
            }
        }
    }

    private void UpdateRam()
    {
        var ram = _monitor.GetRamInfo();
        RamUsagePercent = ram.UsagePercent;
        RamUsedGB = ram.UsedGB;
        RamTotalGB = ram.TotalGB;
        RamAvailableGB = ram.AvailableGB;
        RamUsageText = $"{ram.UsagePercent:F1}%";
        RamDetailText = $"{ram.UsedGB:F1} / {ram.TotalGB:F1} GB";

        AddChartValue(_ramChartValues, ram.UsagePercent);
    }

    private void UpdateGpu()
    {
        var gpu = _monitor.GetGpuInfo();
        GpuName = gpu.Name;
        GpuUsage = gpu.Usage;
        GpuTemperature = gpu.Temperature;
        GpuMemoryUsedMB = gpu.MemoryUsedMB;
        GpuMemoryTotalMB = gpu.MemoryTotalMB;
        GpuMemoryUsagePercent = gpu.MemoryUsagePercent;
        GpuCoreClock = gpu.CoreClock;
        GpuMemoryClock = gpu.MemoryClock;
        GpuFanSpeed = gpu.FanSpeed;
        GpuPower = gpu.Power;
        GpuUsageText = $"{gpu.Usage:F1}%";
        GpuTempText = $"{gpu.Temperature:F0}°C";

        AddChartValue(_gpuChartValues, gpu.Usage);
    }

    private void UpdateNetwork()
    {
        var networks = _monitor.GetNetworkInfo();
        var primary = networks.FirstOrDefault();
        if (primary == null) return;

        NetworkAdapterName = primary.AdapterName;
        DownloadSpeedMBs = primary.DownloadSpeedMBs;
        UploadSpeedMBs = primary.UploadSpeedMBs;
        DownloadSpeedText = FormatSpeed(primary.DownloadSpeedMBs);
        UploadSpeedText = FormatSpeed(primary.UploadSpeedMBs);
        TotalDownloadedText = $"{primary.TotalDownloadedGB:F2} GB";
        TotalUploadedText = $"{primary.TotalUploadedGB:F2} GB";

        AddChartValue(_downloadChartValues, primary.DownloadSpeedMBs);
        AddChartValue(_uploadChartValues, primary.UploadSpeedMBs);
    }

    private void UpdateDisks()
    {
        var diskInfos = _monitor.GetDiskInfo();

        while (Disks.Count < diskInfos.Count)
            Disks.Add(new DiskViewModel());
        while (Disks.Count > diskInfos.Count)
            Disks.RemoveAt(Disks.Count - 1);

        for (int i = 0; i < diskInfos.Count; i++)
        {
            var d = diskInfos[i];
            Disks[i].Name = string.IsNullOrEmpty(d.DriveLetter) ? d.Name : $"{d.DriveLetter} {d.Name}";
            Disks[i].UsagePercent = d.UsagePercent;
            Disks[i].UsedGB = d.UsedGB;
            Disks[i].TotalGB = d.TotalGB;
            Disks[i].FreeGB = d.FreeGB;
            Disks[i].Temperature = d.Temperature;
            Disks[i].ReadSpeedMBs = d.ReadSpeedMBs;
            Disks[i].WriteSpeedMBs = d.WriteSpeedMBs;
        }
    }

    private void UpdateSystem()
    {
        var sys = _monitor.GetSystemInfo();
        OsInfo = $"{sys.OsName} {sys.OsVersion}";
        ComputerName = sys.ComputerName;
        UserName = sys.UserName;
        UptimeText = FormatUptime(sys.Uptime);
        MotherboardName = sys.MotherboardName;
        BiosVersion = sys.BiosVersion;
    }

    private void AddChartValue(ObservableCollection<ObservableValue> collection, double value)
    {
        collection.RemoveAt(0);
        collection.Add(new ObservableValue(value));
    }

    private static string FormatSpeed(double mbPerSec)
    {
        if (mbPerSec >= 1)
            return $"{mbPerSec:F2} MB/s";
        return $"{mbPerSec * 1024:F1} KB/s";
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _monitor.Dispose();
        GC.SuppressFinalize(this);
    }
}

public partial class DiskViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private double _usagePercent;
    [ObservableProperty] private double _usedGB;
    [ObservableProperty] private double _totalGB;
    [ObservableProperty] private double _freeGB;
    [ObservableProperty] private double _temperature;
    [ObservableProperty] private double _readSpeedMBs;
    [ObservableProperty] private double _writeSpeedMBs;

    public string SpaceText => $"{UsedGB:F1} / {TotalGB:F1} GB";
    public string FreeText => $"{FreeGB:F1} GB free";
}

public partial class CoreUsageViewModel : ObservableObject
{
    [ObservableProperty] private int _coreIndex;
    [ObservableProperty] private double _usage;

    public string Label => $"Core {CoreIndex}";
    public string UsageText => $"{Usage:F0}%";
}
