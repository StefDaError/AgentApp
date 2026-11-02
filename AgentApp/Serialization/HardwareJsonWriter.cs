using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentApp.Hardware;

namespace AgentApp.Serialization;

public sealed class HardwareJsonWriter
{
    private readonly string _outputDirectory;
    private readonly string _fileName;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public HardwareJsonWriter(string? outputDirectory = null, string fileName = "hardware.json")
    {
        _outputDirectory = outputDirectory ?? AppContext.BaseDirectory;
        _fileName = fileName;
    }

    public string Write(HardwareInfo hardware)
    {
        var json = JsonSerializer.Serialize(hardware, Options);
        Directory.CreateDirectory(_outputDirectory);
        var fullPath = Path.Combine(_outputDirectory, _fileName);
        File.WriteAllText(fullPath, json);
        return fullPath;
    }
}