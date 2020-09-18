using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSinger.Components
{
    public interface ILyricSpawner
    {
        void SpawnText(string text, float duration);
        void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize);
    }
}
