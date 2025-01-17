#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.Android
{
    /// <summary>
    /// IInitializableObject that configures Addressables for loading content from asset packs.
    /// </summary>
    [Serializable]
    public class PlayAssetDeliveryInitialization : IInitializableObject
    {
        /// <inheritdoc/>
        public bool Initialize(string id, string data)
        {
            return true;
        }

        /// <summary>
        /// Determines whether warnings should be logged during initialization.
        /// </summary>
        /// <param name="data">The JSON serialized <see cref="PlayAssetDeliveryInitializationData"/> object</param>
        /// <returns>True to log warnings, otherwise returns false. Default value is true.</returns>
        public bool LogWarnings(string data)
        {
            var initializeData = JsonUtility.FromJson<PlayAssetDeliveryInitializationData>(data);
            if (initializeData != null)
            {
                return initializeData.LogWarnings;
            }
            return true;
        }

        /// <inheritdoc/>
        public virtual AsyncOperationHandle<bool> InitializeAsync(ResourceManager rm, string id, string data)
        {
            var op = new PlayAssetDeliveryInitializeOperation();
            return op.Start(rm, LogWarnings(data));
        }
    }

    /// <summary>
    /// Configures Addressables for loading content from asset packs
    /// </summary>
    public class PlayAssetDeliveryInitializeOperation : AsyncOperationBase<bool>
    {
        bool m_LogWarnings = false;

        bool m_IsDone = false;
        bool m_HasExecuted = false;

        public AsyncOperationHandle<bool> Start(ResourceManager rm, bool logWarnings)
        {
            m_LogWarnings = logWarnings;
            return rm.StartOperation(this, default);
        }

        protected override bool InvokeWaitForCompletion()
        {
            if (!m_HasExecuted)
            {
                Execute();
            }
            return m_IsDone;
        }

        void CompleteOverride(string warningMsg)
        {
            if (m_LogWarnings && warningMsg != null)
            {
                Debug.LogWarning($"{warningMsg} Default internal id locations will be used instead.");
            }
            Complete(true, true, "");
            m_IsDone = true;
        }

        protected override void Execute()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            DownloadCustomAssetPacksData();
#else
            CompleteOverride(null);
#endif
            m_HasExecuted = true;
        }

        void DownloadCustomAssetPacksData()
        {
            // CustomAssetPacksDataRuntimePath file is always in install-time AddressablesAssetPack (if split binary is on),
            // or in the main APK (if split binary is off). So there is no need to check for core asset packs status before accessing it.
            var www = UnityWebRequest.Get(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath);
            www.SendWebRequest().completed += (op) =>
            {
                var www = (op as UnityWebRequestAsyncOperation).webRequest;
                if (www.result != UnityWebRequest.Result.Success)
                {
                    CompleteOverride($"Could not load '{CustomAssetPackUtility.kCustomAssetPackDataFilename}' : {www.error}.");
                }
                else
                {
                    if (InitializeBundleToAssetPackMap(www.downloadHandler.text))
                    {
                        Addressables.ResourceManager.InternalIdTransformFunc = AppBundleTransformFunc;
                        CompleteOverride(null);
                    }
                    else
                    {
                        CompleteOverride($"'{CustomAssetPackUtility.kCustomAssetPackDataFilename}' is broken");
                    }
                }
            };
        }

        bool InitializeBundleToAssetPackMap(string contents)
        {
            //var customPackData = JsonUtility.FromJson<CustomAssetPackData>(contents);
            //foreach (CustomAssetPackDataEntry entry in entries)
            //{
            //    foreach (var bundle in entry.AssetBundles)
            //    {
            //        PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.Add(bundle, entry);
            //    }
            //}

            // manual deserialization of CustomAssetPackData, required because JsonUtility.FromJson<CustomAssetPackData> sometimes returns empty Entries list
            // https://jira.unity3d.com/browse/UUM-48734
            const string kEntriesHeader = "{\"Entries\":[";
            if (!contents.StartsWith(kEntriesHeader))
            {
                return false;
            }
            var start = kEntriesHeader.Length;
            var end = start;
            while (true)
            {
                end = contents.IndexOf("]}", start);
                if (end == -1)
                {
                    break;
                }
                var entry = JsonUtility.FromJson<CustomAssetPackDataEntry>(contents.Substring(start, end - start + 2));
                start = end + 3; // ]},
                foreach (var bundle in entry.AssetBundles)
                {
                    PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.Add(bundle, entry);
                }
            }
            return true;
        }

        string AppBundleTransformFunc(IResourceLocation location)
        {
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                var locationId = location.InternalId.Replace("\\", "/");
                var relativePath = Path.GetRelativePath(Application.streamingAssetsPath, locationId);
                var bundleName = Path.GetFileNameWithoutExtension(locationId);
                var assetPackNameToDownloadPath = PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath;
                if (PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    var assetPackName = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
                    if (assetPackNameToDownloadPath.ContainsKey(assetPackName))
                    {
                        if (Directory.Exists(assetPackNameToDownloadPath[assetPackName]))
                        {
                            // Load bundle that was assigned to a custom fast-follow or on-demand asset pack.
                            // PlayAssetDeliveryBundleProvider.Provider previously saved the asset pack path.
                            var ret = Path.Combine(assetPackNameToDownloadPath[assetPackName], relativePath);
                            return ret;
                        }
                        // Downloaded asset pack doesn't exist, most likely it was deleted
                        assetPackNameToDownloadPath.Remove(assetPackName);
                    }
                }
            }
            // Load resource from the default location. The generated asset packs contain streaming assets.
            return location.InternalId;
        }
    }

    /// <summary>
    /// Contains settings for <see cref="PlayAssetDeliveryInitialization"/>.
    /// </summary>
    [Serializable]
    public class PlayAssetDeliveryInitializationData
    {
        [SerializeField]
        bool m_LogWarnings = true;
        /// <summary>
        /// Determines whether warnings should be logged during initialization.
        /// </summary>
        public bool LogWarnings { get { return m_LogWarnings; } set { m_LogWarnings = value; } }
    }
}
#endif
