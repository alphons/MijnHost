# Mijn.Host DNS Updater – ACME Challenge Tool

A minimal, fast .NET console application to create or update a single `_acme-challenge` TXT record via the Mijn.Host API v2.

Perfect as a dns-01 challenge hook for Let’s Encrypt tools (Certbot, win-acme, acme.sh, Posh-ACME, etc.).

## Features

- Pure top-level console app – no DI container, no Host, no Singleton
- API key safely loaded from `appsettings.json`
- Fully async `HttpClient` with proper error handling
- Strongly-typed records
- Uses PATCH to add or replace exactly one DNS record
- Tiny binary, zero external runtime dependencies beyond .NET runtime

## Usage

```bash
MijnHost.exe example.com L5f...your-acme-challenge-token
