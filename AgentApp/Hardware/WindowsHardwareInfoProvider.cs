using System.Management;
using System.Runtime.InteropServices;

namespace AgentApp.Hardware;

public sealed class WindowsHardwareInfoProvider : IHardwareInfoProvider
{
    public HardwareInfo Collect()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "WindowsHardwareInfoProvider работает только на Windows.");

        return new HardwareInfo(
            Cpu: CollectCpu(),
            RamModules: CollectRam(),
            Disks: CollectDisks(),
            Gpus: CollectGpus(),
            Motherboard: CollectMotherboard(),
            Os: CollectOs());
    }

    private static CpuInfo CollectCpu()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            return new CpuInfo(
                Name: obj["Name"]?.ToString()?.Trim() ?? "Unknown",
                Cores: Convert.ToInt32(obj["NumberOfCores"] ?? 0),
                Threads: Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0),
                MaxClockGhz: Convert.ToInt32(obj["MaxClockSpeed"] ?? 0) / 1000.0);
        }

        return new CpuInfo("Unknown", 0, 0, 0);
    }

    private static IReadOnlyList<RamModule> CollectRam()
    {
        var result = new List<RamModule>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Capacity, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            var typeCode = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
            result.Add(new RamModule(
                Manufacturer: obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown",
                SizeBytes: Convert.ToInt64(obj["Capacity"] ?? 0L),
                SpeedMhz: Convert.ToInt32(obj["Speed"] ?? 0),
                Type: MapMemoryType(typeCode)));
        }

        return result;
    }

    private static string MapMemoryType(int code) => code switch
    {
        20 => "DDR",
        21 => "DDR2",
        24 => "DDR3",
        26 => "DDR4",
        34 => "DDR5",
        _ => "Unknown"
    };

    private static IReadOnlyList<DiskInfo> CollectDisks()
    {
        var result = new List<DiskInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Model, Size, InterfaceType, MediaType FROM Win32_DiskDrive");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            result.Add(new DiskInfo(
                Model: obj["Model"]?.ToString()?.Trim() ?? "Unknown",
                SizeBytes: Convert.ToInt64(obj["Size"] ?? 0L),
                InterfaceType: obj["InterfaceType"]?.ToString() ?? "Unknown",
                MediaType: obj["MediaType"]?.ToString() ?? "Unknown"));
        }

        return result;
    }

    private static IReadOnlyList<GpuInfo> CollectGpus()
    {
        var result = new List<GpuInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            long? vram = obj["AdapterRAM"] is { } v ? Convert.ToInt64(v) : null;

            result.Add(new GpuInfo(
                Name: obj["Name"]?.ToString()?.Trim() ?? "Unknown",
                VramBytes: vram,
                DriverVersion: obj["DriverVersion"]?.ToString() ?? "Unknown"));
        }

        return result;
    }

    private static MotherboardInfo CollectMotherboard()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            return new MotherboardInfo(
                Manufacturer: obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown",
                Product: obj["Product"]?.ToString()?.Trim() ?? "Unknown",
                SerialNumber: obj["SerialNumber"]?.ToString()?.Trim() ?? "Unknown");
        }

        return new MotherboardInfo("Unknown", "Unknown", "Unknown");
    }

    private static OsInfo CollectOs()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Caption, Version, OSArchitecture, LastBootUpTime FROM Win32_OperatingSystem");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            var bootTime = obj["LastBootUpTime"] is { } b
                ? ManagementDateTimeConverter.ToDateTime(b.ToString())
                : DateTime.Now;

            return new OsInfo(
                Caption: obj["Caption"]?.ToString() ?? "Unknown",
                Version: obj["Version"]?.ToString() ?? "Unknown",
                Architecture: obj["OSArchitecture"]?.ToString() ?? RuntimeInformation.OSArchitecture.ToString(),
                MachineName: Environment.MachineName,
                Uptime: DateTime.Now - bootTime);
        }

        return new OsInfo("Unknown", "Unknown", RuntimeInformation.OSArchitecture.ToString(),
                          Environment.MachineName, TimeSpan.Zero);
    }
}