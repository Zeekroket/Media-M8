# Media M8

**Media M8** *(pronounced “Media Mate”)* is a lightweight Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp), designed to make common media download tasks easier without requiring users to work directly with long command-line commands.

## Status

Media M8 is in early development. No public release has been published yet.

The original PowerShell implementation served as the proof of concept. The active application is now a C#/.NET Windows Forms app.

## Features

The current application includes:

- URL-based media downloads
- MP3 and video output modes
- Selectable MP3 quality
- Selectable video quality
- Playlist support
- Download archive support to avoid duplicate downloads
- Selectable output folders
- Optional opening of the output folder after completion
- Live process output in the application
- Automatic setup of required portable runtime tools
- Session logging under `logs/`

## Runtime dependencies

Media M8 uses separate third-party tools for download and media processing:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/) and ffprobe
- [Deno](https://deno.com/)

Media M8 keeps these tools in its local `tools/` directory. If they are missing, the application can download them from the official project releases and verify their SHA-256 checksums before using them.

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
