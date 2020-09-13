Beat Singer
===========

[![Video](Video.gif)](https://youtu.be/95n0W1IpHFs)

Displays song lyrics in-game.

> **Note**: Some songs loaded using [BeatSaver Downloader](https://github.com/andruzzzhka/BeatSaverDownloader) have invalid metadata, and thus won't be recognized.  
> If you want to use them anyway, you'll have to edit their metadata manually so that the song and artist names are valid.

## Installation
After installing the [custom song loader](https://github.com/xyonico/BeatSaberSongLoader), drop
[`BeatSinger.dll`](https://github.com/6A/BeatSinger/releases) into the `Beat Saber/Plugins` directory.

## Usage
- When lyrics for a song can be found, the message "lyrics found" will be shown at the beginning of the level song.
- Lyrics are automatically looked up using [musixmatch](https://www.musixmatch.com); you do not need to add them yourself.
- You can press the `Right Thumbstick` (or trackpad on the Vive)
  to toggle lyrics in-game. The key can be changed in
  `Beat Saber\UserData\modprefs.ini`, by setting `ToggleKeyCode`
  to a valid `KeyCode` value.

### Custom lyrics
Even though lyrics can be looked up automatically, it can be interesting to have
custom lyrics either for quality, or for offline access.

BeatSinger will thus load lyrics in the following order:
1. File named `lyrics.json` in the directory of the song.
2. File named `lyrics.srt` in the directory of the song.
3. Online resolution.
   * Songs fetched from online services will be saved to the song folder as `lyrics.json` if `SaveFetchedLyrics` is enabled in the config file.

#### JSON files must have one the following formats:
* Local lyrics files can specify a `timeOffset` and/or `timeScale` to change the timing for all lyrics
  * `timeOffset` will change all `time` and `end` (if used) by the specified number of seconds. Default is `0`.
  * `timeScale` will multiply all `time` and `end` (if used) by the given value. This is useful for songs that have been sped up or slowed down by the mapper. Default is `1`.
```json
{
  "timeOffset" : 1.5,
  "timeScale" : 0.98,
  "subtitles" : [
    {
      "text" : "Never gonna give you up",
      "time" : 10.00,
      "end": 11.10
    },
    {
      "text" : "Never gonna let you down",
      "time" : 11.24
    }
  ]
}
```
```json
[
  { "text": "Never gonna give you up", "time": 10.00, "end": 11.10 },
  { "text": "Never gonna let you down", "time": 11.24 },
  "..."
]
```

#### SRT files must have the following format:
```srt
1
00:00:22,791 --> 00:00:26,229
Never gonna give you up.
Never gonna let you down.

2
00:00:30,023 --> 00:00:32,272
Never gonna run away...
And desert you.

...
```
