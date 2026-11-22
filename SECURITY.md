# Security Policy

## Supported versions

Media M8 is currently in early development. Until the first public release, only the latest code on the default branch is actively maintained.

## Reporting a vulnerability

Please avoid publishing security-sensitive details in a public issue before there has been a reasonable opportunity to investigate them.

When GitHub private vulnerability reporting is available for this repository, use that mechanism for security reports.

## Signing keys and secrets

Private signing material must never be committed to the repository.

This includes:

- PFX and P12 files.
- Private keys.
- Certificate passwords.
- Signing tokens.
- API keys and access tokens.

If a secret is accidentally committed, treat it as compromised and rotate or revoke it rather than relying only on deleting the file from Git history.
