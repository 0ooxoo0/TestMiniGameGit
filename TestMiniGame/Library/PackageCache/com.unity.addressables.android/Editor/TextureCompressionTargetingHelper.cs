using System;
using UnityEditor.Build;

namespace UnityEditor.AddressableAssets.Android
{
    internal static class TextureCompressionTargetingHelper
    {
        internal static bool EnabledTextureCompressionTargeting => PlayerSettings.Android.textureCompressionFormats.Length > 1 &&
            EditorUserBuildSettings.overrideTextureCompression != OverrideTextureCompression.ForceUncompressed;

        internal static bool UseAssetPacks => EditorUserBuildSettings.buildAppBundle && (PlayerSettings.Android.splitApplicationBinary || EnabledTextureCompressionTargeting);

        internal static bool IsCurrentTextureCompressionDefault => EnabledTextureCompressionTargeting &&
            EditorUserBuildSettings.androidBuildSubtarget == ConvertToMobileTextureSubtarget(PlayerSettings.Android.textureCompressionFormats[0]);

        internal static string TcfPostfix(MobileTextureSubtarget subtarget)
        {
            return subtarget switch
            {
                MobileTextureSubtarget.ETC => "#tcf_etc1",
                MobileTextureSubtarget.ETC2 => "#tcf_etc2",
                MobileTextureSubtarget.ASTC => "#tcf_astc",
                MobileTextureSubtarget.PVRTC => "#tcf_pvrtc",
                MobileTextureSubtarget.DXT => "#tcf_dxt1",
                _ => throw new ArgumentException($"{subtarget} is not supported by TCFT")
            };
        }

        internal static string TcfPostfix(TextureCompressionFormat compression)
        {
            return TcfPostfix(ConvertToMobileTextureSubtarget(compression));
        }

        internal static string TcfPostfix()
        {
            return EnabledTextureCompressionTargeting ? TcfPostfix(EditorUserBuildSettings.androidBuildSubtarget) : "";
        }

        internal static MobileTextureSubtarget ConvertToMobileTextureSubtarget(TextureCompressionFormat compression)
        {
            return compression switch
            {
                TextureCompressionFormat.ETC => MobileTextureSubtarget.ETC,
                TextureCompressionFormat.ETC2 => MobileTextureSubtarget.ETC2,
                TextureCompressionFormat.ASTC => MobileTextureSubtarget.ASTC,
                TextureCompressionFormat.PVRTC => MobileTextureSubtarget.PVRTC,
                TextureCompressionFormat.DXTC => MobileTextureSubtarget.DXT,
                TextureCompressionFormat.DXTC_RGTC => MobileTextureSubtarget.DXT,
                _ => throw new ArgumentException($"{compression} is not supported by TCFT")
            };
        }
    }
}
