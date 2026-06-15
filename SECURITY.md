# Security Policy

## Supported Versions

MrRexoRR is currently pre-release software. Only the latest `main` branch is actively maintained until the first tagged release.

## Reporting Security or Safety Issues

For security-sensitive or trading-safety issues, please do not open a public issue with exploit details or account-specific information.

Report privately to the repository owner through GitHub.

Examples of sensitive issues:

- unintended order submission,
- duplicate order submission,
- incorrect account selection,
- incorrect flatten behavior,
- OCO failure modes,
- unsafe defaults,
- exposure of local account or machine information.

## Scope

This project does not store broker credentials and does not connect directly to brokers. It runs inside NinjaTrader 8 and uses NinjaTrader APIs.

Users are responsible for their NinjaTrader installation, broker connection, data feed, account selection, and platform security.
