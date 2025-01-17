#if UNITY_EDITOR || UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR && UNITY_ANDROID
using Unity.Android.Types;
#endif

namespace UnityEngine.AddressableAssets.Android
{
    /// <summary>
    /// Serializable representation of 'Unity.Android.Types.AndroidAssetPackDeliveryType'.
    /// </summary>
    public enum DeliveryType
    {
        /// <summary>
        /// No delivery type specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Content is downloaded when the app is installed.
        /// </summary>
        InstallTime = 1,

        /// <summary>
        /// Content is downloaded automatically as soon as the the app is installed.
        /// </summary>
        FastFollow = 2,

        /// <summary>
        /// Content is downloaded while the app is running.
        /// </summary>
        OnDemand = 3
    }

    internal class CustomAssetPackUtility
    {
        internal const string kBuildFolderName = "Build";

        internal const string kBuildProcessorDataFilename = "BuildProcessorData.json";
        internal const string kCustomAssetPackDataFilename = "CustomAssetPacksData.json";

        internal const string kAddressablesAssetPackName = "AddressablesAssetPack";

        internal static string RootDirectory => Path.Combine("Assets", "PlayAssetDelivery");

        internal static string BuildRootDirectory => Path.Combine(RootDirectory, kBuildFolderName);

        internal static string BuildProcessorDataPath => Path.Combine(BuildRootDirectory, Addressables.StreamingAssetsSubFolder, kBuildProcessorDataFilename);

        internal static string CustomAssetPacksDataEditorPath => Path.Combine(BuildRootDirectory, Addressables.StreamingAssetsSubFolder, kCustomAssetPackDataFilename);

        internal static string CustomAssetPacksDataRuntimePath => Path.Combine(Application.streamingAssetsPath, Addressables.StreamingAssetsSubFolder, kCustomAssetPackDataFilename);

        internal static string CustomAssetPacksAssetsPath => $"src/main/assets/{Addressables.StreamingAssetsSubFolder}";

        internal static readonly string[] InstallTimeFilesMasks =
        {
            "*unitybuiltinshaders*.bundle",
            "*unitybuiltinassets*.bundle",
            "*monoscripts*.bundle",
            "settings.json",
            "catalog.json",
            "catalog.bundle",
            "catalog.bin"
        };

#if UNITY_EDITOR && UNITY_ANDROID
        static readonly Dictionary<DeliveryType, AndroidAssetPackDeliveryType> k_DeliveryTypeToGradleString = new Dictionary<DeliveryType, AndroidAssetPackDeliveryType>()
        {
            { DeliveryType.InstallTime, AndroidAssetPackDeliveryType.InstallTime },
            { DeliveryType.FastFollow, AndroidAssetPackDeliveryType.FastFollow },
            { DeliveryType.OnDemand, AndroidAssetPackDeliveryType.OnDemand },
        };

        internal static string DeliveryTypeToGradleString(DeliveryType deliveryType)
        {
            return k_DeliveryTypeToGradleString[deliveryType].Name;
        }
#endif
    }
}
#endif
