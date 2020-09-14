using IPA;
using IPALogger = IPA.Logging.Logger;
using UnityEngine.SceneManagement;
using BeatSinger.UI;
using BeatSinger.Helpers;
using System;
using System.IO;

namespace BeatSinger
{
    /// <summary>
    ///   Entry point of the plugin.
    /// </summary>
    [Plugin(RuntimeOptions.DynamicInit)]
    public sealed class Plugin
    {
        public string Name => "Beat Singer";
        public string Version => "1.1.0.0";

        private static IPreviewBeatmapLevel selectedLevel;
        internal static IPreviewBeatmapLevel SelectedLevel
        {
            get => selectedLevel;
        }
        private static SubtitleContainer selectedLevelSubtitles;

        internal static IPALogger log;
        internal static Settings config;
        private static ModifiersConfig modifierConfig;
        /// <summary>
        /// Do not use an empty container and add subtitles after without raising <see cref="SelectedLevelChanged"/>,
        /// <see cref="ModifiersConfig"/> will not update it's enabled state properly.
        /// </summary>
        internal static SubtitleContainer SelectedLevelSubtitles { get => selectedLevelSubtitles; }
        internal static bool SubtitlesLoaded => (selectedLevelSubtitles?.Count ?? 0) > 0;
        internal static event EventHandler<LyricsFetchedEventArgs> SelectedLevelChanged;

        [Init]
        public void Init(IPALogger logger)
        {
            log = logger;
            config = new Settings();
            config.Load();
            if (config.VerboseLogging)
                log.Debug($"VerboseLogging enabled.");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu(Name, "BeatSinger.UI.SettingsView.bsml", config);
            modifierConfig = new ModifiersConfig();
        }

        [OnEnable]
        public void OnEnabled()
        {
            SubscribeEvents();
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab(Name, "BeatSinger.UI.ModifiersView.bsml", modifierConfig);
        }

        [OnDisable]
        public void OnDisabled()
        {
            UnsubscribeEvents();
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.RemoveTab(Name);
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            config.Save();
        }

        public static SubtitleContainer GetSubtitlesForLevel(CustomPreviewBeatmapLevel customLevel)
        {
            if (customLevel == null)
                return null;
            try
            {
                if (LyricsFetcher.TryGetLocalLyrics(customLevel, out SubtitleContainer subtitles))
                {
                    return subtitles;
                }
            }
            catch (Exception e)
            {
                log?.Error($"Error loading local lyrics for '{customLevel?.songName}': {e.Message}");
                log?.Debug(e);
            }
            return null;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            BS_Utils.Utilities.BSEvents.gameSceneActive += OnGameSceneActive;
            BS_Utils.Utilities.BSEvents.levelSelected += OnLevelSelected;
            BS_Utils.Utilities.BSEvents.menuSceneActive += OnMenuSceneActive;
            LyricsFetcher.LyricsOnlineFetchFinished += OnLyricsFetched;
        }

        private void UnsubscribeEvents()
        {
            BS_Utils.Utilities.BSEvents.gameSceneActive -= OnGameSceneActive;
            BS_Utils.Utilities.BSEvents.levelSelected -= OnLevelSelected;
            BS_Utils.Utilities.BSEvents.menuSceneActive -= OnMenuSceneActive;
            LyricsFetcher.LyricsOnlineFetchFinished -= OnLyricsFetched;
        }

        private void OnLyricsFetched(object sender, LyricsFetchedEventArgs e)
        {
            if (SelectedLevel == e.BeatmapLevel)
            {
                selectedLevelSubtitles = e.Subtitles;
                SelectedLevelChanged.RaiseEventSafe(this, new LyricsFetchedEventArgs(e.BeatmapLevel, e.Subtitles), nameof(SelectedLevelChanged));
            }
        }

        private void OnMenuSceneActive()
        {
            if (SelectedLevelSubtitles == null && SelectedLevel is CustomPreviewBeatmapLevel customLevel)
            {
                SubtitleContainer subtitles = GetSubtitlesForLevel(customLevel);
                selectedLevelSubtitles = subtitles;
                if (subtitles != null)
                {
                    string fileName = subtitles.Source;
                    if (!string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                        fileName = Path.GetFileName(fileName);
                    log?.Info($"Local subtitles loaded for '{customLevel.songName}' from {fileName}");
                }
                SelectedLevelChanged.RaiseEventSafe(this, new LyricsFetchedEventArgs(customLevel, subtitles), nameof(SelectedLevelChanged));
            }
        }

        private void OnLevelSelected(LevelCollectionViewController _, IPreviewBeatmapLevel level)
        {
            if (config.VerboseLogging)
                Plugin.log?.Debug($"Level selected: {level?.songName}");
            selectedLevel = level;
            selectedLevelSubtitles = null;
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                SubtitleContainer subtitles = GetSubtitlesForLevel(customLevel);
                selectedLevelSubtitles = subtitles;
                if (subtitles != null)
                {
                    string fileName = subtitles.Source;
                    if (!string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                        fileName = Path.GetFileName(fileName);
                    log?.Info($"Local subtitles loaded for '{customLevel.songName}' from {fileName}");
                }
                else
                {
                    if (config.VerboseLogging)
                        log?.Info($"No local subtitles for '{customLevel.songName}'");
                }
            }
            else
            {
                if (config.VerboseLogging)
                    log?.Info($"'{level.songName}' is not a custom level.");
            }

            SelectedLevelChanged.RaiseEventSafe(this, new LyricsFetchedEventArgs(level, selectedLevelSubtitles), nameof(SelectedLevelChanged));
        }

        private void OnGameSceneActive()
        {
            Scene gameScene = SceneManager.GetActiveScene();
            GameLyricsComponent lyricsComponent = gameScene.GetRootGameObjects()[0].AddComponent<GameLyricsComponent>();

        }
    }
}
