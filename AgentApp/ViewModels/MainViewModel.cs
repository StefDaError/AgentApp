using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AgentApp.Hardware;
using AgentApp.Serialization;

namespace AgentApp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IHardwareInfoProvider _provider;
    private readonly HardwareJsonWriter _jsonWriter;

    public MainViewModel()
    {
        _provider = new WindowsHardwareInfoProvider();
        _jsonWriter = new HardwareJsonWriter();

        RefreshCommand = new RelayCommand(_ => Refresh());
        SendCommand = new RelayCommand(_ => Send(), _ => !string.IsNullOrWhiteSpace(AdminUrl));

        Refresh();
    }

    public ObservableCollection<InfoSection> Sections { get; } = new();

    private string _jsonPath = string.Empty;
    public string JsonPath
    {
        get => _jsonPath;
        set { _jsonPath = value; OnPropertyChanged(); }
    }

    private string _adminUrl = string.Empty;
    public string AdminUrl
    {
        get => _adminUrl;
        set { _adminUrl = value; OnPropertyChanged(); }
    }

    private string _status = "Готово";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SendCommand { get; }

    private void Refresh()
    {
        try
        {
            var info = _provider.Collect();
            Sections.Clear();
            BuildSections(info);
            JsonPath = _jsonWriter.Write(info);
            Status = "Данные обновлены, JSON сохранён";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
    }

    private void Send()
    {
        if (!Uri.TryCreate(AdminUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https" &&
             uri.Scheme != "ws" && uri.Scheme != "wss"))
        {
            MessageBox.Show("URL должен быть вида http(s)://... или ws(s)://...",
                            "Некорректный URL",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Status = $"URL принят: {uri}. Отправка появится после реализации транспорта.";
    }

    private void BuildSections(HardwareInfo info)
    {
        Sections.Add(new InfoSection("CPU", new[]
        {
            new InfoRow("Модель", info.Cpu.Name),
            new InfoRow("Ядра", $"{info.Cpu.Cores} физических / {info.Cpu.Threads} логических"),
            new InfoRow("Частота", $"{info.Cpu.MaxClockGhz:F2} GHz"),
        }));

        var ramRows = new List<InfoRow>
        {
            new("Всего", FormatBytes(info.TotalRamBytes)),
            new("Планок", info.RamModules.Count.ToString()),
        };
        for (int i = 0; i < info.RamModules.Count; i++)
        {
            var m = info.RamModules[i];
            ramRows.Add(new InfoRow($"[{i + 1}]",
                $"{m.Type} {FormatBytes(m.SizeBytes)} @ {m.SpeedMhz} MHz ({m.Manufacturer})"));
        }
        Sections.Add(new InfoSection("RAM", ramRows));

        var diskRows = info.Disks.Count == 0
            ? new List<InfoRow> { new("—", "нет данных") }
            : info.Disks.Select(d => new InfoRow(
                d.Model,
                $"{FormatBytes(d.SizeBytes)} [{d.InterfaceType}, {d.MediaType}]")).ToList();
        Sections.Add(new InfoSection("Диски", diskRows));

        var gpuRows = info.Gpus.Count == 0
            ? new List<InfoRow> { new("—", "нет данных") }
            : info.Gpus.Select(g => new InfoRow(
                g.Name,
                $"VRAM: {(g.VramBytes is { } v ? FormatBytes(v) : "n/a")}, драйвер: {g.DriverVersion}")).ToList();
        Sections.Add(new InfoSection("GPU", gpuRows));

        Sections.Add(new InfoSection("Материнская плата", new[]
        {
            new InfoRow("Производитель", info.Motherboard.Manufacturer),
            new InfoRow("Модель", info.Motherboard.Product),
            new InfoRow("Серийный №", info.Motherboard.SerialNumber),
        }));

        Sections.Add(new InfoSection("ОС", new[]
        {
            new InfoRow("Система", info.Os.Caption),
            new InfoRow("Версия", info.Os.Version),
            new InfoRow("Архитектура", info.Os.Architecture),
            new InfoRow("Имя ПК", info.Os.MachineName),
            new InfoRow("Uptime", info.Os.Uptime.ToString(@"dd\.hh\:mm\:ss")),
        }));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
        return $"{size:F2} {units[u]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}