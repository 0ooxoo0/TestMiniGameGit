#if UNITY_ANDROID || UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.AddressableAssets.Android
{
    /// <summary>
    /// Stores runtime data for loading content from asset packs.
    /// </summary>
    class PlayAssetDeliveryRuntimeData
    {
        Dictionary<string, string> m_AssetPackNameToDownloadPath;
        Dictionary<string, CustomAssetPackDataEntry> m_BundleNameToAssetPack;
        static PlayAssetDeliveryRuntimeData s_Instance = null;

        /// <summary>
        /// Reference to the singleton object.
        /// </summary>
        internal static PlayAssetDeliveryRuntimeData Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PlayAssetDeliveryRuntimeData();
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// Maps an asset bundle name to the name of its assigned asset pack.
        /// </summary>
        internal Dictionary<string, CustomAssetPackDataEntry> BundleNameToAssetPack => m_BundleNameToAssetPack;

        /// <summary>
        /// Maps an asset pack name to the location where it has been downloaded.
        /// </summary>
        internal Dictionary<string, string> AssetPackNameToDownloadPath => m_AssetPackNameToDownloadPath;

        /// <summary>
        /// Creates a new PlayAssetDeliveryRuntimeData object.
        /// </summary>
        internal PlayAssetDeliveryRuntimeData()
        {
            m_AssetPackNameToDownloadPath = new Dictionary<string, string>();
            m_BundleNameToAssetPack = new Dictionary<string, CustomAssetPackDataEntry>();
        }
    }
}
#endif
