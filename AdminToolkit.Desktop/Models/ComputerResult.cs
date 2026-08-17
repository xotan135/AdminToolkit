namespace AdminToolkit.Desktop.Models;

public enum ResultStatus { Success, Online, Offline, Error }

public sealed record ComputerResult(string ComputerName, ResultStatus Status, string Message, DateTimeOffset StartedAt, DateTimeOffset FinishedAt, string Details = "")
{
    public string DurationText => $"{(FinishedAt - StartedAt).TotalMilliseconds:N0} ms";
}
