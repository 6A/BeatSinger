using BeatSinger.Helpers;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace BeatSinger
{
    /// <summary>
    ///   Defines the main component of BeatSinger, which displays lyrics on loaded songs.
    /// </summary>
    public sealed class LyricsComponent : MonoBehaviour
    {
        private const BindingFlags NON_PUBLIC_INSTANCE = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameSongController songController;
        private FlyingTextSpawner textSpawner;
        private AudioTimeSyncController audio;

        public IEnumerator Start()
        {
            // The goal now is to find the clip this scene will be playing.
            // For this, we find the single root gameObject (which is the gameObject
            // to which we are attached),
            // then we get its GameSongController to find the audio clip,
            // and its FlyingTextSpawner to display the lyrics.

            if (Plugin.config.VerboseLogging)
            {
                Plugin.log?.Debug("Attached to scene.");
                Plugin.log?.Info($"Lyrics are {(Plugin.config.DisplayLyrics ? "enabled" : "disabled")}.");
            }

            textSpawner = FindObjectOfType<FlyingTextSpawner>();
            songController = FindObjectOfType<GameSongController>();

            var sceneSetup = FindObjectOfType<GameplayCoreSceneSetup>();
            if (songController == null || sceneSetup == null)
                yield break;

            if (textSpawner == null)
            {
                var installer = sceneSetup as MonoInstallerBase;
                var diContainer = Accessors.Access_DiContainer(ref installer);

                textSpawner = diContainer.InstantiateComponentOnNewGameObject<FlyingTextSpawner>();
            }

            var sceneSetupData = Accessors.Access_SceneSetupData(ref sceneSetup);

            if (sceneSetupData == null)
                yield break;

            audio = Accessors.Access_AudioTimeSync(ref songController);

            IBeatmapLevel level = sceneSetupData.difficultyBeatmap.level;

            Plugin.log?.Info($"Corresponding song data found: {level.songName} by {level.songAuthorName} ({(level.songSubName != null ? level.songSubName : "No sub-name")}).");


            CustomPreviewBeatmapLevel customLevel = level as CustomPreviewBeatmapLevel;
            if (Plugin.config.VerboseLogging)
                Plugin.log?.Debug($"{level.songName} is {(customLevel != null ? "" : "not ")}a custom level.");
            SubtitleContainer container = Plugin.SelectedLevelSubtitles;
            List<Subtitle> subtitles = new List<Subtitle>();
            string sourceName = null;
            LyricSource lyricSource = LyricSource.None;

            if (container != null || (customLevel != null && LyricsFetcher.TryGetLocalLyrics(customLevel.customLevelPath, out container)))
            {
                Plugin.log?.Info("Found local lyrics.");
                Plugin.log?.Info($"These lyrics can be uploaded online using the ID: \"{level.GetLyricsHash()}\".");

                // Lyrics found locally, continue with them.
                SpawnText("Lyrics found locally", 3f);
            }
            else
            {
                Plugin.log?.Debug("Did not find local lyrics, trying online lyrics...");

                // When this coroutine ends, it will call the given callback with a list
                // of all the subtitles we found, and allow us to react.
                // If no subs are found, the callback is not called.
                yield return StartCoroutine(LyricsFetcher.GetOnlineLyrics(level, subtitles));

                if (subtitles.Count != 0)
                {
                    sourceName = "beatsinger.herokuapp.com";
                    lyricSource = LyricSource.Online_BeatSinger;
                    goto FoundOnlineLyrics;
                }

                if (!string.IsNullOrEmpty(level.songAuthorName))
                    yield return StartCoroutine(LyricsFetcher.GetMusixmatchLyrics(level.songName, level.songAuthorName, subtitles));
                else
                    Plugin.log?.Debug($"Song has no artist name.");

                if (subtitles.Count != 0)
                {
                    sourceName = "MusixMatch";
                    lyricSource = LyricSource.Online_MusixMatch;
                    goto FoundOnlineLyrics;
                }
                if (!string.IsNullOrEmpty(level.songSubName))
                    yield return StartCoroutine(LyricsFetcher.GetMusixmatchLyrics(level.songName, level.songSubName, subtitles));
                else
                    Plugin.log?.Debug($"Song has no subname.");

                if (subtitles.Count != 0)
                {
                    sourceName = "MusixMatch";
                    lyricSource = LyricSource.Online_MusixMatch;
                    goto FoundOnlineLyrics;
                }

                yield break;

            FoundOnlineLyrics:
                SpawnText("Lyrics found online", 3f);
                string songDir = customLevel?.customLevelPath;
                container = new SubtitleContainer(subtitles) { Source = sourceName, SourceType = lyricSource };
                if (Plugin.config.SaveFetchedLyrics)
                {
                    if (!string.IsNullOrEmpty(songDir))
                    {
                        Task.Run(() =>
                        {
                            string lyricsPath = Path.Combine(songDir, "lyrics.json");
                            try
                            {
                                if (!File.Exists(lyricsPath))
                                {
                                    File.WriteAllText(lyricsPath, container.ToJson().ToString(3));
                                    Plugin.log?.Info($"Saved fetched lyrics to '{lyricsPath}'");
                                }
                                else
                                    Plugin.log?.Warn($"Unable to save lyrics, file already exists: '{lyricsPath}'");
                            }
                            catch (Exception e)
                            {
                                Plugin.log?.Error($"Error saving fetched lyrics to '{lyricsPath}': {e.Message}");
                                Plugin.log?.Debug(e);
                            }
                        });
                    }
                    else
                        Plugin.log?.Warn($"Unable save lyrics, song directory couldn't be determined.");
                }
            }
            StartCoroutine(DisplayLyrics(container));
        }

        public void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.R))
                Plugin.config.Load();
