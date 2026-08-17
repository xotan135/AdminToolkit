# Security policy

## Sensitive configuration

Never commit `AdminToolkit.ini`, internal server names, protected-computer lists, credentials, tokens, private keys, or production log files. Use `AdminToolkit.ini.example` as the public template.

The INI file is not encrypted. Credentials should use Windows-managed authentication or a dedicated secret store rather than configuration files.

## Administrative safety

New remote actions should remain disabled until they include:

- explicit input validation;
- a documented risk classification;
- protected-computer checks;
- confirmation for high-impact operations;
- bounded concurrency and cancellation;
- per-computer results and audit records; and
- tests covering failure and partial-success behavior.

The application must not automatically broaden WinRM TrustedHosts.

## Reporting vulnerabilities

Please report suspected vulnerabilities privately to the repository owner. Do not open a public issue containing credentials, internal hostnames, network paths, or exploit details.
