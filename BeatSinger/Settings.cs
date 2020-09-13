using System;
using BS_Utils.Utilities;
using UnityEngine.XR;

namespace BeatSinger
{
    public static class Settings
    {
        public static Config Instance;

        public const string ModName = "BeatSinger";

        public static bool DisplayLyrics { get; set; }
        public static int  ToggleKeyCode { get; set; }
        public static float DisplayDelay { get; set; }
        public static float HideDelay    { get; set; }
        public static bool SaveFetchedLyrics { get; set; }
        public static bool VerboseLogging { get; set; }

        public static void Save()
        {
            Instance.SetBool (ModName, "Enabled"                , DisplayLyrics);
            Instance.SetInt  (ModName, nameof(ToggleKeyCode)    , ToggleKeyCode);
            Instance.SetFloat(ModName, nameof(DisplayDelay)     , DisplayDelay);
            Instance.SetFloat(ModName, nameof(HideDelay)        , HideDelay);
            Instance.SetBool (ModName, nameof(SaveFetchedLyrics), SaveFetchedLyrics);

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

            DisplayLyrics = Instance.GetBool(ModName, "Enabled"            , true);
            ToggleKeyCode = Instance.GetInt (ModName, nameof(ToggleKeyCode), defaultKeycode);

            DisplayDelay      = Instance.GetFloat(ModName, nameof(DisplayDelay)     , -.1f);
            HideDelay         = Instance.GetFloat(ModName, nameof(HideDelay)        , 0f);
            SaveFetchedLyrics = Instance.GetBool (ModName, nameof(SaveFetchedLyrics), false);

            VerboseLogging = Instance.GetBool(ModName, nameof(VerboseLogging), false);
        }
    }
}
