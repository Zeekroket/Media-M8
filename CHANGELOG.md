# Changelog

All notable changes to Media M8 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- Hid the technical log behind an optional Details control so the normal window stays focused on status and progress.
- Reordered the quality controls to Audio quality → Video quality and made both quality lists run from Best to lower quality.

## [0.1.0] - 2026-11-22

### Added

- C#/.NET 10 Windows Forms application replacing the PowerShell proof-of-concept runtime.
- Self-contained single-file win-x64 publishing so end users do not need PowerShell or a separate .NET runtime.
- Direct process execution for yt-dlp from the application.
- Native asynchronous runtime tool installation for yt-dlp, FFmpeg, ffprobe and Deno with SHA-256 verification.
- Persistent per-session diagnostic logging under `logs/`.
- Dedicated Windows AppUserModelID so the Media M8 window has its own taskbar identity.
- Machine-readable yt-dlp progress tracking for download percentage, byte counts, speed, ETA and processing phase.
- Visible download progress with plain-language phase, percentage, speed and estimated time remaining.
- Playlists are prepared before downloading and then processed one item at a time so Media M8 knows the current item and total count.
- STOP button for safely ending the active download process and returning Media M8 to an idle state.
- Friendly download failure messages with a clear next action and expandable technical details.
- Automatic text preview for pasted media links, showing title plus uploader/channel and duration or playlist item count when available.
- Thumbnail image in the single-media preview when the source provides a usable image.
- Main menu bar with File, Tools, Presets, Queue and Help headings, plus working shortcuts to the download folder, logs and Exit.
- Dedicated user Help page, F1 shortcut and About Media M8 dialog with version, project link and third-party acknowledgements.
- Manual Check for Updates action using the latest stable GitHub Release, with no automatic installation.
- Download tool health check and manual verified refresh action for yt-dlp, FFmpeg/ffprobe and Deno.
- Simple built-in presets for MP3 Best, MP3 320 kbps, Video 1080p and Video Best.
- Session-only single-worker download queue with per-item setting snapshots and automatic next-item processing.
- Queue window with live item states, remove-pending, clear-finished and Pause/Resume Queue controls.
- Reassuring first-run setup wording with a Learn more link to dependency and verification information.
- Expanded About Media M8 screen with app icon, project links, author/copyright, MIT licence and third-party acknowledgements.
- Persistent Match System/Light/Dark appearance setting covering the main window, menus, queue, progress/status area and Media M8-owned dialogs.

### Changed

- Runtime dependencies are resolved from Media M8's local `tools` directory only.
- The original PowerShell implementation is no longer part of the application runtime.

[Unreleased]: https://github.com/Zeekroket/Media-M8/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Zeekroket/Media-M8/releases/tag/v0.1.0
