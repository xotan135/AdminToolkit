using AdminToolkit.Desktop.Models;

namespace AdminToolkit.Desktop.Services;

public static class ActionCatalog
{
    public static IReadOnlyList<AdminAction> All { get; } =
    [
        new("status", "Check online status", "Checks whether each computer responds without making remote changes.", ActionRisk.ReadOnly, true),
        new("wsus", "Windows updates via WSUS", "Installs approved WSUS updates, optionally allowing a reboot.", ActionRisk.High, false),
        new("report", "Windows Update report now", "Requests a Windows Update reporting cycle.", ActionRisk.Medium, false),
        new("temp", "Delete temporary files", "Removes aged temporary files from approved locations.", ActionRisk.High, false),
        new("dell-scan", "Dell Command Update scan", "Scans Dell computers for applicable driver and firmware updates.", ActionRisk.ReadOnly, false),
        new("dell-apply", "Dell Command Update apply", "Installs applicable Dell driver and firmware updates.", ActionRisk.High, false),
        new("reboot", "Reboot", "Forcibly restarts approved computers after explicit confirmation.", ActionRisk.High, false),
        new("shutdown", "Shut down", "Forcibly powers off approved computers after explicit confirmation.", ActionRisk.High, false),
        new("wpkg", "Start WPKG", "Starts the WPKG service on remote computers.", ActionRisk.Medium, false),
        new("gpupdate", "Group Policy update", "Forces a Group Policy refresh on remote computers.", ActionRisk.Medium, false),
        new("inventory", "Collect inventory", "Collects hardware, operating-system, network, and software details.", ActionRisk.ReadOnly, false),
        new("vnc", "Update VNC settings", "Deploys approved encryption files and restarts the VNC service.", ActionRisk.High, false),
        new("microsoft-update", "Windows updates via Microsoft Update", "Installs Microsoft updates, optionally allowing a reboot.", ActionRisk.High, false)
    ];
}
