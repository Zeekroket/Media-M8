# Changelog

All notable changes to Media M8 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- C#/.NET 10 Windows Forms application replacing the PowerShell proof-of-concept runtime.
- Self-contained single-file win-x64 publishing so end users do not need PowerShell or a separate .NET runtime.
- Direct process execution for yt-dlp from the application.
- Native asynchronous runtime tool installation for yt-dlp, FFmpeg, ffprobe and Deno with SHA-256 verification.
- Persistent per-session diagnostic logging under `logs/`.
- Dedicated Windows AppUserModelID so the Media M8 window has its own taskbar identity.

### Changed

- Runtime dependencies are resolved from Media M8's local `tools` directory only.
- The original PowerShell implementation is no longer part of the application runtime.

No public release has been published yet.
