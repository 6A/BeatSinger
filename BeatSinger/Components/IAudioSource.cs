using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.Components
{
    public interface IAudioSource
    {
        float songTime { get; }
        float timeScale { get; }
        float songEndTime { get; }

    }
}
