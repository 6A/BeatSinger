using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers.Settings;
using BeatSinger.Helpers;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeatSinger.UI
{
    public class ModifiersConfig : INotifiableHost
    {

        [UIAction("OnDragReleased")]
        private void OnDragRelease()
        {
            UpdateSliderBounds(_offsetSlider);
        }

        [UIComponent("OffsetSlider")]
        protected SliderSetting _offsetSlider;

        public ModifiersConfig()
        {
            Plugin.config.EnabledChanged += OnEnabledChanged;
            Plugin.SelectedLevelChanged += OnSelectedLevelChanged;
            FetchEnabled = !LyricsFetcher.FetchInProgress;
            LyricsFetcher.LyricsOnlineFetchStarted += OnOnlineFetchStarted;
            LyricsFetcher.LyricsOnlineFetchFinished += OnOnlineFetchFinished;
        }
        private void NotifyEnabledChanged()
        {
            NotifyPropertyChanged(nameof(Enabled));
            NotifyPropertyChanged(nameof(ConfigEnabled));
            NotifyPropertyChanged(nameof(FetchEnabled));
        }
        private void OnEnabledChanged(object sender, EventArgs _)
        {
            NotifyEnabledChanged();
        }

        private void OnSelectedLevelChanged(object sender, LyricsFetchedEventArgs e)
        {
            Subtitles = e.Subtitles;
            SongName = e.BeatmapLevel.songName;
        }

        private void OnOnlineFetchFinished(object sender, LyricsFetchedEventArgs e)
        {
            FetchEnabled = true;
        }

        private void OnOnlineFetchStarted(object sender, IPreviewBeatmapLevel e)
        {
            FetchEnabled = false;
        }

        private string _songName;
        [UIValue(nameof(SongName))]
        public string SongName
        {
            get { return _songName ?? "N/A"; }
            set
            {
                if (_songName == value) return;
                _songName = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue(nameof(Enabled))]
        public bool Enabled
        {
            get => Plugin.config.DisplayLyrics;
            set
            {
                Plugin.config.DisplayLyrics = value;
                NotifyEnabledChanged();
            }
        }
        private float _timeOffset;

        [UIValue(nameof(TimeOffset))]
        public float TimeOffset
        {
            get
            {
                return _timeOffset;
            }
            set
            {
                if (_timeOffset == value) return;
                _timeOffset = value;
                if (Subtitles != null)
                    Subtitles.TimeOffset = value;
                NotifyPropertyChanged();
            }
        }

        public float OffsetMax
        {
            get => DragReleaseSupported ? _timeOffset + 5f : 15f;
        }

        public float OffsetMin
        {
            get => DragReleaseSupported ? _timeOffset - 5f : -15f;
        }

        public void UpdateSliderBounds(SliderSetting slider)
        {
            if (DragReleaseSupported && slider != null)
            {
                float val = slider.slider.value;
                slider.slider.minValue = OffsetMin;
                slider.slider.maxValue = OffsetMax;
                slider.slider.numberOfSteps = (int)Math.Round((OffsetMax - OffsetMin) / slider.increments) + 1;
                slider.slider.value = val + 0.0001f;
                slider.slider.value = val;
                slider.ReceiveValue();
            }
        }

        private float _timeScale;

        [UIValue(nameof(TimeScale))]
        public float TimeScale
        {
            get { return _timeScale; }
            set
            {
                if (_timeScale == value) return;
                _timeScale = value;
                if (Subtitles != null)
                    Subtitles.TimeScale = value;
                NotifyPropertyChanged();
            }
        }

        private bool _configEnabled;

        [UIValue(nameof(ConfigEnabled))]
        public bool ConfigEnabled
        {
            get { return _configEnabled && Enabled && (Subtitles?.Count ?? 0) > 0; }
            set
            {
                if (_configEnabled == value) return;
                _configEnabled = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(FetchEnabled));
            }
        }
        private bool _fetchEnabled;
        [UIValue(nameof(FetchEnabled))]
        public bool FetchEnabled
        {
            get => _fetchEnabled && !ConfigEnabled && Enabled;
            set
            {
                _fetchEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private SubtitleContainer _subtitles;

        public SubtitleContainer Subtitles
        {
            get { return _subtitles; }
            private set
            {
                if (_subtitles != value)
                {
                    _subtitles = value;
                    if (value != null)
                    {
                        _timeOffset = Subtitles.TimeOffset;
                        _timeScale = Subtitles.TimeScale;
                    }
                    else
                    {
                        _timeOffset = 0;
                        _timeScale = 1;
                    }
                    UpdateSliderBounds(_offsetSlider);
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(TimeOffset));
                    NotifyPropertyChanged(nameof(TimeScale));
                }
                ConfigEnabled = (value?.Count ?? 0) > 0;

            }
        }

        [UIAction("SaveSettings")]
        public void SaveSettings()
        {
            SubtitleContainer container = Plugin.SelectedLevelSubtitles;
            if (container == null)
                return;
            try
            {
                if (Plugin.SelectedLevel is CustomPreviewBeatmapLevel level)
                {
                    if (Directory.Exists(level.customLevelPath))
                    {
                        string lyricsPath = Path.Combine(level.customLevelPath, "lyrics.json");
                        File.WriteAllText(lyricsPath, container.ToJson().ToString(3));
                        Plugin.log?.Info($"Updated lyrics file at '{lyricsPath}'");
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log?.Error($"Error saving song lyric settings: {e.Message}");
                Plugin.log?.Debug(e);
            }
        }

        [UIAction("Fetch")]
        public void FetchSubtitles()
        {
            SubtitleContainer container = SubtitleContainer.Empty;

            SharedCoroutineStarter.instance.StartCoroutine(LyricsFetcher.FetchOnlineLyrics(Plugin.SelectedLevel, container));
        }

        [UIAction("PlaySong")]
        public void PlaySong()
        {
            if (LyricPreviewController == null)
            {
                LyricPreviewController = new GameObject("BeatSinger_PreviewPlayer").AddComponent<LyricPreviewController>();
            }
            else if (Plugin.SubtitlesLoaded)
                LyricPreviewController.gameObject.SetActive(!LyricPreviewController.gameObject.activeSelf);
            else
                LyricPreviewController.gameObject.SetActive(false);
            NotifyPropertyChanged(nameof(PlayButtonText));
        }

        [UIValue(nameof(PlayButtonText))]
        public string PlayButtonText => (LyricPreviewController?.gameObject.activeSelf ?? false) ? "Stop" : "Test Song";

        LyricPreviewController LyricPreviewController;

        [UIAction("formatter-percent")]
        public string FloatToPercent(float val) => val.ToString("P");

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [UIAction("#post-parse")]
        public void PostParse()
        {
            if (Type.GetType(DragHelperName) != null)
            {
                DragReleaseSupported = true;
            }
            UpdateSliderBounds(_offsetSlider);
        }

        private bool DragReleaseSupported;
        public static readonly string DragHelperName = $"BeatSaberMarkupLanguage.Components.Settings.DragHelper, {typeof(SliderSettingHandler).Assembly.FullName}";
    }
}
