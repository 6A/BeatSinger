using System;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;
using BeatSinger.Helpers;
using BS_Utils.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSinger
{
    public class Settings : Config
    {
        public static readonly Color DefaultColor = new Color(0, 190, 255, 255);
        public event EventHandler SettingsReloaded;
        public event EventHandler EnabledChanged;
        private static Vector3 _position;
        public const string ModName = "BeatSinger";
        public const string Section_Mod = "General";
        public const string Section_TextStyle = "Text Style";

        private bool _displayLyrics;

        [UIValue(nameof(DisplayLyrics))]
        public bool DisplayLyrics
        {
            get { return _displayLyrics; }
            set
            {
                if (_displayLyrics == value) return;
                _displayLyrics = value;
                EnabledChanged.RaiseEventSafe(this, nameof(EnabledChanged));
            }
        }


        [UIValue(nameof(ToggleKeyCode))]
        public int ToggleKeyCode { get; set; }
        [UIValue(nameof(DisplayDelay))]
        public  float DisplayDelay { get; set; }
        [UIValue(nameof(HideDelay))]
        public  float HideDelay { get; set; }
        [UIValue(nameof(SaveFetchedLyrics))]
        public  bool SaveFetchedLyrics { get; set; }
        [UIValue(nameof(EnableShake))]
        public  bool EnableShake { get; set; }
        [UIValue(nameof(TextSize))]
        public  float TextSize { get; set; }
        [UIValue(nameof(TextColor))]
        public  Color TextColor
        {
            get
            {
                for (int i = 0; i < ColorAry.Length; i++)
                {
                    if (ColorAry[i] < 0)
                        return DefaultColor;
                }
                return new Color(
                    ColorAry[0] / 255f, // R
                    ColorAry[1] / 255f, // G
                    ColorAry[2] / 255f, // B
                    ColorAry[3] / 255f  // A
                    );
            }
            set
            {
                ColorAry[0] = (int)(value.r * 255);
                ColorAry[1] = (int)(value.g * 255);
                ColorAry[2] = (int)(value.b * 255);
                ColorAry[3] = (int)(value.a * 255);
            }
        }

        public  Vector3 Position { get => _position; set => _position = value; }

        [UIValue(nameof(PosX))]
        internal  float PosX { get => _position.x; set => _position.x = value; }
        [UIValue(nameof(PosY))]
        internal  float PosY { get => _position.y; set => _position.y = value; }
        [UIValue(nameof(PosZ))]
        internal  float PosZ { get => _position.z; set => _position.z = value; }

        [UIValue(nameof(VerboseLogging))]
        public  bool VerboseLogging { get; set; }
        // RGBA
        private  readonly int[] ColorAry = new int[] { 0, 190, 255, 255 };

        public Settings()
            : base(ModName)
        {

        }
        [UIAction("#apply")]
        public void Save()
        {
            if (VerboseLogging)
                Plugin.log?.Debug($"Saving config.");
            SetBool(Section_Mod, "Enabled", DisplayLyrics);
            SetInt(Section_Mod, nameof(ToggleKeyCode), ToggleKeyCode);
            SetFloat(Section_Mod, nameof(DisplayDelay), DisplayDelay);
            SetFloat(Section_Mod, nameof(HideDelay), HideDelay);
            SetBool(Section_Mod, nameof(SaveFetchedLyrics), SaveFetchedLyrics);
            if (VerboseLogging)
                SetBool(Section_Mod, nameof(VerboseLogging), true);
            SetBool(Section_TextStyle, nameof(EnableShake), EnableShake);
            SetFloat(Section_TextStyle, nameof(TextSize), TextSize);
            SetInt(Section_TextStyle, "ColorR", (int)(TextColor.r * 255f));
            SetInt(Section_TextStyle, "ColorG", (int)(TextColor.g * 255f));
            SetInt(Section_TextStyle, "ColorB", (int)(TextColor.b * 255f));
            SetInt(Section_TextStyle, "ColorA", (int)(TextColor.a * 255f));
            SetFloat(Section_TextStyle, "PosX", Position.x);
            SetFloat(Section_TextStyle, "PosY", Position.y);
            SetFloat(Section_TextStyle, "PosZ", Position.z);
        }

        public void Load()
        {
            if (VerboseLogging)
                Plugin.log?.Debug($"Loading config.");
            int defaultKeycode;

            if (XRDevice.model.IndexOf("rift", StringComparison.InvariantCultureIgnoreCase) != -1)
                defaultKeycode = (int)ConInput.Oculus.LeftThumbstickPress;
            else if (XRDevice.model.IndexOf("vive", StringComparison.InvariantCultureIgnoreCase) != -1)
                defaultKeycode = (int)ConInput.Vive.LeftTrackpadPress;
            else
                defaultKeycode = (int)ConInput.WinMR.LeftThumbstickPress;

            DisplayLyrics = GetBool(Section_Mod, "Enabled", true, true);
            ToggleKeyCode = GetInt(Section_Mod, nameof(ToggleKeyCode), defaultKeycode, true);
            DisplayDelay = GetFloat(Section_Mod, nameof(DisplayDelay), -.1f, true);
            HideDelay = GetFloat(Section_Mod, nameof(HideDelay), 0f, true);
            SaveFetchedLyrics = GetBool(Section_Mod, nameof(SaveFetchedLyrics), true, true);
            VerboseLogging = GetBool(Section_Mod, nameof(VerboseLogging), false, true);

            EnableShake = GetBool(Section_TextStyle, nameof(EnableShake), false, true);
            TextSize = GetFloat(Section_TextStyle, nameof(TextSize), 4f, true);

            TextColor = new Color(
                GetInt(Section_TextStyle, "ColorR", 0, true) / 255f,
                GetInt(Section_TextStyle, "ColorG", 190, true) / 255f,
                GetInt(Section_TextStyle, "ColorB", 255, true) / 255f,
                GetInt(Section_TextStyle, "ColorA", 255, true) / 255f);
            Position = new Vector3(
                GetFloat(Section_TextStyle, "PosX", 0, true),
                GetFloat(Section_TextStyle, "PosY", 4, true),
                GetFloat(Section_TextStyle, "PosZ", 0, true));
            SettingsReloaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
