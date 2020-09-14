using BeatSinger.Helpers;
using BeatSinger.Components;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSinger
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class LyricPreviewController : MonoBehaviour
    {
        private SongPreviewPlayer PreviewPlayer;
        private FlowCoordinator MainFlowCoordinator;
        private LyricsComponent LyricsComponent;
        private ScreenSystem ScreenSystem => MainFlowCoordinator != null ? Accessors.Access_ScreenSystem(ref MainFlowCoordinator) : null;
        private HMUI.Screen _mainScreen;

        public HMUI.Screen MainScreen
        {
            get
            {
                if (_mainScreen == null)
                {
                    ScreenSystem screens = ScreenSystem;
                    _mainScreen = screens?.mainScreen;
                }
                return _mainScreen;
            }
            set { _mainScreen = value; }
        }

        private AudioSource[] AudioSources;
        private Vector3 PreviousMainPosition;
        private IEnumerator LogPosition()
        {
            while (enabled)
            {
                AudioSource[] sources = AudioSources;
                if (PreviewPlayer != null && sources != null && sources.Length > 0)
                {
                    Plugin.log?.Info($"Preview position:");
                    for (int i = 0; i < sources.Length; i++)
                    {
                        Plugin.log?.Info($"  {i}: {sources[i].isPlaying}|{sources[i].time}");
                    }
                }
                else
                    Plugin.log?.Debug("No audio sources.");
                yield return new WaitForSeconds(3f);
            }
        }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            PreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();
            MainFlowCoordinator = BeatSaberMarkupLanguage.BeatSaberUI.MainFlowCoordinator;
        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private async void OnEnable()
        {
            AudioClip thing = await Plugin.SelectedLevel.GetPreviewAudioClipAsync(CancellationToken.None);

            HMUI.Screen screen = MainScreen;
            if (screen != null)
            {
                if (screen.transform.position.y < 50)
                    PreviousMainPosition = screen.transform.position;
                screen.transform.position = new Vector3(PreviousMainPosition.x, 100, PreviousMainPosition.z);
            }
            Plugin.log?.Info($"Starting song preview: {thing.loadState}");
            thing.LoadAudioData();
            if (thing.loadState != AudioDataLoadState.Loaded)
            {
                Plugin.log?.Info($"Beginning audio load: {thing.loadState}");
                await Task.Run(async () =>
                {
                    AudioDataLoadState loadState = thing.loadState;
                    while (loadState == AudioDataLoadState.Loading)
                    {
                        await Task.Delay(25).ConfigureAwait(false);
                        loadState = thing.loadState;
                    }
                    Plugin.log?.Info($"Audio loading finished: {thing.loadState}");
                });
            }
            PreviewPlayer.CrossfadeTo(thing, 0, thing.length);
            AudioSources = Accessors.Access_PreviewPlayerAudioSources(ref PreviewPlayer);
            if (thing != null && AudioSources != null && AudioSources.Length > 0)
            {
                LyricsComponent = gameObject.AddComponent<LyricsComponent>();
                ILyricSpawner spawner = gameObject.AddComponent<SimpleLyricSpawner>();
                LyricsComponent.Initialize(new PreviewAudioSource(thing, AudioSources), spawner, Plugin.SelectedLevelSubtitles);
            }
            StartCoroutine(LogPosition());
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            HMUI.Screen screen = MainScreen;
            if (screen != null)
            {
                screen.transform.position = PreviousMainPosition;
            }
            PreviewPlayer?.FadeOut();
            AudioSources = null;
            if (LyricsComponent != null)
            {
                Destroy(LyricsComponent);
                LyricsComponent = null;
            }
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {

        }
        #endregion
    }
}
