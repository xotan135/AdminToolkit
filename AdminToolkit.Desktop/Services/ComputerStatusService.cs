using System.Net.NetworkInformation;
using AdminToolkit.Desktop.Models;

namespace AdminToolkit.Desktop.Services;

public sealed class ComputerStatusService(int maximumConcurrency)
{
    public async Task CheckAsync(IReadOnlyList<string> computerNames, IProgress<ComputerResult> progress, CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(maximumConcurrency);
        var tasks = computerNames.Select(async computerName =>
        {
            await gate.WaitAsync(cancellationToken);
            try { progress.Report(await CheckOneAsync(computerName, cancellationToken)); }
            finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    private static async Task<ComputerResult> CheckOneAsync(string computerName, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(computerName, 2_000).WaitAsync(cancellationToken);
            var online = reply.Status == IPStatus.Success;
            return new ComputerResult(computerName, online ? ResultStatus.Online : ResultStatus.Offline,
                online ? $"Responded from {reply.Address}." : $"No response ({reply.Status}).", started, DateTimeOffset.Now);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return new ComputerResult(computerName, ResultStatus.Error, exception.Message, started, DateTimeOffset.Now); }
    }
}
