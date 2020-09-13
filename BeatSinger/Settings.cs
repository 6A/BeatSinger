using System;
using BS_Utils.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSinger
{
    public static class Settings
    {
        public static readonly Color DefaultColor = new Color(0, 190, 255, 255);

        public static Config Instance;

        public const string ModName = "BeatSinger";
        public const string Section_Mod = "General";
        public const string Section_TextStyle = "Text Style";

        public static bool DisplayLyrics { get; set; }
        public static int ToggleKeyCode { get; set; }
        public static float DisplayDelay { get; set; }
        public static float HideDelay { get; set; }
        public static bool SaveFetchedLyrics { get; set; }
        public static bool EnableShake { get; set; }
        public static float TextSize { get; set; }
        public static Color TextColor
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

        public static Vector3 Position { get; set; }

        public static bool VerboseLogging { get; set; }
        // RGBA
        private static readonly int[] ColorAry = new int[] { 0, 190, 255, 255 };

        public static void Save()
        {
            Instance.SetBool(Section_Mod, "Enabled", DisplayLyrics);
            Instance.SetInt(Section_Mod, nameof(ToggleKeyCode), ToggleKeyCode);
            Instance.SetFloat(Section_Mod, nameof(DisplayDelay), DisplayDelay);
            Instance.SetFloat(Section_Mod, nameof(HideDelay), HideDelay);
            Instance.SetBool(Section_Mod, nameof(SaveFetchedLyrics), SaveFetchedLyrics);
            if (VerboseLogging)
                Instance.SetBool(Section_Mod, nameof(VerboseLogging), true);
            Instance.SetBool(Section_TextStyle, nameof(EnableShake), EnableShake);
            Instance.SetFloat(Section_TextStyle, nameof(TextSize), TextSize);
            Instance.SetInt(Section_TextStyle, "ColorR", (int)(TextColor.r * 255f));
            Instance.SetInt(Section_TextStyle, "ColorG", (int)(TextColor.g * 255f));
            Instance.SetInt(Section_TextStyle, "ColorB", (int)(TextColor.b * 255f));
            Instance.SetInt(Section_TextStyle, "ColorA", (int)(TextColor.a * 255f));
            Instance.SetFloat(Section_TextStyle, "PosX", Position.x);
            Instance.SetFloat(Section_TextStyle, "PosY", Position.y);
            Instance.SetFloat(Section_TextStyle, "PosZ", Position.z);
        }

        public static void Load()
        {
            if (Instance == null)
                Instance = new Config(ModName);
            int defaultKeycode;

            if (XRDevice.model.IndexOf("rift", StringComparison.InvariantCultureIgnoreCase) != -1)
                defaultKeycode = (int)ConInput.Oculus.LeftThumbstickPress;
            else if (XRDevice.model.IndexOf("vive", StringComparison.InvariantCultureIgnoreCase) != -1)
                defaultKeycode = (int)ConInput.Vive.LeftTrackpadPress;
            else
                defaultKeycode = (int)ConInput.WinMR.LeftThumbstickPress;

            DisplayLyrics = Instance.GetBool(Section_Mod, "Enabled", true, true);
            ToggleKeyCode = Instance.GetInt(Section_Mod, nameof(ToggleKeyCode), defaultKeycode, true);
            DisplayDelay = Instance.GetFloat(Section_Mod, nameof(DisplayDelay), -.1f, true);
            HideDelay = Instance.GetFloat(Section_Mod, nameof(HideDelay), 0f, true);
            SaveFetchedLyrics = Instance.GetBool(Section_Mod, nameof(SaveFetchedLyrics), true, true);
            VerboseLogging = Instance.GetBool(Section_Mod, nameof(VerboseLogging), false, true);

            EnableShake = Instance.GetBool(Section_TextStyle, nameof(EnableShake), false, true);
            TextSize = Instance.GetFloat(Section_TextStyle, nameof(TextSize), 4f, true);

            TextColor = new Color(
                Instance.GetInt(Section_TextStyle, "ColorR", 0, true) / 255f,
                Instance.GetInt(Section_TextStyle, "ColorG", 190, true) / 255f,
                Instance.GetInt(Section_TextStyle, "ColorB", 255, true) / 255f,
                Instance.GetInt(Section_TextStyle, "ColorA", 255, true) / 255f);
            Position = new Vector3(
                Instance.GetFloat(Section_TextStyle, "PosX", 0, true),
                Instance.GetFloat(Section_TextStyle, "PosY", 4, true),
                Instance.GetFloat(Section_TextStyle, "PosZ", 0, true));
        }
    }
}
