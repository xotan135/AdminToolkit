using System.IO;

namespace AdminToolkit.Desktop.Services;

public sealed class ToolkitConfiguration
{
    private readonly Dictionary<string, Dictionary<string, string>> _values = new(StringComparer.OrdinalIgnoreCase);

    private ToolkitConfiguration(string filePath) => FilePath = filePath;

    public string FilePath { get; }
    public string LogDirectory => Expand(Get("Logs", "AuditDirectory", @"%LOCALAPPDATA%\AdminToolkit\Logs"));
    public string DellScanDirectory => Expand(Get("Logs", "DellScanDirectory", @"%LOCALAPPDATA%\AdminToolkit\Logs\Dell"));
    public string PowerShellExecutable => Expand(Get("Commands", "PowerShellExecutable", "powershell.exe"));
    public string DellCommandUpdate => Get("Commands", "DellCommandUpdate", @"%ProgramFiles%\Dell\CommandUpdate\dcu-cli.exe");
    public int MaximumConcurrency => GetPositiveInteger("Safety", "MaximumConcurrency", 12);
    public int PingTimeoutMilliseconds => GetPositiveInteger("Safety", "PingTimeoutMilliseconds", 2_000);
    public bool DarkMode
    {
        get => bool.TryParse(Get("Appearance", "DarkMode", "false"), out var enabled) && enabled;
        set => Set("Appearance", "DarkMode", value.ToString().ToLowerInvariant());
    }

    public string Get(string section, string key, string fallback = "") =>
        _values.TryGetValue(section, out var entries) && entries.TryGetValue(key, out var value) ? value : fallback;

    public static ToolkitConfiguration Load()
    {
        var explicitPath = Environment.GetEnvironmentVariable("ADMIN_TOOLKIT_CONFIG");
        var candidates = new[]
        {
            explicitPath,
            Path.Combine(AppContext.BaseDirectory, "AdminToolkit.ini"),
            Path.Combine(Environment.CurrentDirectory, "AdminToolkit.ini")
        }.Where(path => !string.IsNullOrWhiteSpace(path)).Cast<string>();

        var path = candidates.FirstOrDefault(File.Exists) ?? Path.Combine(AppContext.BaseDirectory, "AdminToolkit.ini");
        var configuration = new ToolkitConfiguration(path);
        if (File.Exists(path)) configuration.Read(path);
        return configuration;
    }

    public void Set(string section, string key, string value)
    {
        if (!_values.TryGetValue(section, out var entries))
            _values[section] = entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        entries[key] = value;
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        using var writer = new StreamWriter(FilePath, false);
        foreach (var section in _values)
        {
            writer.WriteLine($"[{section.Key}]");
            foreach (var entry in section.Value) writer.WriteLine($"{entry.Key}={entry.Value}");
            writer.WriteLine();
        }
    }

    private void Read(string path)
    {
        var section = "General";
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                continue;
            }
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;
            Set(section, line[..separator].Trim(), line[(separator + 1)..].Trim());
        }
    }

    private static string Expand(string value) => Environment.ExpandEnvironmentVariables(value);

    private int GetPositiveInteger(string section, string key, int fallback) =>
        int.TryParse(Get(section, key), out var value) && value > 0 ? value : fallback;
}
