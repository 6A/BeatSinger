using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.Helpers
{
    using SimpleJSON;
    using System.Collections;

    /// <summary>
    ///   Container for subtitles.
    /// </summary>
    public sealed class SubtitleContainer : IEnumerable<Subtitle>
    {
        public static SubtitleContainer Empty => new SubtitleContainer();

        /// <summary>
        /// Amount of time to offset subtitle entries by.
        /// </summary>
        public float TimeOffset { get; set; }
        /// <summary>
        /// Speed up/slow down subtitles by this %.
        /// </summary>
        public float TimeScale { get; set; } = 1;
        public int Count => _subtitles?.Length ?? 0;

        public Subtitle[] Subtitles => _subtitles.ToArray();

        private Subtitle[] _subtitles;

        private SubtitleContainer()
        {
            _subtitles = Array.Empty<Subtitle>();
        }

        public SubtitleContainer(IEnumerable<Subtitle> subtitles, float timeOffset = 0, float timeScale = 1)
        {
            _subtitles = subtitles.ToArray();
            TimeOffset = timeOffset;
            TimeScale = timeScale;
        }

        public void SetSubtitles(IEnumerable<Subtitle> subtitles)
        {
            _subtitles = subtitles.ToArray();
        }

        public IEnumerator<Subtitle> GetEnumerator()
        {
            Subtitle[] subtitles = _subtitles;
            for (int i = 0; i < subtitles.Length; i++)
            {
                float? endTime = subtitles[i].EndTime;
                if (endTime != null)
                    endTime = ModifyTime(endTime.Value);
                else if (i < subtitles.Length - 1)
                {
                    endTime = ModifyTime(subtitles[i + 1].Time);
                }

                yield return new Subtitle(subtitles[i].Text, ModifyTime(subtitles[i].Time), endTime);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private float ModifyTime(float time) => (time + TimeOffset) * TimeScale;
    }


    /// <summary>
    ///   Defines a subtitle.
    /// </summary>
    public struct Subtitle
    {
        public string Text { get; }
        public float Time { get; }
        public float? EndTime { get; }

        public Subtitle(JSONNode node)
        {
            JSONNode time = node["time"];
            EndTime = null;
            if (time == null)
                throw new Exception("Subtitle did not have a 'time' property.");

            Text = node["text"];

            if (time.IsNumber)
            {
                Time = time;

                if (node["end"])
                    EndTime = node["end"];
            }
            else
            {
                Time = time["total"];
            }
        }

        public Subtitle(string text, float time, float? end = null)
        {
            Text = text;
            Time = time;
            EndTime = end;
        }
    }

}
