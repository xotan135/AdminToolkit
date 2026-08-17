using System.Text.Json;
using System.IO;
using AdminToolkit.Desktop.Models;

namespace AdminToolkit.Desktop.Services;

public sealed class AuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _logDirectory;

    public AuditLogService(string logDirectory) => _logDirectory = logDirectory;

    public async Task WriteAsync(AdminAction action, IEnumerable<ComputerResult> results, CancellationToken cancellationToken, bool wasCancelled = false)
    {
        Directory.CreateDirectory(_logDirectory);
        var timestamp = DateTimeOffset.Now;
        var entry = new { Timestamp = timestamp, Environment.UserName, Environment.MachineName, Action = action.Id, action.Name, WasCancelled = wasCancelled, Results = results.ToArray() };
        await using var stream = File.Create(Path.Combine(_logDirectory, $"{timestamp:yyyyMMdd-HHmmss}-{action.Id}.json"));
        await JsonSerializer.SerializeAsync(stream, entry, JsonOptions, cancellationToken);
    }
}
