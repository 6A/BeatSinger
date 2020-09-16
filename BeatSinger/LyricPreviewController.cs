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
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSinger.UI;
using BeatSaberMarkupLanguage;

namespace BeatSinger
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class LyricPreviewController : MonoBehaviour
    {
        private SongPreviewPlayer PreviewPlayer;
        private LyricsComponent LyricsComponent;


        private AudioSource[] AudioSources;
        private CancellationTokenSource loadCancellationSource;
        private CancellationTokenSource LoadCancellationSource
        {
            get => loadCancellationSource;
            set
            {
                if (loadCancellationSource != null)
                {
                    loadCancellationSource.Cancel();
                    loadCancellationSource.Dispose();
                }
                loadCancellationSource = value;
            }
        }
        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            PreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();
        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }
        FloatingScreen FloatingScreen;
        LyricsViewController LyricsView;
        public void ShowLyricsPanel()
        {
            if (FloatingScreen == null)
            {
                FloatingScreen = CreateFloatingScreen();
                LyricsView = BeatSaberUI.CreateViewController<LyricsViewController>();
                LyricsView.CloseClicked += OnLyricsViewClosedClicked;
                FloatingScreen.SetRootViewController(LyricsView, false);
                SetScreenTransform(FloatingScreen, new Pose(new Vector3(0, 1.6f, 2.4f), Quaternion.identity));
            }
            FloatingScreen?.gameObject.SetActive(true);
            LyricsView?.gameObject.SetActive(true);
        }
        public void HideLyricsPanel()
        {
            LyricsView?.Clear();
            FloatingScreen?.gameObject.SetActive(false);
            LyricsView?.gameObject.SetActive(false);
        }

        private void OnLyricsViewClosedClicked(object sender, EventArgs e)
        {
            if(sender is LyricsViewController view)
            {
                view.gameObject.SetActive(false);
            }
            HideLyricsPanel();
        }

        public FloatingScreen CreateFloatingScreen()
        {
            FloatingScreen screen = FloatingScreen.CreateFloatingScreen(
                new Vector2(100, 50), true,
                Vector3.zero,
                Quaternion.identity);
            screen.ShowHandle = false;
            return screen;
        }

        internal static void SetScreenTransform(FloatingScreen screen, Pose pose)
        {
            screen.transform.position = pose.position;
            screen.transform.rotation = pose.rotation;
        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private async void OnEnable()
        {
            LoadCancellationSource = new CancellationTokenSource();
            if (!Plugin.SubtitlesLoaded)
            {
                Plugin.log?.Warn($"Cannot preview song's lyrics, no lyrics are available.");
                return;
            }
            AudioClip audioClip = await Plugin.SelectedLevel.GetPreviewAudioClipAsync(loadCancellationSource.Token);
            ShowLyricsPanel();

            audioClip.LoadAudioData();
            if (audioClip.loadState != AudioDataLoadState.Loaded)
            {
                Plugin.log?.Debug($"Beginning audio load: {audioClip.loadState}");
                await Task.Run(async () =>
                {
                    AudioDataLoadState loadState = audioClip.loadState;
                    while (loadState == AudioDataLoadState.Loading)
                    {
                        await Task.Delay(25).ConfigureAwait(false);
                        loadState = audioClip.loadState;
                    }
                    Plugin.log?.Debug($"Audio loading finished: {audioClip.loadState}");
                });
            }
            PreviewPlayer.CrossfadeTo(audioClip, 0, audioClip.length);
            AudioSources = Accessors.Access_PreviewPlayerAudioSources(ref PreviewPlayer);
            if (audioClip != null && AudioSources != null && AudioSources.Length > 0)
            {
                LyricsComponent = gameObject.AddComponent<LyricsComponent>();
                ILyricSpawner spawner = LyricsView;
                LyricsComponent.Initialize(new PreviewAudioSource(audioClip, AudioSources), spawner, Plugin.SelectedLevelSubtitles);
            }
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            LoadCancellationSource = null;
            HideLyricsPanel();

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
            if (LyricsView != null)
            {
                Destroy(LyricsView);
                LyricsView = null;
            }
            if (FloatingScreen != null)
            {
                Destroy(FloatingScreen);
                FloatingScreen = null;
            }

        }
        #endregion
    }
}