#endif
            if (!Input.GetKeyUp((KeyCode)Plugin.config.ToggleKeyCode))
                return;

            Plugin.config.DisplayLyrics = !Plugin.config.DisplayLyrics;

            SpawnText(Plugin.config.DisplayLyrics ? "Lyrics enabled" : "Lyrics disabled", 3f);
        }

        private IEnumerator DisplayLyrics(SubtitleContainer subtitles)
        {
            if (subtitles == null)
                yield break;
            if (subtitles.Count == 0)
            {
                Plugin.log?.Info("No subtitles to display for this song.");
                yield break;
            }
            int skipped = 0;
            if (Plugin.config.VerboseLogging)
                Plugin.log?.Debug($"{subtitles.Count} lyrics found for song. Displaying with offset {subtitles.TimeOffset}s and a scale of {subtitles.TimeScale:P}");
            // Subtitles are sorted by time of appearance, so we can iterate without sorting first.
            foreach (var subtitle in subtitles)
            {
                float currentTime = audio.songTime;
                float subtitleTime = subtitle.Time + Plugin.config.DisplayDelay * (1 / audio.timeScale);
                // First, skip all subtitles that have already been seen.
                if (currentTime > subtitleTime)
                {
                    skipped++;
                    continue;
                }
                if (skipped > 0)
                {
                    if (Plugin.config.VerboseLogging)
                        Plugin.log?.Debug($"Skipped {skipped} lyrics because they started too soon.");
                    skipped = 0;
                }

                // Wait for time to display next lyrics
                yield return new WaitForSeconds((subtitleTime - currentTime) * (1 / audio.timeScale));
                if (!Plugin.config.DisplayLyrics)
                    // Don't display lyrics this time
                    continue;

                currentTime = audio.songTime;
                float displayDuration = ((subtitle.EndTime ?? audio.songEndTime) - currentTime) * (1 / audio.timeScale);
                if (Plugin.config.VerboseLogging)
                    Plugin.log?.Debug($"At {currentTime} and for {displayDuration} seconds, displaying lyrics \"{subtitle.Text}\".");

                SpawnText(subtitle.Text, displayDuration + Plugin.config.HideDelay, Plugin.config.EnableShake, Plugin.config.TextColor, Plugin.config.TextSize);
            }
        }

        private void SpawnText(string text, float duration) => SpawnText(text, duration, false, null, Plugin.config.TextSize);

        private void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize)
        {
            // Little hack to spawn text for a chosen duration in seconds:
            // Save the initial float _duration field to a variable,
            // then set it to the chosen duration, call SpawnText, and restore the
            // previously saved duration.
            float initialDuration = Accessors.Access_FlyingTextDuration(ref textSpawner);
            bool initialShake = Accessors.Access_FlyingTextShake(ref textSpawner);
            Color initialcolor = Accessors.Access_FlyingTextColor(ref textSpawner);
            float initialSize = Accessors.Access_FlyingTextFontSize(ref textSpawner);
#if DEBUG
            if (Plugin.config.VerboseLogging)
            {
                Plugin.log?.Info($"Text Settings:");
                Plugin.log?.Info($"       Duration: {duration}");
                Plugin.log?.Info($"   ShakeEnabled: {(enableShake ? "True" : "False")}");
                Plugin.log?.Info($"          Color: {(color?.ToString() ?? "Default")}");
                Plugin.log?.Info($"           Size: {fontSize}");
            }
#endif
            if (duration <= 0)
            {
                Plugin.log?.Warn($"Text '{text}' has a duration less than 0. Using 1s instead.");
                duration = 1;
            }
            Accessors.Access_FlyingTextDuration(ref textSpawner) = duration;
            Accessors.Access_FlyingTextShake(ref textSpawner) = enableShake;
            if (color.HasValue)
                Accessors.Access_FlyingTextColor(ref textSpawner) = color.Value;
            if (fontSize > 0)
                Accessors.Access_FlyingTextFontSize(ref textSpawner) = fontSize;

            textSpawner.SpawnText(Plugin.config.Position, Quaternion.identity, Quaternion.Inverse(Quaternion.identity), text);

            // Reset values
            Accessors.Access_FlyingTextDuration(ref textSpawner) = initialDuration;
            Accessors.Access_FlyingTextShake(ref textSpawner) = initialShake;
            Accessors.Access_FlyingTextColor(ref textSpawner) = initialcolor;
            Accessors.Access_FlyingTextFontSize(ref textSpawner) = initialSize;
        }
    }
}
