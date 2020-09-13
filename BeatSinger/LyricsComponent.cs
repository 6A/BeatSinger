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
                var container = Access_DiContainer(ref installer);

                textSpawner = container.InstantiateComponentOnNewGameObject<FlyingTextSpawner>();
            }

            var sceneSetupData = Access_SceneSetupData(ref sceneSetup);
            //(GameplayCoreSceneSetupData)SceneSetupDataField.GetValue(sceneSetup);

            if (sceneSetupData == null)
                yield break;

            audio = Access_AudioTimeSync(ref songController);
            //(AudioTimeSyncController)AudioTimeSyncField.GetValue(songController);

            IBeatmapLevel level = sceneSetupData.difficultyBeatmap.level;
            List<Subtitle> subtitles = new List<Subtitle>();

            Plugin.log?.Info($"Corresponding song data found: {level.songName} by {level.songAuthorName} ({(level.songSubName != null ? level.songSubName : "No sub-name")}).");


            CustomPreviewBeatmapLevel customLevel = level as CustomPreviewBeatmapLevel;
            if (Settings.VerboseLogging)
                Plugin.log?.Debug($"{level.songName} is {(customLevel != null ? "" : "not ")}a custom level.");
            if (customLevel != null && LyricsFetcher.GetLocalLyrics(customLevel.customLevelPath, subtitles))
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
                                    File.WriteAllText(lyricsPath, subtitles.ToJsonArray().ToString());
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

            StartCoroutine(DisplayLyrics(subtitles));
        }

        public void Update()
        {
            if (!Input.GetKeyUp((KeyCode)Settings.ToggleKeyCode))
                return;

            Settings.DisplayLyrics = !Settings.DisplayLyrics;

            SpawnText(Settings.DisplayLyrics ? "Lyrics enabled" : "Lyrics disabled", 3f);
        }

        private IEnumerator DisplayLyrics(IList<Subtitle> subtitles)
        {
            // Subtitles are sorted by time of appearance, so we can iterate without sorting first.
            int i = 0;

            // First, skip all subtitles that have already been seen.
            {
                float currentTime = audio.songTime;

                while (i < subtitles.Count)
                {
                    Subtitle subtitle = subtitles[i];

                    if (subtitle.Time >= currentTime)
                        // Subtitle appears after current moment, stop skipping
                        break;

                    i++;
                }
            }

            if (Settings.VerboseLogging && i > 0)
                Plugin.log?.Debug($"Skipped {i} lyrics because they started too soon.");

            // Display all lyrics
            while (i < subtitles.Count)
            {
                // Wait for time to display next lyrics
                yield return new WaitForSeconds(subtitles[i++].Time - audio.songTime + Settings.DisplayDelay);

                if (!Settings.DisplayLyrics)
                    // Don't display lyrics this time
                    continue;

                // We good, display lyrics
                Subtitle subtitle = subtitles[i - 1];

                float displayDuration,
                      currentTime = audio.songTime;

                if (subtitle.EndTime.HasValue)
                {
                    displayDuration = subtitle.EndTime.Value - currentTime;
                }
                else
                {
                    displayDuration = i == subtitles.Count
                                    ? audio.songLength - currentTime
                                    : subtitles[i].Time - currentTime;
                }

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
