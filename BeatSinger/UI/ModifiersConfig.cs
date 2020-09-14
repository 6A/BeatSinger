using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Parser;
using BeatSinger.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.UI
{
    public class ModifiersConfig : INotifiableHost
    {
        public ModifiersConfig()
        {
            Plugin.SelectedLevelChanged += OnSelectedLevelChanged;
            FetchEnabled = !LyricsFetcher.FetchInProgress;
            LyricsFetcher.LyricsOnlineFetchStarted += OnOnlineFetchStarted;
            LyricsFetcher.LyricsOnlineFetchFinished += OnOnlineFetchFinished;
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
        public bool Enabled { get => Plugin.config.DisplayLyrics; set => Plugin.config.DisplayLyrics = value; }
        private float _timeOffset;

        [UIValue(nameof(TimeOffset))]
        public float TimeOffset
        {
            get { return _timeOffset; }
            set
            {
                if (_timeOffset == value) return;
                _timeOffset = value;
                if (Subtitles != null)
                    Subtitles.TimeOffset = value;
                NotifyPropertyChanged();
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
            get { return _configEnabled; }
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
            get => _fetchEnabled && !ConfigEnabled;
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
                if (_subtitles == value) return;
                _subtitles = value;
                if (value != null)
                {
                    _timeOffset = Subtitles.TimeOffset;
                    _timeScale = Subtitles.TimeScale;
                    ConfigEnabled = true;
                }
                else
                {
                    _timeOffset = 0;
                    _timeScale = 1;
                    ConfigEnabled = false;
                }
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(TimeOffset));
                NotifyPropertyChanged(nameof(TimeScale));
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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
