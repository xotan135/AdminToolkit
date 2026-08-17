# Contributing

## Development

1. Install the .NET 8 SDK on Windows.
2. Copy `AdminToolkit.ini.example` to `AdminToolkit.ini` for local configuration.
3. Build with `dotnet build .\AdminToolkit.Desktop\AdminToolkit.Desktop.csproj`.
4. Do not enable a remote action until it meets the controls in `SECURITY.md`.

Before opening a pull request, verify that the repository contains no internal server names, computer names, network shares, credentials, generated logs, or real INI files.

Keep each administrative action isolated behind a service interface so it can be validated and tested independently of the desktop interface.
