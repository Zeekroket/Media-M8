# Contributing to Media M8

Thanks for your interest in Media M8.

## Language

Repository content, issues, pull requests, code comments and documentation should be written in English.

## Before contributing

- Keep changes focused on one clear purpose.
- Avoid unrelated refactoring in a small fix.
- Follow the project's versioning rules in [VERSIONING.md](VERSIONING.md).
- Update [CHANGELOG.md](CHANGELOG.md) when a user-visible change is introduced.
- Do not commit third-party binaries unless the change explicitly requires it and licensing has been reviewed.

## Security and signing material

Never commit:

- PFX or P12 files.
- Private keys.
- Certificate passwords.
- Signing passwords or tokens.
- Secrets or API credentials.
- Personal access tokens.

Public certificates may be shared when needed for verification, but private signing material must remain private.

## Pull requests

A pull request should explain:

- What changed.
- Why it changed.
- How it was tested.
- Whether the change is a revision, feature update or major-version change.

## External projects

Media M8 depends on separate third-party projects. Do not copy code from yt-dlp, FFmpeg, Deno or another project into Media M8 without first confirming that the relevant license permits it and that the required notices are included.
