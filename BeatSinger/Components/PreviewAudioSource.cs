using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSinger.Components
{
    public class PreviewAudioSource : IAudioSource
    {
        private AudioClip AudioClip;
        private AudioSource[] AudioSources;
        private AudioSource PlayingSource => AudioSources.Where(s => s.isPlaying).OrderByDescending(s => s.time).FirstOrDefault();
        public float songTime => PlayingSource?.time ?? 0;

        public float timeScale => 1f;

        public float songEndTime => AudioClip.length;

        public PreviewAudioSource(AudioClip audioClip, AudioSource[] audioSources)
        {
            AudioClip = audioClip;
            AudioSources = audioSources;
        }
    }
}
