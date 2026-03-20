# PER-TRAC - Performance Tracker

Real-time PC performance monitoring application built with C# / WPF / .NET 8.

## Features

- **CPU Monitoring** - Total & per-core usage, temperature, clock speed, power consumption
- **RAM Monitoring** - Used/available memory with real-time percentage chart
- **GPU Monitoring** - Usage, temperature, VRAM, clock speeds, fan speed, power
- **Disk Monitoring** - Space usage, read/write speeds, temperatures per drive
- **Network Monitoring** - Download/upload speeds with live chart, total transferred data
- **System Information** - OS, motherboard, BIOS, uptime, computer name

## Tech Stack

- **C# / .NET 8** - Application framework
- **WPF** - UI framework with dark theme
- **LibreHardwareMonitor** - Hardware sensor data
- **LiveCharts2** - Real-time charts
- **CommunityToolkit.Mvvm** - MVVM pattern support
- **Inno Setup** - Windows installer

## Requirements

- Windows 10/11 (x64)
- .NET 8 Runtime
- Administrator privileges (required for hardware sensor access)

## Build

```bash
cd src/PER-TRAC
dotnet restore
dotnet build -c Release
dotnet publish -c Release
```

## Install

Build the installer using Inno Setup:
1. Install [Inno Setup](https://jrsoftware.org/isinfo.php)
2. Open `installer/setup.iss`
3. Compile (Ctrl+F9)
4. Find the installer in `installer/Output/`

## Architecture

```
src/PER-TRAC/
├── Models/          # Data models (CpuInfo, RamInfo, GpuInfo, etc.)
├── Services/        # Hardware monitoring service (LibreHardwareMonitor wrapper)
├── ViewModels/      # MVVM ViewModels with CommunityToolkit.Mvvm
├── Views/           # WPF XAML views (dark theme dashboard)
├── Converters/      # Value converters for UI bindings
└── Assets/          # Icons and resources
```

## Note

The application requires **administrator privileges** to read hardware sensors (CPU temperature, GPU data, disk SMART, etc.) via LibreHardwareMonitor.
