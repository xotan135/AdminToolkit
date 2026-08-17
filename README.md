# Admin Toolkit

Admin Toolkit is a Windows desktop application for running approved administrative tasks across one or more computers. It is being migrated from a legacy PowerShell workflow into a compiled .NET application with visible results, cancellation, safety controls, and audit logging.

## Current capabilities

- Light and dark grey themes with a persistent toggle
- Multi-computer input with duplicate removal
- Parallel online-status checks with a concurrency limit
- Live progress, per-computer results, and cancellation
- Thirteen-action migration catalog with risk labels
- JSON audit logs
- Private INI-based configuration for internal paths and computer names

Only **Check online status** is enabled in the current release. Higher-impact actions remain disabled until their validation, confirmation, logging, and protected-computer rules are implemented.

## Requirements

- Windows 10 or Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) when using the framework-dependent build
- Appropriate network access for the computers being managed

## Configuration

1. Copy `AdminToolkit.ini.example` to `AdminToolkit.ini`.
2. Replace the placeholder paths and names with values for your environment.
3. Keep `AdminToolkit.ini` private. It is excluded by `.gitignore`.

The application checks these locations in order:

1. The path in the `ADMIN_TOOLKIT_CONFIG` environment variable
2. `AdminToolkit.ini` beside the executable
3. `AdminToolkit.ini` in the current working directory

The INI is plaintext. Use it for internal paths and computer names, not passwords, API keys, or other secrets.

## Build and run

```powershell
dotnet restore .\AdminToolkit.Desktop\AdminToolkit.Desktop.csproj
dotnet build .\AdminToolkit.Desktop\AdminToolkit.Desktop.csproj --configuration Release
dotnet run --project .\AdminToolkit.Desktop\AdminToolkit.Desktop.csproj
```

## Create a Windows release

```powershell
dotnet publish .\AdminToolkit.Desktop\AdminToolkit.Desktop.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

GitHub Actions produces a self-contained `win-x64` artifact on pushes, pull requests, and version tags matching `v*`.

## Audit logs

Audit records are JSON files written to `[Logs] AuditDirectory` in the INI. The default example uses `%LOCALAPPDATA%\AdminToolkit\Logs`.

## Security

Review [SECURITY.md](SECURITY.md) before deploying administrative actions. The application does not change WinRM TrustedHosts or store credentials.

## Project status

This is an early foundation release. See [CHANGELOG.md](CHANGELOG.md) for completed milestones.

## License

Licensed under the [MIT License](LICENSE).
