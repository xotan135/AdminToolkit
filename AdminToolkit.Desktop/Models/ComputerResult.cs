namespace AdminToolkit.Desktop.Models;

public enum ResultStatus { Online, Offline, Error }

public sealed record ComputerResult(string ComputerName, ResultStatus Status, string Message, DateTimeOffset StartedAt, DateTimeOffset FinishedAt)
{
    public string DurationText => $"{(FinishedAt - StartedAt).TotalMilliseconds:N0} ms";
}
