# Release Process

This document describes the intended release process for Media M8.

## 1. Choose the release type

Use [VERSIONING.md](../VERSIONING.md) to determine whether the change is:

- A revision.
- A feature update.
- A major version.

Update [CHANGELOG.md](../CHANGELOG.md) before release.

## 2. Build

Create the final Windows executable and any portable package files.

Do not sign intermediate builds.

## 3. Test

Test the final build before signing.

At minimum verify:

- The application starts.
- The expected Media M8 version is displayed.
- MP3 mode works.
- Video mode works.
- Playlist behavior works.
- Output-folder selection works.
- Download archive behavior works.
- External tool detection works as expected.

## 4. Sign

During private development, Media M8 may be signed with the private development certificate used by the project owner.

For public distribution, a self-signed development certificate should not be presented as publicly trusted code signing.

Never commit the signing certificate's private key, PFX file or password.

The executable must be signed only after it is final.

## 5. Verify

Verify that the Authenticode signature reports a valid status on the signing machine.

Do not modify the executable after signing.

## 6. Checksums

Generate a SHA-256 checksum for each published binary or archive.

Example:

```powershell
Get-FileHash .\MediaM8.exe -Algorithm SHA256
```

Publish the checksums with the release.

## 7. Package

A release package should clearly distinguish Media M8 files from third-party tools.

If third-party binaries are redistributed, review and include their applicable notices and license requirements.

## 8. Publish

Create a GitHub Release using the matching Media M8 version.

Include:

- Release notes.
- The signed Media M8 executable where applicable.
- Portable package where applicable.
- SHA-256 checksums.
- Any third-party notices required for bundled components.
