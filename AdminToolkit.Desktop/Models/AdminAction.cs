namespace AdminToolkit.Desktop.Models;

public enum ActionRisk { ReadOnly, Medium, High }

public sealed record AdminAction(string Id, string Name, string Description, ActionRisk Risk, bool IsAvailable)
{
    public string RiskLabel => Risk switch
    {
        ActionRisk.ReadOnly => "Read only",
        ActionRisk.Medium => "Changes remote state",
        ActionRisk.High => "High impact",
        _ => "Unknown risk"
    };
}
