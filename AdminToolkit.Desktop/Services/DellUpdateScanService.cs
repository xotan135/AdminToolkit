using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AdminToolkit.Desktop.Models;

namespace AdminToolkit.Desktop.Services;

public sealed partial class DellUpdateScanService(
    int maximumConcurrency,
    string powerShellExecutable,
    string remoteDcuPath,
    string logDirectory)
{
    private const string ScanScript = """
        $target = $env:ADMIN_TOOLKIT_TARGET
        $configuredPath = $env:ADMIN_TOOLKIT_DCU_PATH
        Invoke-Command -ComputerName $target -ArgumentList $configuredPath -ErrorAction Stop -ScriptBlock {
            param($dcuPath)
            $expandedPath = [Environment]::ExpandEnvironmentVariables($dcuPath)
            if (-not (Test-Path -LiteralPath $expandedPath)) {
                throw "Dell Command Update was not found at the configured path: $expandedPath"
            }
            $reportDirectory = Join-Path $env:ProgramData 'Dell\AdminToolkit'
            New-Item -Path $reportDirectory -ItemType Directory -Force -ErrorAction Stop | Out-Null
            $reportPath = Join-Path $reportDirectory ("DellScan-{0}.xml" -f ([guid]::NewGuid().ToString('N')))
            try {
                $consoleOutput = (& $expandedPath /scan "-report=$reportPath" 2>&1 | Out-String).Trim()
                $exitCode = $LASTEXITCODE
                $updates = @()
                if (Test-Path -LiteralPath $reportPath) {
                    [xml]$report = Get-Content -LiteralPath $reportPath -Raw
                    $nodes = @($report.SelectNodes("//*[local-name()='update' or local-name()='Update']"))
                    $updates = @($nodes | ForEach-Object {
                        $properties = [ordered]@{}
                        foreach ($attribute in $_.Attributes) { $properties[$attribute.LocalName] = $attribute.Value }
                        foreach ($child in $_.ChildNodes) {
                            if ($child.NodeType -eq [System.Xml.XmlNodeType]::Element) { $properties[$child.LocalName] = $child.InnerText.Trim() }
                        }
                        if ($properties.Count -eq 0) { $properties['Description'] = $_.InnerText.Trim() }
                        [pscustomobject]$properties
                    })
                }
                [pscustomobject]@{
                    ExitCode = $exitCode
                    UpdateCount = $updates.Count
                    Updates = $updates
                    ConsoleOutput = $consoleOutput
                } | ConvertTo-Json -Depth 8 -Compress
            }
            finally {
                Remove-Item -LiteralPath $reportPath -Force -ErrorAction SilentlyContinue
            }
        }
        """;

    public async Task ScanAsync(
        IReadOnlyList<string> computerNames,
        IProgress<ComputerResult> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(logDirectory);
        using var gate = new SemaphoreSlim(maximumConcurrency);
        var tasks = computerNames.Select(async computerName =>
        {
            await gate.WaitAsync(cancellationToken);
            try { progress.Report(await ScanOneAsync(computerName, cancellationToken)); }
            finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    private async Task<ComputerResult> ScanOneAsync(string computerName, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;
        if (!ComputerNamePattern().IsMatch(computerName))
            return new ComputerResult(computerName, ResultStatus.Error, "Computer name contains unsupported characters.", started, DateTimeOffset.Now);

        var timestamp = started.ToString("yyyyMMdd-HHmmss");
        var logPath = Path.Combine(logDirectory, $"{computerName}-{timestamp}-DellScan.log");
        try
        {
            var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(ScanScript));
            var startInfo = new ProcessStartInfo
            {
                FileName = powerShellExecutable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-EncodedCommand");
            startInfo.ArgumentList.Add(encodedScript);
            startInfo.Environment["ADMIN_TOOLKIT_TARGET"] = computerName;
            startInfo.Environment["ADMIN_TOOLKIT_DCU_PATH"] = remoteDcuPath;

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start()) throw new InvalidOperationException("PowerShell could not be started.");
            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            try { await process.WaitForExitAsync(cancellationToken); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw;
            }

            var output = await standardOutput;
            var error = await standardError;
            await File.WriteAllTextAsync(logPath, BuildLog(computerName, process.ExitCode, output, error), CancellationToken.None);
            if (process.ExitCode != 0)
                return new ComputerResult(computerName, ResultStatus.Error, $"Scan failed. See {Path.GetFileName(logPath)}.", started, DateTimeOffset.Now);

            var scan = ParseScanResult(output);
            if (scan.ExitCode != 0)
                return new ComputerResult(computerName, ResultStatus.Error, $"Dell scan exited with code {scan.ExitCode}.", started, DateTimeOffset.Now, scan.ConsoleOutput);

            var message = scan.UpdateCount == 0 ? "Up to date — no updates available." : $"{scan.UpdateCount} update(s) available.";
            return new ComputerResult(computerName, ResultStatus.Success, message, started, DateTimeOffset.Now, FormatResultDetails(scan));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(logPath, BuildLog(computerName, -1, string.Empty, exception.ToString()), CancellationToken.None);
            return new ComputerResult(computerName, ResultStatus.Error, $"{exception.Message} See {Path.GetFileName(logPath)}.", started, DateTimeOffset.Now);
        }
    }

    private static string BuildLog(string computerName, int exitCode, string output, string error) =>
        $"Computer: {computerName}{Environment.NewLine}Finished: {DateTimeOffset.Now:O}{Environment.NewLine}Exit code: {exitCode}{Environment.NewLine}{Environment.NewLine}Output:{Environment.NewLine}{output}{Environment.NewLine}Errors:{Environment.NewLine}{error}";

    private static ScanResult ParseScanResult(string output)
    {
        var jsonLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault(line => line.TrimStart().StartsWith('{'));
        if (jsonLine is null) throw new InvalidDataException("Dell scan did not return an update report.");
        using var document = JsonDocument.Parse(jsonLine);
        var root = document.RootElement;
        var updates = new List<IReadOnlyDictionary<string, string>>();
        if (root.TryGetProperty("Updates", out var updateArray) && updateArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var update in updateArray.EnumerateArray())
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in update.EnumerateObject()) values[property.Name] = property.Value.ToString();
                updates.Add(values);
            }
        }
        return new ScanResult(
            root.TryGetProperty("ExitCode", out var exitCode) ? exitCode.GetInt32() : -1,
            root.TryGetProperty("UpdateCount", out var count) ? count.GetInt32() : updates.Count,
            updates,
            root.TryGetProperty("ConsoleOutput", out var console) ? console.GetString() ?? string.Empty : string.Empty);
    }

    private static string FormatUpdates(IEnumerable<IReadOnlyDictionary<string, string>> updates) =>
        string.Join(Environment.NewLine, updates.Select((update, index) =>
        {
            var preferredKeys = new[] { "Name", "Title", "PackageName", "Type", "Category", "Version", "Severity", "Urgency" };
            var values = preferredKeys.Where(update.ContainsKey).Select(key => $"{key}: {update[key]}").ToArray();
            if (values.Length == 0) values = update.Select(entry => $"{entry.Key}: {entry.Value}").Take(5).ToArray();
            return $"{index + 1}. {string.Join(" · ", values)}";
        }));

    private static string FormatResultDetails(ScanResult scan)
    {
        var updateDetails = FormatUpdates(scan.Updates);
        if (string.IsNullOrWhiteSpace(updateDetails)) updateDetails = "No applicable updates were listed in the Dell report.";
        var commandOutput = string.IsNullOrWhiteSpace(scan.ConsoleOutput) ? "Dell Command Update returned no console text." : scan.ConsoleOutput.Trim();
        return $"AVAILABLE UPDATES ({scan.UpdateCount}){Environment.NewLine}{updateDetails}{Environment.NewLine}{Environment.NewLine}DELL COMMAND UPDATE OUTPUT{Environment.NewLine}{commandOutput}";
    }

    private sealed record ScanResult(int ExitCode, int UpdateCount, IReadOnlyList<IReadOnlyDictionary<string, string>> Updates, string ConsoleOutput);

    [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9.-]{0,251}[A-Za-z0-9])?$")]
    private static partial Regex ComputerNamePattern();
}
