# Contributing

Contributions to Media M8 are welcome.

## Ground rules

- Keep changes focused and limited to one clear purpose.
- Use English for code comments, documentation, issues and pull requests.
- Do not include unrelated refactoring in a bug fix or small feature.
- Do not commit generated binaries or downloaded third-party tools unless the change specifically requires them.
- Do not commit credentials, signing material or other secrets.

## Development changes

For non-trivial changes, open an issue first so the scope can be agreed before implementation.

When submitting a pull request:

- Describe what changed and why.
- Explain how the change was tested.
- Update documentation when behavior changes.
- Add an entry under `Unreleased` in [CHANGELOG.md](CHANGELOG.md) when the change is user-visible.

Version numbers are assigned by the maintainer as part of the release process.

## Third-party code

Media M8 relies on external projects such as yt-dlp, FFmpeg and Deno. Do not copy third-party source code into this repository without first confirming the applicable license and attribution requirements.

## Security issues

Do not disclose security vulnerabilities in a public issue. See [SECURITY.md](SECURITY.md).
