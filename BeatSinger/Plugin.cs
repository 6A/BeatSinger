using IPA;
using IPALogger = IPA.Logging.Logger;
using UnityEngine.SceneManagement;

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
        internal static IPALogger log;
        internal static Settings config;

        [Init]
        public void Init(IPALogger logger)
        {
            log = logger;
            config = new Settings();
            config.Load();
            if (config.VerboseLogging)
                log.Debug($"VerboseLogging enabled.");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu(Name, "BeatSinger.UI.SettingsView.bsml", config);
        }

        [OnEnable]
        public void OnEnabled()
        {
            BS_Utils.Utilities.BSEvents.gameSceneActive -= OnGameSceneActive;
            BS_Utils.Utilities.BSEvents.gameSceneActive += OnGameSceneActive;
        }

        [OnDisable]
        public void OnDisabled()
        {
            BS_Utils.Utilities.BSEvents.gameSceneActive -= OnGameSceneActive;
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            config.Save();
        }



        private void OnGameSceneActive()
        {
            Scene gameScene = SceneManager.GetActiveScene();
            gameScene.GetRootGameObjects()[0].AddComponent<LyricsComponent>();
        }
    }
}
