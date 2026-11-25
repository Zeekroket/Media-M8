# Media M8 Help

**Media M8** *(pronounced “Media Mate”)* is a simple Windows app for downloading media from supported websites.

## First-run setup

When you open Media M8 for the first time, it checks whether the supporting components are available. If any are missing, choose **Set up** to get Media M8 ready before pasting a link. Once setup is complete, the prompt will not appear again unless components are missing.

You do not need to install or configure these yourself. Media M8 downloads the missing components from the project release sources it is configured to use and verifies the downloaded files with SHA-256 checksums before placing them in its `tools` folder (see Application data below).

The components are:

- **yt-dlp** — handles supported websites and media downloads;
- **FFmpeg / ffprobe** — handles media conversion and inspection;
- **Deno** — provides the JavaScript runtime used by supported extractors when needed.

Choosing **Cancel** leaves the missing components uninstalled. You can complete setup later by clicking **DOWNLOAD** with a valid link. If setup fails, you can try again the same way.

You can check or refresh these components later through **Tools → Check download tools** and **Tools → Update download tools**.

## Application data

The portable ZIP includes a file named `portable.flag`. Keep that file next to `MediaM8.exe`. Tools, logs and appearance settings are stored in the portable folder.

Without the marker, Media M8 uses installed mode and stores tools, logs and settings under `%LOCALAPPDATA%\Media M8`. The download destination remains whatever you choose in **Save to**.

When upgrading an older portable copy in place, keep its existing tools and logs and add an empty `portable.flag` beside the executable before starting the new version. Alternatively, extract the new portable ZIP over the existing copy, keeping your data. Older versions stored appearance settings under `%LOCALAPPDATA%\Media M8\settings.json`. To retain that appearance in a portable copy, copy that file next to the executable if there is no local settings file yet. Installed mode keeps using the existing per-user settings.

Existing data is not automatically moved or deleted.

## Quick start

1. Copy the link to the video, audio or playlist you want.
2. Paste it into **Link to download**.
3. Wait for the **Preview** to appear so you can check that Media M8 found the right media.
4. Choose **MP3** or **Video**.
5. Choose the quality you want.
6. Check the **Save to** folder.
7. Click **DOWNLOAD**.

Media M8 shows progress at the bottom of the window. After a successful download, **Download completed.** remains visible and **Open folder** opens the folder used for that download.

When several downloads run in the queue, the final status shows the results. **Open folder** opens the folder of the last completed download. **Open folder when finished** still opens the folder automatically if selected.

## Playlists

**Download playlist** is off by default.

Turn it on when you intentionally want the full playlist, such as an album or a playlist with multiple items.

Media M8 prepares the playlist first so it knows how many items there are. It then downloads the items one at a time.

Leave **Download playlist** off when you only want the single video or audio item from the pasted link.

## Queue several downloads

Start the first download normally with **DOWNLOAD**.

While it is running, paste another link and choose its settings. The button changes to **ADD TO QUEUE**. Each queued item keeps the settings that were selected when it was added.

Media M8 downloads exactly one queue item at a time and automatically starts the next waiting item when the current one finishes.

Open **Queue → Open Queue** to see the current session queue. The queue window shows each item as Pending, Downloading, Completed, Failed or Stopped.

From the queue window you can:

- retry a selected Failed or Stopped item with **Retry**; a new Pending item is added at the end using the original settings, while the original attempt stays in the history;
- move a selected Pending item earlier or later using **Move up** or **Move down**;
- remove a selected Pending item;
- clear Completed, Failed and Stopped items;
- choose **Pause Queue** to let the current download finish but prevent the next Pending item from starting;
- choose **Resume Queue** to continue with the next Pending item.

Pause Queue does not pause the download that is already running. Retrying and reordering while paused do not resume the queue. The active download cannot be moved.

The queue only exists for the current Media M8 session.

## Stop a download

Click **STOP** while a download is active.

STOP ends the current queue item, including the rest of that item's playlist. If other queue items are waiting, Media M8 continues with the next one.


## Skip files you already downloaded

When **Skip items I've downloaded before** is checked, Media M8 skips items it has already completed. Media M8 keeps this history in a small file named `downloaded.txt` in the download folder.

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
- **Check download tools** — checks whether Media M8's local download tools are present and working, and reports updates where they can be compared reliably.
- **Update download tools** — downloads fresh verified copies from the official project releases. Nothing updates automatically in the background.
- **Appearance → Match System / Light / Dark** — follows the Windows app theme by default, or lets you force Light or Dark. Your choice is remembered for the next launch.
- **Open logs** — opens the folder containing Media M8 session logs.

**Presets**
- **MP3 — Best quality**
- **MP3 — 320 kbps**
- **Video — 1080p**
- **Video — Best quality**

Choosing a preset only updates the existing download type and quality controls. You can still change those controls manually afterwards.

**Help**
- **Media M8 Help** — opens this page.
- **Check for Updates** — checks GitHub Releases for a newer stable version. Media M8 never installs an update automatically.
- **About Media M8** — shows version and project information.

## Project

Media M8 is open source:

https://github.com/Zeekroket/Media-M8
