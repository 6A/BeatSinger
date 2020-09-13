using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Parser;
using BeatSinger.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.UI
{
    public class ModifiersConfig : INotifiableHost
    {
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
            }
        }

        private bool _fetchEnabled;

        public bool FetchEnabled
        {
            get { return _fetchEnabled; }
            set
            {
                if (_fetchEnabled == value) return;
                _fetchEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private SubtitleContainer _subtitles;

        public SubtitleContainer Subtitles
        {
            get { return _subtitles; }
            set
            {
                if (_subtitles == value) return;
                _subtitles = value;
                if(value != null)
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



        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
