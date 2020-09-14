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

namespace BeatSinger
{
    /// <summary>
    ///   Defines the main component of BeatSinger, which displays lyrics on loaded songs.
    /// </summary>
    public class LyricsComponent : MonoBehaviour
    {
        public bool Initialized { get; private set; }
        private ILyricSpawner textSpawner;
        private IAudioSource audio;
        private SubtitleContainer Subtitles;

        public void Initialize(IAudioSource source, ILyricSpawner lyricSpawner, SubtitleContainer subtitles)
        {
            audio = source ?? throw new ArgumentNullException(nameof(source));
            textSpawner = lyricSpawner ?? throw new ArgumentNullException(nameof(lyricSpawner));
            Subtitles = subtitles ?? throw new ArgumentNullException(nameof(subtitles));
            Initialized = true;
            enabled = true;
        }

        protected void SpawnText(string text, float duration)
        {
            if (textSpawner != null)
                textSpawner.SpawnText(text, duration);
            else
                Plugin.log?.Warn($"Tried to spawn text, but there is no text spawner.");
        }
        protected void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize)
        {
            if (textSpawner != null)
                textSpawner.SpawnText(text, duration, enableShake, color, fontSize);
            else
                Plugin.log?.Warn($"Tried to spawn text, but there is no text spawner.");
        }

        public virtual void Awake()
        {
            enabled = false;
        }

        public void OnEnable()
        {
            StartCoroutine(DisplayLyrics());
        }

        public void OnDisable()
        {
            StopCoroutine(DisplayLyrics());
        }


        public void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.R))
                Plugin.config.Load();
#endif
            if (!Input.GetKeyUp((KeyCode)Plugin.config.ToggleKeyCode))
                return;

            Plugin.config.DisplayLyrics = !Plugin.config.DisplayLyrics;

            textSpawner.SpawnText(Plugin.config.DisplayLyrics ? "Lyrics enabled" : "Lyrics disabled", 3f);
        }

        protected IEnumerator DisplayLyrics()
        {
            yield return new WaitUntil(() => Initialized);
            SubtitleContainer subtitles = Subtitles;
            if (subtitles == null)
                yield break;
            if (subtitles.Count == 0)
            {
                Plugin.log?.Info("No subtitles to display for this song.");
                yield break;
            }
            int skipped = 0;
            if (Plugin.config.VerboseLogging)
                Plugin.log?.Debug($"{subtitles.Count} lyrics found for song. Displaying with offset {subtitles.TimeOffset}s and a scale of {subtitles.TimeScale:P}");
            // Subtitles are sorted by time of appearance, so we can iterate without sorting first.
            foreach (var subtitle in subtitles)
            {
                float currentTime = audio.songTime;
                float subtitleTime = subtitle.Time + Plugin.config.DisplayDelay * (1 / audio.timeScale);
                // First, skip all subtitles that have already been seen.
                if (currentTime > subtitleTime)
                {
                    skipped++;
                    continue;
                }
                if (skipped > 0)
                {
                    if (Plugin.config.VerboseLogging)
                        Plugin.log?.Debug($"Skipped {skipped} lyrics because they started too soon.");
                    skipped = 0;
                }

                // Wait for time to display next lyrics
                yield return new WaitForSeconds((subtitleTime - currentTime) * (1 / audio.timeScale));
                if (!Plugin.config.DisplayLyrics)
                    // Don't display lyrics this time
                    continue;

                currentTime = audio.songTime;
                float displayDuration = ((subtitle.EndTime ?? audio.songEndTime) - currentTime) * (1 / audio.timeScale);
                if (Plugin.config.VerboseLogging)
                    Plugin.log?.Debug($"At {currentTime} and for {displayDuration} seconds, displaying lyrics \"{subtitle.Text}\".");

                textSpawner.SpawnText(subtitle.Text, displayDuration + Plugin.config.HideDelay, Plugin.config.EnableShake, Plugin.config.TextColor, Plugin.config.TextSize);
            }
        }
    }
}
