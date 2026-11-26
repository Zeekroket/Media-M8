# Media M8

**Media M8** *(pronounced “Media Mate”)* is a lightweight Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp), designed to make common media download tasks easier without requiring users to work directly with command-line tools.

The core workflow is intentionally simple:

**Paste a link → choose MP3 or video → choose quality → Download.**

## Status

Current version: **0.3.0**

Media M8 is under active development and should be considered pre-1.0 software.

Published builds and release notes are available on the [Releases](https://github.com/Zeekroket/Media-M8/releases) page.

## Choose your distribution

Starting with v0.2.0, releases include two Windows x64 packages:

- `MediaM8-<version>-win-x64-setup.exe`: Windows installer, Start menu shortcut and uninstall support.
- `MediaM8-<version>-win-x64-portable.zip`: extract and run without installing. Keep the portable marker beside the executable.

## Features

Media M8 currently includes:

- URL-based media downloads from sites supported by yt-dlp
- Automatic media preview with title and available metadata
- Thumbnail preview for single media items when available
- MP3 and MKV video output modes
- Selectable MP3 and video quality
- Built-in presets for common MP3 and video choices
- Playlist preparation and sequential playlist downloading
- Session-only single-worker download queue
- Queue window with Pending / Downloading / Completed / With skips / Skipped / Failed / Stopped states
- Remove pending, clear finished, retry failed/stopped items and reorder pending downloads
- Pause Queue / Resume Queue without interrupting the current download
- Download archive support to avoid duplicate downloads
- Remembered output folder, download format and quality preferences
- Optional opening of the output folder after completion
- Live download progress, speed and estimated time remaining
- STOP button for the active download
- Friendly user-facing error messages with optional technical details
- One-step first-run setup of required runtime tools
- Download-tool health check and manual verified tool refresh
- Weekly automatic update notification, manual update check and guided verified updates
- Match System, Light and Dark appearance modes
- F1 user help and an About Media M8 screen
- Session logging under `logs/` in the selected application data location
- DPI-aware layouts and keyboard navigation

## Help

User documentation is available in [HELP.md](HELP.md).

Useful project links:

- [Releases](https://github.com/Zeekroket/Media-M8/releases)
- [Issue tracker](https://github.com/Zeekroket/Media-M8/issues)
- [Changelog](CHANGELOG.md)

## Runtime dependencies

Media M8 uses separate third-party tools for download and media processing:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/) and ffprobe
- [Deno](https://deno.com/)

Media M8 keeps these tools in the `tools/` directory of the active application data location. If they are missing, the application can download them from the configured official project release sources and verify their SHA-256 checksums before using them.

These projects are not part of Media M8 and remain subject to their own licenses and distribution terms.

## Application data

The portable ZIP and development builds include `portable.flag`. With this marker beside the executable, Media M8 keeps tools, logs and settings in the same folder. Without the marker, it stores mutable application data under `%LOCALAPPDATA%\Media M8`, independently of the executable location.

See [Application data in Help](HELP.md#application-data) for upgrading existing portable copies safely.

## Application runtime

Media M8 is built as a self-contained .NET 10 Windows application for x64 Windows.

The published `MediaM8.exe` does not require PowerShell and does not require a separately installed .NET runtime on the end user's computer.

## Development

Source code lives in `src/MediaM8/`. Build and packaging helpers live in `scripts/`.

Building requires the .NET 10 SDK.

```powershell
.\scripts\build.ps1
```

To build and sign with the configured development code-signing certificate:

```powershell
.\scripts\build.ps1 -Sign
```

Run the deterministic regression tests (no live downloads or real user settings are used):

```powershell
.\scripts\test.ps1
```

Release packaging validates the source and built executable versions, runs the regression tests, then checks both packages and writes their SHA-256 digests. Manual Windows GUI and update smoke tests remain required. Follow [RELEASE.md](RELEASE.md) before publishing either package.

To package both release assets after building, install [Inno Setup](https://jrsoftware.org/isdl.php) 6.3 or newer (the build helper automatically locates Inno Setup 6, or accepts an explicit `-CompilerPath` to ISCC.exe).

```powershell
.\scripts\build.ps1 -Sign
.\scripts\package-release.ps1 -Version 0.3.0 -Sign
```

For an unsigned local test, omit `-Sign`. To create just the portable ZIP, run `.\scripts\package.ps1 -Version 0.3.0`. The release process must include both the installer and the portable ZIP.

The PowerShell scripts under `scripts/` are developer build helpers only; PowerShell is not part of the Media M8 application runtime.

Versioning follows [Semantic Versioning](https://semver.org/):

- PATCH: backwards-compatible fixes
- MINOR: backwards-compatible feature additions
- MAJOR: incompatible or substantial changes

Before version 1.0.0, the project should be considered under active development.

## Responsible use

Media M8 is a user interface for third-party media tooling. Users are responsible for ensuring that their use complies with applicable law, copyright, platform terms and any permissions required for the content they access.

Media M8 is not affiliated with or endorsed by YouTube, Google, yt-dlp, FFmpeg or Deno.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

See [SECURITY.md](SECURITY.md).

## License

Media M8 is licensed under the [MIT License](LICENSE).
