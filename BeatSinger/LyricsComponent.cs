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

        private static readonly FieldAccessor<GameSongController, AudioTimeSyncController>.Accessor Access_AudioTimeSync
            = FieldAccessor<GameSongController, AudioTimeSyncController>.GetAccessor("_audioTimeSyncController");

        private static readonly FieldAccessor<GameplayCoreSceneSetup, GameplayCoreSceneSetupData>.Accessor Access_SceneSetupData
            = FieldAccessor<GameplayCoreSceneSetup, GameplayCoreSceneSetupData>.GetAccessor("_sceneSetupData");


        private static readonly FieldAccessor<MonoInstallerBase, DiContainer>.Accessor Access_DiContainer
            = FieldAccessor<MonoInstallerBase, DiContainer>.GetAccessor("<Container>k__BackingField");

        private static readonly FieldAccessor<FlyingTextSpawner, FlyingTextEffect.Pool>.Accessor Access_FlyingTextEffectPool
            = FieldAccessor<FlyingTextSpawner, FlyingTextEffect.Pool>.GetAccessor("_flyingTextEffectPool");

        private static readonly Func<FlyingTextSpawner, float> GetTextSpawnerDuration;
        private static readonly Action<FlyingTextSpawner, float> SetTextSpawnerDuration;

        static LyricsComponent()
        {
            FieldInfo durationField = typeof(FlyingTextSpawner).GetField("_duration", NON_PUBLIC_INSTANCE);

            if (durationField == null)
                throw new Exception("Cannot find _duration field of FlyingTextSpawner.");

            // Create dynamic setter
            DynamicMethod setterMethod = new DynamicMethod("SetDuration", typeof(void), new[] { typeof(FlyingTextSpawner), typeof(float) }, typeof(FlyingTextSpawner));
            ILGenerator setterIl = setterMethod.GetILGenerator(16);

            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, durationField);
            setterIl.Emit(OpCodes.Ret);

            SetTextSpawnerDuration = setterMethod.CreateDelegate(typeof(Action<FlyingTextSpawner, float>))
                                  as Action<FlyingTextSpawner, float>;

            // Create dynamic getter
            DynamicMethod getterMethod = new DynamicMethod("GetDuration", typeof(float), new[] { typeof(FlyingTextSpawner) }, typeof(FlyingTextSpawner));
            ILGenerator getterIl = getterMethod.GetILGenerator(16);

            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, durationField);
            getterIl.Emit(OpCodes.Ret);

            GetTextSpawnerDuration = getterMethod.CreateDelegate(typeof(Func<FlyingTextSpawner, float>))
                                  as Func<FlyingTextSpawner, float>;
        }


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

            if (Settings.VerboseLogging)
            {
                Plugin.log?.Debug("Attached to scene.");
                Plugin.log?.Info($"Lyrics are {(Settings.DisplayLyrics ? "enabled" : "disabled")}.");
            }

            textSpawner = FindObjectOfType<FlyingTextSpawner>();
            songController = FindObjectOfType<GameSongController>();

            var sceneSetup = FindObjectOfType<GameplayCoreSceneSetup>();
            if (songController == null || sceneSetup == null)
                yield break;

            if (textSpawner == null)
            {
                var installer = sceneSetup as MonoInstallerBase;
                var diContainer = Access_DiContainer(ref installer);

                textSpawner = diContainer.InstantiateComponentOnNewGameObject<FlyingTextSpawner>();
            }

            var sceneSetupData = Access_SceneSetupData(ref sceneSetup);

            if (sceneSetupData == null)
                yield break;

            audio = Access_AudioTimeSync(ref songController);

            IBeatmapLevel level = sceneSetupData.difficultyBeatmap.level;

            Plugin.log?.Info($"Corresponding song data found: {level.songName} by {level.songAuthorName} ({(level.songSubName != null ? level.songSubName : "No sub-name")}).");


            CustomPreviewBeatmapLevel customLevel = level as CustomPreviewBeatmapLevel;
            if (Settings.VerboseLogging)
                Plugin.log?.Debug($"{level.songName} is {(customLevel != null ? "" : "not ")}a custom level.");
            SubtitleContainer container = null;
            List<Subtitle> subtitles = new List<Subtitle>();
            if (customLevel != null && LyricsFetcher.TryGetLocalLyrics(customLevel.customLevelPath, out container))
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
                    goto FoundOnlineLyrics;

                if (!string.IsNullOrEmpty(level.songAuthorName))
                    yield return StartCoroutine(LyricsFetcher.GetMusixmatchLyrics(level.songName, level.songAuthorName, subtitles));
                else
                    Plugin.log?.Debug($"Song has no artist name.");

                if (subtitles.Count != 0)
                    goto FoundOnlineLyrics;
                if (!string.IsNullOrEmpty(level.songSubName))
                    yield return StartCoroutine(LyricsFetcher.GetMusixmatchLyrics(level.songName, level.songSubName, subtitles));
                else
                    Plugin.log?.Debug($"Song has no subname.");

                if (subtitles.Count != 0)
                    goto FoundOnlineLyrics;

                yield break;

            FoundOnlineLyrics:
                SpawnText("Lyrics found online", 3f);
                string songDir = customLevel?.customLevelPath;
                container = new SubtitleContainer(subtitles);
                if (Settings.SaveFetchedLyrics)
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
            if (!Input.GetKeyUp((KeyCode)Settings.ToggleKeyCode))
                return;

            Settings.DisplayLyrics = !Settings.DisplayLyrics;

            SpawnText(Settings.DisplayLyrics ? "Lyrics enabled" : "Lyrics disabled", 3f);
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
            if (Settings.VerboseLogging)
                Plugin.log?.Debug($"{subtitles.Count} lyrics found for song. Displaying with offset {subtitles.TimeOffset}s and a scale of {subtitles.TimeScale:P}");
            // Subtitles are sorted by time of appearance, so we can iterate without sorting first.
            foreach (var subtitle in subtitles)
            {
                float currentTime = audio.songTime;
                float subtitleTime = subtitle.Time + Settings.DisplayDelay;
                // First, skip all subtitles that have already been seen.
                if (currentTime > subtitleTime)
                {
                    skipped++;
                    continue;
                }
                if (skipped > 0)
                {
                    if (Settings.VerboseLogging)
                        Plugin.log?.Debug($"Skipped {skipped} lyrics because they started too soon.");
                    skipped = 0;
                }

                // Wait for time to display next lyrics
                yield return new WaitForSeconds(subtitleTime - currentTime);
                if (!Settings.DisplayLyrics)
                    // Don't display lyrics this time
                    continue;

                currentTime = audio.songTime;
                float displayDuration = (subtitle.EndTime ?? audio.songEndTime) - currentTime;
                if (Settings.VerboseLogging)
                    Plugin.log?.Debug($"At {currentTime} and for {displayDuration} seconds, displaying lyrics \"{subtitle.Text}\".");

                SpawnText(subtitle.Text, displayDuration + Settings.HideDelay);
            }
        }

        private void SpawnText(string text, float duration)
        {
            // Little hack to spawn text for a chosen duration in seconds:
            // Save the initial float _duration field to a variable,
            // then set it to the chosen duration, call SpawnText, and restore the
            // previously saved duration.
            float initialDuration = GetTextSpawnerDuration(textSpawner);

            SetTextSpawnerDuration(textSpawner, duration);
            textSpawner.SpawnText(new Vector3(0, 4, 0), Quaternion.identity, Quaternion.Inverse(Quaternion.identity), text);
            SetTextSpawnerDuration(textSpawner, initialDuration);
        }
    }
}
