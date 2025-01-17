using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Android;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Exceptions;

namespace UnityEngine.AddressableAssets.Android
{
    /// <summary>
    /// Ensures that the asset pack containing the AssetBundle is installed/downloaded before attemping to load the bundle.
    /// </summary>
    [DisplayName("Play Asset Delivery Provider")]
    public class PlayAssetDeliveryAssetBundleProvider : AssetBundleProvider, IUpdateReceiver
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Dictionary<string, HashSet<ProvideHandle>> m_ProviderInterfaces = new Dictionary<string, HashSet<ProvideHandle>>();
        List<string> m_AssetPackQueue = new List<string>();

        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
        {
            LoadFromAssetPack(providerInterface);
        }

        void LoadFromAssetPack(ProvideHandle providerInterface)
        {
            string bundleName = Path.GetFileNameWithoutExtension(providerInterface.Location.InternalId.Replace("\\", "/"));
            if (!PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
            {
                // Bundle is either assigned to the generated asset packs, or not assigned to any asset pack
                base.Provide(providerInterface);
                return;
            }

            var assetPackNameToDownloadPath = PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath;
            var assetPackName = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
            // Bundle is assigned to install-time AddressablesAssetPack
            if (assetPackName == CustomAssetPackUtility.kAddressablesAssetPackName)
            {
                assetPackNameToDownloadPath.Add(CustomAssetPackUtility.kAddressablesAssetPackName, Application.streamingAssetsPath);
                base.Provide(providerInterface);
                return;
            }
            // Bundle is assigned to the previously downloaded asset pack
            if (assetPackNameToDownloadPath.ContainsKey(assetPackName))
            {
                if (Directory.Exists(assetPackNameToDownloadPath[assetPackName]))
                {
                    base.Provide(providerInterface);
                    return;
                }
                // Downloaded asset pack doesn't exist, most likely it was deleted
                assetPackNameToDownloadPath.Remove(assetPackName);
            }
            // Download the asset pack
            DownloadRemoteAssetPack(providerInterface, assetPackName);
        }

        /// <inheritdoc/>
        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);
            m_ProviderInterfaces.Clear();
        }

        internal override IOperationCacheKey CreateCacheKeyForLocation(ResourceManager rm, IResourceLocation location, Type desiredType)
        {
            return new IdCacheKey(location.InternalId);
        }

        void DownloadRemoteAssetPack(ProvideHandle providerInterface, string assetPackName)
        {
            // Note that most methods in the AndroidAssetPacks class are either direct wrappers of java APIs in Google's PlayCore plugin,
            // or depend on values that the PlayCore API returns. If the PlayCore plugin is missing, calling these methods will throw an InvalidOperationException exception.
            try
            {
                if (!m_ProviderInterfaces.ContainsKey(assetPackName))
                {
                    if (m_AssetPackQueue.Count == 0)
                    {
                        Addressables.ResourceManager.AddUpdateReceiver(this);
                    }
                    m_ProviderInterfaces[assetPackName] = new HashSet<ProvideHandle>();
                    m_AssetPackQueue.Add(assetPackName);
                }
                m_ProviderInterfaces[assetPackName].Add(providerInterface);
            }
            catch (InvalidOperationException ioe)
            {
                m_ProviderInterfaces.Remove(assetPackName);
                var message = $"Cannot retrieve state for asset pack '{assetPackName}'. This might be because PlayCore Plugin is not installed: {ioe.Message}";
                Debug.LogError(message);
                providerInterface.Complete(this, false, new RemoteProviderException(message));
            }
        }

        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            var message = "";
            switch (info.status)
            {
                case AndroidAssetPackStatus.Failed:
                    message = $"Failed to retrieve the state of asset pack '{info.name}'.";
                    break;
                case AndroidAssetPackStatus.Unknown:
                    message = $"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.";
                    break;
                case AndroidAssetPackStatus.Canceled:
                    message = $"Cancelled asset pack download request '{info.name}'.";
                    break;
                case AndroidAssetPackStatus.WaitingForWifi:
                    AndroidAssetPacks.RequestToUseMobileDataAsync(OnRequestToUseMobileDataComplete);
                    break;
                case AndroidAssetPackStatus.Completed:
                    {
                        var assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);
                        if (!string.IsNullOrEmpty(assetPackPath))
                        {
                            // Asset pack was located on device. Proceed with loading the bundle.
                            PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath.Add(info.name, assetPackPath);
                            foreach (var pi in m_ProviderInterfaces[info.name])
                            {
                                base.Provide(pi);
                            }
                            m_ProviderInterfaces.Remove(info.name);
                        }
                        else
                        {
                            message = $"Downloaded asset pack '{info.name}' but cannot locate it on device.";
                        }
                        break;
                    }
            }

            if (!string.IsNullOrEmpty(message))
            {
                Debug.LogError(message);
                foreach (var pi in m_ProviderInterfaces[info.name])
                {
                    pi.Complete(this, false, new RemoteProviderException(message));
                }
                m_ProviderInterfaces.Remove(info.name);
            }
        }

        /// <inheritdoc/>
        public void Update(float unscaledDeltaTime)
        {
            if (m_AssetPackQueue.Count == 0) {
                return;
            }
            AndroidAssetPacks.DownloadAssetPackAsync(m_AssetPackQueue.ToArray(), CheckDownloadStatus);
            m_AssetPackQueue.Clear();
            Addressables.ResourceManager.RemoveUpdateReciever(this);
        }

        void OnRequestToUseMobileDataComplete(AndroidAssetPackUseMobileDataRequestResult result)
        {
            if (!result.allowed)
            {
                var message = "Request to use mobile data was denied.";
                Debug.LogError(message);
                foreach (var p in m_ProviderInterfaces)
                {
                    foreach (var pi in p.Value)
                    {
                        pi.Complete(this, false, new RemoteProviderException(message));
                    }
                }
                m_ProviderInterfaces.Clear();
            }
        }
#else
        /// <inheritdoc/>
        public void Update(float unscaledDeltaTime)
        {
        }
#endif
    }
}
