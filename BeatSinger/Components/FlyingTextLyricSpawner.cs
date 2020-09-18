using BeatSinger.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSinger.Components
{
    public class FlyingTextLyricSpawner : ILyricSpawner
    {
        FlyingTextSpawner textSpawner;

        public FlyingTextLyricSpawner(FlyingTextSpawner spawner) => textSpawner = spawner ?? throw new ArgumentNullException(nameof(spawner));

        public void SpawnText(string text, float duration) => SpawnText(text, duration, false, null, Plugin.config.TextSize);

        public void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize)
        {
            // Little hack to spawn text for a chosen duration in seconds:
            // Save the initial float _duration field to a variable,
            // then set it to the chosen duration, call SpawnText, and restore the
            // previously saved duration.
            float initialDuration = Accessors.Access_FlyingTextDuration(ref textSpawner);
            bool initialShake = Accessors.Access_FlyingTextShake(ref textSpawner);
            Color initialcolor = Accessors.Access_FlyingTextColor(ref textSpawner);
            float initialSize = Accessors.Access_FlyingTextFontSize(ref textSpawner);
#if DEBUG
            if (Plugin.config.VerboseLogging)
            {
                Plugin.log?.Info($"Text Settings:");
                Plugin.log?.Info($"       Duration: {duration}");
                Plugin.log?.Info($"   ShakeEnabled: {(enableShake ? "True" : "False")}");
                Plugin.log?.Info($"          Color: {(color?.ToString() ?? "Default")}");
                Plugin.log?.Info($"           Size: {fontSize}");
            }
#endif
            if (duration <= 0)
            {
                Plugin.log?.Warn($"Text '{text}' has a duration less than 0. Using 1s instead.");
                duration = 1;
            }
            Accessors.Access_FlyingTextDuration(ref textSpawner) = duration;
            Accessors.Access_FlyingTextShake(ref textSpawner) = enableShake;
            if (color.HasValue)
                Accessors.Access_FlyingTextColor(ref textSpawner) = color.Value;
            if (fontSize > 0)
                Accessors.Access_FlyingTextFontSize(ref textSpawner) = fontSize;

            textSpawner.SpawnText(Plugin.config.Position, Quaternion.identity, Quaternion.Inverse(Quaternion.identity), text);

            // Reset values
            Accessors.Access_FlyingTextDuration(ref textSpawner) = initialDuration;
            Accessors.Access_FlyingTextShake(ref textSpawner) = initialShake;
            Accessors.Access_FlyingTextColor(ref textSpawner) = initialcolor;
            Accessors.Access_FlyingTextFontSize(ref textSpawner) = initialSize;
        }
    }
}
