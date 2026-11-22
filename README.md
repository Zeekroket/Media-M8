# Media M8

**Media M8** is a lightweight Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp).

The project exists for people who want the power of yt-dlp without repeatedly typing or copying long command-line commands, while keeping the workflow local and transparent.

## Project status

Media M8 is in early development. The current development baseline is **v1 rev 0.1**.

There is no public release yet.

## Goals

- Provide a simple Windows interface for common yt-dlp tasks.
- Keep downloads local instead of relying on third-party online downloader websites.
- Make the underlying tools and actions clear to the user.
- Minimize setup work for end users.
- Keep third-party tools separate and identifiable.
- Make the source code easy to inspect.

## Current development scope

The prototype currently covers:

- Download from a supplied media URL.
- MP3 or video output.
- Selectable MP3 quality.
- Selectable video quality.
- Playlist handling.
- Download archive support to avoid downloading the same item twice.
- Selectable output folder.
- Optional opening of the output folder when a job completes.
- A live output/log area.

## External tools

Media M8 is designed to work with external tools such as:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/) and ffprobe
- [Deno](https://deno.com/)

These are separate third-party projects and are **not covered by the Media M8 MIT license**.

A future setup flow is planned to make it easy for users to obtain the required tools from their official sources. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Versioning

Media M8 distinguishes between revisions and updates.

Examples:

- `v1 rev 0.1` → initial development baseline
- `v1 rev 0.2` → small fix or minor adjustment
- `v1.1 rev 0.1` → feature update
- `v2 rev 0.1` → major new version

See [VERSIONING.md](VERSIONING.md) for the full policy.

## Releases and trust

Source code is kept in this repository so users can inspect how Media M8 works.

Release builds should be finalized before signing. During development, Windows builds may use a private development signing certificate. A self-signed development certificate is not a substitute for publicly trusted code signing.

Signing keys, PFX files, passwords and other private key material must never be committed to this repository.

## Responsible use

Media M8 is a user interface for third-party media tooling. Users are responsible for ensuring that their use complies with applicable law, copyright, platform terms and any permissions required for the content they access.

Media M8 is not affiliated with or endorsed by YouTube, Google, yt-dlp, FFmpeg or Deno.

## License

Media M8 is licensed under the [MIT License](LICENSE).

Third-party components retain their own licenses and copyright notices.
