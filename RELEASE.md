# Release checklist

Use this checklist for each stable release. Keep test results, commit SHA and artifact digests in the release issue or pull request. A successful CI build or package preflight is not evidence of a successful manual upgrade.

## 1. Prepare the release

- [ ] Accepted roadmap issues are merged; unresolved release gates are explicitly recorded.
- [ ] Choose the version; align AppInfo.Version, project Version/FileVersion/AssemblyVersion, Inno Setup AppVersion default, and the default version in all three packaging scripts. Update README examples/current version and CHANGELOG.
- [ ] Work from the intended release commit with a clean working tree.
- [ ] Review CI results for the exact commit. Keep private signing keys on the maintainer's computer. Do not disable endpoint protection or bypass quarantine to run a test.

## 2. Build and validate

Run on the authorized Windows development machine with .NET 10 SDK and Inno Setup 6.3 or newer. Substitute the intended version for VERSION below.

    .\scripts\release-preflight.ps1 -Version VERSION -Stage Source
    .\scripts\build.ps1 -Sign
    .\scripts\package-release.ps1 -Version VERSION -Sign

Packaging checks source and application versions, runs regression tests, builds the installer and portable archive, verifies the installer version and requested signatures, reads the portable archive without executing or extracting it, checks exact expected files and the empty portable marker, verifies the internal manifest and compares the archived executable with the signed build. It writes MediaM8-VERSION-SHA256SUMS.txt beside the two release assets. Repeat the package preflight after any change to the files.

    .\scripts\release-preflight.ps1 -Version VERSION -Sign

- [ ] Signed package preflight passes.
- [ ] Record source commit and both asset digests.
- [ ] Confirm installer actually installs the intended executable and excludes portable.flag; preflight validates installer metadata and signature, not its embedded payload.
- [ ] Confirm signatures of the installed application and portable executable are valid.
- [ ] Confirm normal MP3/video download, playlist, archive skip, queue, STOP, exit confirmation and remembered preferences.
- [ ] Confirm tools, logs and settings remain separate for installed and portable copies; preserve any existing downloads.

## 3. Update verification

Use isolated authorized test installations and disposable data. Do not replace production executables with test fixtures or bypass security alerts. If a test is blocked, record it as blocked, not passed.

- [ ] Previous stable installed version: check for update, explicitly confirm, complete setup, verify new version, normal download and preserved installed data.
- [ ] Previous stable portable version: check for update, explicitly confirm, verify restart, new version, preserved marker/data and normal download.
- [ ] Verify cancellation leaves a usable application, and failure does not silently destroy user data.
- [ ] For changes to the updater itself, verify the new updater with an approved test environment or a later release; an upgrade from the previous version only tests the previous updater.
- [ ] For #78, specifically verify installer cancellation, interrupted/offline download, locked portable executable and restricted destination; recovery backup and marked temporary workspace cleanup after later startup. The PowerShell-based manual fixture was blocked by endpoint protection on 2026-11-26; do not repeat that fixture. CI success alone does not clear this gate.

## 4. Publish manually

- [ ] Check all pre-publication gates. Create the release/tag for the recorded commit only when the release is approved; upload exactly the setup EXE and portable ZIP. Do not upload private certificates or keys, development data or logs. No automatic publication.
- [ ] Compare uploaded asset names and SHA-256 digests with local release hashes.
- [ ] Verify release is stable, not draft/prerelease, and is shown as latest when intended.
- [ ] Run the actual previous-version installed and portable upgrade checks against the published release; record results. A published version is not considered fully verified until these pass.
- [ ] Record unresolved issues and any blocked tests; keep the roadmap release gate open until verified.

If the published release is needed to exercise the live upgrade check, perform the local package and installation gates first and explicitly record the remaining post-publication checks. Never label blocked or unrun tests as passed.
