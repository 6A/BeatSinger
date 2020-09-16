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
using BeatSinger.Components;
using BS_Utils.Utilities;

namespace BeatSinger
{
    /// <summary>
    ///   Defines the main component of BeatSinger, which displays lyrics on loaded songs.
    /// </summary>
    public class GameLyricsComponent : LyricsComponent
    {
        private GameSongController songController;

        public override void Awake()
        {
            enabled = true;
            BSEvents.songPaused += OnSongPaused;
            BSEvents.songUnpaused += OnSongUnpaused;
        }

        private void OnSongUnpaused()
        {
            // TODO: Handle game pauses
        }

        private void OnSongPaused()
        {
            // TODO: Handle game pauses
        }

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

            FlyingTextSpawner flyingTextSpawner = FindObjectOfType<FlyingTextSpawner>();
            songController = FindObjectOfType<GameSongController>();

            var sceneSetup = FindObjectOfType<GameplayCoreSceneSetup>();
            if (songController == null || sceneSetup == null)
                yield break;

            if (flyingTextSpawner == null)
            {
                var installer = sceneSetup as MonoInstallerBase;
                var diContainer = Accessors.Access_DiContainer(ref installer);

                flyingTextSpawner = diContainer.InstantiateComponentOnNewGameObject<FlyingTextSpawner>();
            }
            if (flyingTextSpawner == null)
            {
                Plugin.log?.Error($"Could not get a FlyingTextSpawner.");
                yield break;
            }
            ILyricSpawner spawner = new FlyingTextLyricSpawner(flyingTextSpawner);
            var sceneSetupData = Accessors.Access_SceneSetupData(ref sceneSetup);

            if (sceneSetupData == null)
                yield break;
            AudioTimeSyncController audioTimeSyncController = Accessors.Access_AudioTimeSync(ref songController);
            if (audioTimeSyncController == null)
            {
                Plugin.log?.Error($"Could not get the AudioTimeSyncController.");
                yield break;
            }
            IAudioSource audioWrapper = new AudioTimeSyncWrapper(audioTimeSyncController);

            IPreviewBeatmapLevel level = sceneSetupData?.difficultyBeatmap.level ?? Plugin.SelectedLevel;

            Plugin.log?.Info($"Corresponding song data found: {level.songName} by {level.songAuthorName} ({(level.songSubName != null ? level.songSubName : "No sub-name")}).");


            CustomPreviewBeatmapLevel customLevel = level as CustomPreviewBeatmapLevel;
            if (Plugin.config.VerboseLogging)
                Plugin.log?.Debug($"{level.songName} is {(customLevel != null ? "" : "not ")}a custom level.");
            SubtitleContainer container = Plugin.SelectedLevelSubtitles;

            if (container != null && container.Count > 0)
            {
                Initialize(audioWrapper, spawner, container);
                Plugin.log?.Info("Lyrics already loaded.");

                // Lyrics found locally, continue with them.
                SpawnText("Lyrics loaded", 3f);
            }
            else if (customLevel != null && LyricsFetcher.TryGetLocalLyrics(customLevel, out container))
            {
                // This doesn't set Plugin.SelectedLevelSubtitles, but it probably doesn't matter because they'll be loaded on MenuScene.
                Initialize(audioWrapper, spawner, container);
                Plugin.log?.Info("Found local lyrics.");
                Plugin.log?.Info($"These lyrics can be uploaded online using the ID: \"{level.GetLyricsHash()}\".");

                // Lyrics found locally, continue with them.
                SpawnText("Lyrics found locally", 3f);
            }
            else
            {
                Plugin.log?.Debug("Did not find local lyrics, trying online lyrics...");
                container = SubtitleContainer.Empty;
                // When this coroutine ends, it will call the given callback with a list
                // of all the subtitles we found, and allow us to react.
                // If no subs are found, the callback is not called.
                yield return LyricsFetcher.FetchOnlineLyrics(level, container);
                if (container.Count > 0)
                {
                    Initialize(audioWrapper, spawner, container);
                    SpawnText("Lyrics found online", 3f);
                }
            }

        }
    }
}
