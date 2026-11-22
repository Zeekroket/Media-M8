# Media M8

**Media M8** *(pronounced “Media Mate”)* is a lightweight Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp), designed to make common media download tasks easier without requiring users to work directly with long command-line commands.

## Status

Media M8 is in early development. No public release has been published yet.

## Features

The current prototype includes:

- URL-based media downloads
- MP3 and video output modes
- Selectable MP3 quality
- Selectable video quality
- Playlist support
- Download archive support to avoid duplicate downloads
- Selectable output folders
- Optional opening of the output folder after completion
- Live process output in the application

## Dependencies

Media M8 uses separate third-party tools for download and media processing:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/) and ffprobe
- [Deno](https://deno.com/)

These projects are not part of Media M8 and remain subject to their own licenses and distribution terms.

The long-term goal is to make dependency setup straightforward for end users while keeping the external tools clearly separated from the Media M8 application.

## Development

The repository is currently being prepared for the first public development release.

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
