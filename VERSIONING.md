# Versioning

Media M8 deliberately distinguishes between a **revision**, a **feature update**, and a **major version**.

## Format

The project uses:

```text
v<major>[.<minor>] rev <revision>
```

Examples:

```text
v1 rev 0.1
v1 rev 0.2
v1.1 rev 0.1
v2 rev 0.1
```

## Revision

A revision is a small change that does not materially change the purpose or feature set of the current version.

Examples:

- Bug fixes.
- Small UI corrections.
- Text changes.
- Minor technical fixes.
- Small adjustments to an existing option.

A revision increments the revision number:

```text
v1 rev 0.1
v1 rev 0.2
v1 rev 0.3
```

## Feature update

A feature update adds or meaningfully changes user-facing functionality while remaining within the same major generation.

The minor version increases and the revision resets:

```text
v1 rev 0.3
v1.1 rev 0.1
```

Examples:

- A new download workflow.
- A new settings area.
- Integrated dependency setup.
- A meaningful new user-facing capability.

## Major version

A major version represents a substantial new generation of Media M8.

The major version increases and the revision resets:

```text
v1.4 rev 0.2
v2 rev 0.1
```

## External tool updates

Updating yt-dlp, FFmpeg, ffprobe or Deno by itself does not automatically change the Media M8 application version when those tools are managed independently.

If Media M8 itself changes to support, configure or integrate those tools differently, the normal revision/update rules apply.

## Release rule

Every published Media M8 executable must correspond to a documented version.

The release sequence is:

```text
build
→ test
→ finalize files
→ sign executable
→ verify signature
→ generate checksums
→ package release
→ publish
```

Do not modify an executable after signing it.
