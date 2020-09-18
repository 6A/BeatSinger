using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.Components
{
    public class AudioTimeSyncWrapper : IAudioSource
    {
        private AudioTimeSyncController TimeSyncController;
        public AudioTimeSyncWrapper(AudioTimeSyncController controller)
        {
            TimeSyncController = controller;
        }

        public float songTime => TimeSyncController.songTime;

        public float timeScale => TimeSyncController.timeScale;

        public float songEndTime => TimeSyncController.songEndTime;
    }
}
