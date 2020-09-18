using HMUI;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace BeatSinger.Helpers
{
    public static class Accessors
    {
        public static readonly FieldAccessor<GameSongController, AudioTimeSyncController>.Accessor Access_AudioTimeSync
            = FieldAccessor<GameSongController, AudioTimeSyncController>.GetAccessor("_audioTimeSyncController");

        public static readonly FieldAccessor<GameplayCoreSceneSetup, GameplayCoreSceneSetupData>.Accessor Access_SceneSetupData
            = FieldAccessor<GameplayCoreSceneSetup, GameplayCoreSceneSetupData>.GetAccessor("_sceneSetupData");


        public static readonly FieldAccessor<MonoInstallerBase, DiContainer>.Accessor Access_DiContainer
            = FieldAccessor<MonoInstallerBase, DiContainer>.GetAccessor("<Container>k__BackingField");

        public static readonly FieldAccessor<FlyingTextSpawner, FlyingTextEffect.Pool>.Accessor Access_FlyingTextEffectPool
            = FieldAccessor<FlyingTextSpawner, FlyingTextEffect.Pool>.GetAccessor("_flyingTextEffectPool");


        public static readonly FieldAccessor<FlyingTextSpawner, float>.Accessor Access_FlyingTextDuration
            = FieldAccessor<FlyingTextSpawner, float>.GetAccessor("_duration");

        public static readonly FieldAccessor<FlyingTextSpawner, bool>.Accessor Access_FlyingTextShake
            = FieldAccessor<FlyingTextSpawner, bool>.GetAccessor("_shake");

        public static readonly FieldAccessor<FlyingTextSpawner, float>.Accessor Access_FlyingTextXSpread
            = FieldAccessor<FlyingTextSpawner, float>.GetAccessor("_xSpread");

        public static readonly FieldAccessor<FlyingTextSpawner, Color>.Accessor Access_FlyingTextColor
            = FieldAccessor<FlyingTextSpawner, Color>.GetAccessor("_color");

        public static readonly FieldAccessor<FlyingTextSpawner, float>.Accessor Access_FlyingTextFontSize
            = FieldAccessor<FlyingTextSpawner, float>.GetAccessor("_fontSize");

        public static readonly FieldAccessor<SongPreviewPlayer, AudioSource[]>.Accessor Access_PreviewPlayerAudioSources
            = FieldAccessor<SongPreviewPlayer, AudioSource[]>.GetAccessor("_audioSources");

        public static readonly FieldAccessor<FlowCoordinator, ScreenSystem>.Accessor Access_ScreenSystem
            = FieldAccessor<FlowCoordinator, ScreenSystem>.GetAccessor("_screenSystem");
    }
}
