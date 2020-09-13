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
            Instance.SetBool(ModName, "Enabled", DisplayLyrics);
            Instance.SetInt(ModName, nameof(ToggleKeyCode), ToggleKeyCode);
            Instance.SetFloat(ModName, nameof(DisplayDelay), DisplayDelay);
            Instance.SetFloat(ModName, nameof(HideDelay), HideDelay);
            Instance.SetBool(ModName, nameof(SaveFetchedLyrics), SaveFetchedLyrics);
            Instance.SetBool(ModName, nameof(EnableShake), EnableShake);
            Instance.SetFloat(ModName, nameof(TextSize), TextSize);
            Instance.SetInt(ModName, "ColorR", (int)(TextColor.r * 255f));
            Instance.SetInt(ModName, "ColorG", (int)(TextColor.g * 255f));
            Instance.SetInt(ModName, "ColorB", (int)(TextColor.b * 255f));
            Instance.SetInt(ModName, "ColorA", (int)(TextColor.a * 255f));
            Instance.SetFloat(ModName, "PosX", Position.x);
            Instance.SetFloat(ModName, "PosY", Position.y);
            Instance.SetFloat(ModName, "PosZ", Position.z);
            if (VerboseLogging)
                Instance.SetBool(ModName, nameof(VerboseLogging), true);
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

            DisplayLyrics = Instance.GetBool(ModName, "Enabled", true);
            ToggleKeyCode = Instance.GetInt(ModName, nameof(ToggleKeyCode), defaultKeycode);
            DisplayDelay = Instance.GetFloat(ModName, nameof(DisplayDelay), -.1f);
            HideDelay = Instance.GetFloat(ModName, nameof(HideDelay), 0f);
            SaveFetchedLyrics = Instance.GetBool(ModName, nameof(SaveFetchedLyrics), true);

            TextSize = Instance.GetFloat(ModName, nameof(TextSize), 4f, true);
            TextColor = new Color(
                Instance.GetInt(ModName, "ColorR", 0, true) / 255f,
                Instance.GetInt(ModName, "ColorG", 190, true) / 255f,
                Instance.GetInt(ModName, "ColorB", 255, true) / 255f,
                Instance.GetInt(ModName, "ColorA", 255, true) / 255f);
            Position = new Vector3(
                Instance.GetFloat(ModName, "PosX", 0, true),
                Instance.GetFloat(ModName, "PosY", 4, true),
                Instance.GetFloat(ModName, "PosZ", 0, true));

            EnableShake = Instance.GetBool(ModName, nameof(EnableShake), false, true);
            VerboseLogging = Instance.GetBool(ModName, nameof(VerboseLogging), false);
        }
    }
}
