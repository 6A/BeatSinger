using System;
using System.Collections.Generic;
using System.Linq;

using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSinger.Components;
using BeatSinger.Helpers;
using UnityEngine;

namespace BeatSinger.UI
{
    [HotReload]
    internal class LyricsViewController : BSMLAutomaticViewController, ILyricSpawner
    {
        // For this method of setting the ResourceName, this class must be the first class in the file.
        //public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);
        public event EventHandler CloseClicked;
        [UIValue(nameof(TimeSpanFormatter))]
        public readonly TimeSpanFormatter TimeSpanFormatter = new TimeSpanFormatter();

        [UIAction("CloseClicked")]
        public void OnCloseClicked()
        {
            Clear();
            CloseClicked.RaiseEventSafe(this, nameof(CloseClicked));
        }

        private int FontSizeOffset = 5;
        private DateTime CurrentTextEndTime = DateTime.MinValue;
        private DateTime CurrentTextStartTime = DateTime.MinValue;

        private string _currentText;
        public string CurrentText
        {
            get { return _currentText; }
            set
            {
                if (_currentText == value) return;
                _currentText = value;
                NotifyPropertyChanged();
            }
        }

        public TimeSpan FromStart => CurrentTextStartTime == DateTime.MinValue ? TimeSpan.MinValue : DateTime.UtcNow - CurrentTextStartTime;
        public TimeSpan ToEnd => CurrentTextEndTime == DateTime.MinValue ? TimeSpan.MinValue : DateTime.UtcNow - CurrentTextEndTime;

        private float _currentTextSize = 20;

        public float CurrentTextSize
        {
            get { return _currentTextSize; }
            set
            {
                if (_currentTextSize == value) return;
                _currentTextSize = value;
                NotifyPropertyChanged();
            }
        }

        private Color _currentTextColor;

        public Color CurrentTextColorRaw
        {
            get { return _currentTextColor; }
            set
            {
                if (_currentTextColor == value) return;
                _currentTextColor = value;
                NotifyPropertyChanged(nameof(CurrentTextColor));
            }
        }
        public string CurrentTextColor => $"#{ColorUtility.ToHtmlStringRGBA(CurrentTextColorRaw)}";


        int poll;
        private void Update()
        {
            if (DateTime.UtcNow >= CurrentTextEndTime)
            {
                CurrentText = "";
            }
            if(poll % 10 == 0)
            {
                poll = 0;
                NotifyPropertyChanged(nameof(FromStart));
                NotifyPropertyChanged(nameof(ToEnd));
            }
            poll++;
        }

        public void Clear()
        {
            CurrentText = null;
            CurrentTextStartTime = DateTime.MinValue;
            CurrentTextEndTime = DateTime.MinValue;
        }
        public void SpawnText(string text, float duration) => SpawnText(text, duration, false, null, 4f);

        public void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize)
        {
            CurrentTextStartTime = DateTime.UtcNow;
            CurrentTextEndTime = CurrentTextStartTime + TimeSpan.FromSeconds(duration);
            if (color.HasValue)
                CurrentTextColorRaw = color.Value;
            CurrentTextSize = fontSize + FontSizeOffset;
            CurrentText = text;
        }
    }
}
