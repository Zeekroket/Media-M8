# Media M8

**Media M8** *(pronounced “Media Mate”)* is a lightweight Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp), designed to make common media download tasks easier without requiring users to work directly with command-line tools.

The core workflow is intentionally simple:

**Paste a link → choose MP3 or video → choose quality → Download.**

## Status

Current version: **0.1.0**

Media M8 is under active development and should be considered pre-1.0 software.

Published builds and release notes are available on the [Releases](https://github.com/Zeekroket/Media-M8/releases) page.

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
- Queue window with Pending / Downloading / Completed / Failed / Stopped states
- Remove-pending and clear-finished queue actions
- Pause Queue / Resume Queue without interrupting the current download
- Download archive support to avoid duplicate downloads
- Selectable output folder
- Optional opening of the output folder after completion
- Live download progress, speed and estimated time remaining
- STOP button for the active download
- Friendly user-facing error messages with optional technical details
- Automatic setup of required portable runtime tools
- Download-tool health check and manual verified tool refresh
- Manual Media M8 update check through GitHub Releases
- Match System, Light and Dark appearance modes
- F1 user help and an About Media M8 screen
- Session logging under `logs/`

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

Media M8 keeps these tools in its local `tools/` directory. If they are missing, the application can download them from the configured official project release sources and verify their SHA-256 checksums before using them.

These projects are not part of Media M8 and remain subject to their own licenses and distribution terms.

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

To create the versioned win-x64 release ZIP after building:

```powershell
.\scripts\package.ps1 -Version 0.1.0
```

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
