# Third-Party Notices

Media M8 is an independent project and uses external software to perform media download, processing and runtime tasks.

The Media M8 MIT license applies only to Media M8's own source code and original project files. Third-party software remains subject to its own licenses, copyright notices and distribution requirements.

## yt-dlp

Project: [yt-dlp](https://github.com/yt-dlp/yt-dlp)

yt-dlp is a separate project. Its source code and distributed executables may include components governed by different license terms. Always refer to the upstream project for the license terms that apply to the exact artifact being used.

Media M8 should invoke yt-dlp as a separate executable rather than incorporating yt-dlp source code into Media M8.

## FFmpeg and ffprobe

Project: [FFmpeg](https://ffmpeg.org/)

FFmpeg licensing depends on how a specific build was configured. Some builds are LGPL, while builds that include GPL components are distributed under the GPL.

Any Media M8 release that redistributes an FFmpeg build must document the exact build source and comply with the applicable FFmpeg license requirements.

## Deno

Project: [Deno](https://deno.com/)

Deno is a separate runtime used by yt-dlp for JavaScript-related functionality on supported sites. Refer to the upstream Deno project for the license applying to the distributed binary.

## Distribution policy

The preferred public distribution model for Media M8 is:

1. Distribute Media M8's own source code and application files.
2. Obtain third-party tools from their official upstream sources.
3. Verify downloaded files when official checksums or equivalent integrity information are available.
4. Keep third-party binaries clearly separated from Media M8's own files.
5. Include all notices required by an upstream license whenever a third-party binary is redistributed.

Third-party binaries should not be committed directly to the Media M8 source repository unless there is a specific distribution reason and the relevant license obligations have been reviewed.

## No affiliation

Media M8 is not affiliated with or endorsed by yt-dlp, FFmpeg, Deno, YouTube or Google.
