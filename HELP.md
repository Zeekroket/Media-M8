# Media M8 Help

**Media M8** *(pronounced “Media Mate”)* is a simple Windows app for downloading media from supported websites.

## Quick start

1. Copy the link to the video, audio or playlist you want.
2. Paste it into **Media URL**.
3. Wait for the **Preview** to appear so you can check that Media M8 found the right media.
4. Choose **MP3** or **Video (MKV)**.
5. Choose the quality you want.
6. Check the **Save to** folder.
7. Click **DOWNLOAD**.

Media M8 shows progress at the bottom of the window.

## Playlists

Keep **Download playlist** checked when you want the full playlist.

Media M8 prepares the playlist first so it knows how many items there are. It then downloads the items one at a time.

If you only want the single video from a link that also contains a playlist, clear **Download playlist** before downloading.

## Stop a download

Click **STOP** while a download is active.

Media M8 stops the current download and does not continue with the next playlist item. You can start another download afterwards.

## Skip files you already downloaded

When **Skip previously downloaded items (downloaded.txt)** is checked, Media M8 keeps a small history file in the download folder and skips items it has already completed.

## Preview

For supported links, Media M8 can show:

- title;
- uploader or channel, when available;
- duration for a single item;
- playlist item count;
- thumbnail, when available.

Some websites provide less information than others, so not every field will always be shown.

## If a download fails

Media M8 shows a plain-language error where possible.

Choose **Details...** in the error window if you need the technical information. You can also open the session logs through **Tools → Open logs**.

## Supported websites

Media M8 uses yt-dlp for website support. yt-dlp supports many websites, and that list changes over time.

Current yt-dlp supported-sites list:

https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md

A listed site can temporarily stop working when that website changes. Some sites not listed explicitly may still work through generic or embedded-media support.

## Useful menu options

**File**
- **Open download folder** — opens the current Save to folder.
- **Exit** — closes Media M8.

**Tools**
- **Open logs** — opens the folder containing Media M8 session logs.

**Help**
- **Media M8 Help** — opens this page.
- **About Media M8** — shows version and project information.

## Project

Media M8 is open source:

https://github.com/Zeekroket/Media-M8
