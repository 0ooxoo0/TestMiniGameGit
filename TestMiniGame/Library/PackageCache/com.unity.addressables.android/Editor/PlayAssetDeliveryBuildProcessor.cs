using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// When building for Android with asset packs support ensures that active player data builder is set to build script which builds for Play Asset Delivery.
    ///
    /// This script executes before the AddressablesPlayerBuildProcessor which depending on AddressableAssetSettings.BuildAddressablesWithPlayerBuild value might call
    /// active data builder to build addressables before building player.
    /// </summary>
    public class PlayAssetDeliveryBuildProcessor : BuildPlayerProcessor
    {
        /// <summary>
        /// Returns the player build processor callback order.
        /// </summary>
        public override int callbackOrder => 0;

        static internal bool BuildingPlayer { get; set; } = false;

        static internal int DataBuilderIndex { get; set; } = -1;

        /// <summary>
        /// Invoked before performing a Player build.
        /// Sets active player data builder to build script which builds for Play Asset Delivery (when building for Android with asset packs support).
        /// Disables adding asset bundles from Build folder to the StreamingAssets.
        /// </summary>
        /// <param name="buildPlayerContext">Contains data related to the player.</param>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                return;
            }
            if (TextureCompressionTargetingHelper.UseAssetPacks)
            {
                if (PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized())
                {
                    Addressables.LogWarning($"Addressables are not initialized to use with Play Asset Delivery. Open '{PlayAssetDeliverySetup.kInitPlayAssetDeliveryMenuItem}'.");
                    return;
                }
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var padBuildScriptIndex = settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery);
                DataBuilderIndex = settings.ActivePlayerDataBuilderIndex;
                settings.ActivePlayerDataBuilderIndex = padBuildScriptIndex;
                AddressablesPlayerBuildProcessor.AddPathToStreamingAssets = (string path) => false;
                BuildingPlayer = true;
            }
        }
    }

    /// <summary>
    /// Checks that Addressables are generated for Play Asset Delivery if required.
    /// Restores active player data builder after building Addressables for Play Asset Delivery.
    /// Adds non-asset packs data to the Streaming Assets.
    ///
    /// This script executes after the AddressablesPlayerBuildProcessor.
    /// </summary>
    public class PlayAssetDeliverySecondBuildProcessor : BuildPlayerProcessor
    {
        /// <summary>
        /// Returns the player build processor callback order.
        /// </summary>
        public override int callbackOrder => 2;

        internal const string kAddressableMustBeBuiltMessage = "Addressables groups must be built before building player";

        HashSet<string> m_FilesAssignedToAssetPacks = new HashSet<string>();

        void AssignBundleFilesToAssetPacks(string postfix)
        {
            var buildProcessorDataPath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", CustomAssetPackUtility.kBuildProcessorDataFilename);
            if (!File.Exists(buildProcessorDataPath))
            {
                return;
            }
            var contents = File.ReadAllText(buildProcessorDataPath);
            var data = JsonUtility.FromJson<BuildProcessorData>(contents);
            m_FilesAssignedToAssetPacks.UnionWith(data.Entries.Select(e => e.BundleBuildPath));
        }

        void AssignInstallTimeFilesToAssetPacks(string postfix)
        {
            var sourcePath = $"{Addressables.BuildPath}{postfix}";
            if (!Directory.Exists(sourcePath))
            {
                return;
            }
            foreach (var mask in CustomAssetPackUtility.InstallTimeFilesMasks)
            {
                m_FilesAssignedToAssetPacks.UnionWith(Directory.EnumerateFiles(sourcePath, mask, SearchOption.AllDirectories));
            }
        }

        void AddFilesToStreamingAssets(BuildPlayerContext buildPlayerContext, string postfix)
        {
            var sourcePath = $"{Addressables.BuildPath}{postfix}";
            if (!Directory.Exists(sourcePath))
            {
                // using default texture compression variant
                sourcePath = Addressables.BuildPath;
            }
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).ToList();
            foreach (var f in files)
            {
                if (!m_FilesAssignedToAssetPacks.Contains(f))
                {
                    var dest = Path.Combine($"{Addressables.StreamingAssetsSubFolder}{postfix}", Path.GetRelativePath(sourcePath, f));
                    buildPlayerContext.AddAdditionalPathToStreamingAssets(f, dest);
                }
            }
        }

        /// <summary>
        /// Invoked before performing a Player build.
        /// Restores active player data builder after building Addressables for Play Asset Delivery.
        /// Checks that addressables hve been built for Play Asset Delivery for all texture compressions.
        /// Moves asset bundles (including texture compression targeted) which are not assigned to any asset pack to the Streaming Assets.
        /// </summary>
        /// <param name="buildPlayerContext">Contains data related to the player.</param>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
                !TextureCompressionTargetingHelper.UseAssetPacks ||
                PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized())
            {
                return;
            }

            AddressablesPlayerBuildProcessor.AddPathToStreamingAssets = null;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (PlayAssetDeliveryBuildProcessor.DataBuilderIndex != -1)
            {
                settings.ActivePlayerDataBuilderIndex = PlayAssetDeliveryBuildProcessor.DataBuilderIndex;
                PlayAssetDeliveryBuildProcessor.DataBuilderIndex = -1;
            }

            // Checking that Addressables have been built for Android for all TC variants, if not - throw exception to break build
            if (settings.groups.FindIndex(g => g.HasSchema<PlayAssetDeliverySchema>() && g.HasSchema<BundledAssetGroupSchema>()) != -1 &&
                TextureCompressionTargetingHelper.UseAssetPacks)
            {
                // check default CustomAssetPacksData.json exist
                var padBuilt = File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath);
                // check default BuildProcessorData.json exist
                padBuilt &= File.Exists(Path.Combine(CustomAssetPackUtility.BuildRootDirectory, Addressables.StreamingAssetsSubFolder, CustomAssetPackUtility.kBuildProcessorDataFilename));
                if (padBuilt && TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting)
                {
                    // check that BuildProcessorData.json and CustomAssetPacksData.json exist for all texture compressions
                    foreach (var tc in PlayerSettings.Android.textureCompressionFormats)
                    {
                        var buildPathWithPostfix = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{TextureCompressionTargetingHelper.TcfPostfix(tc)}");
                        padBuilt &= File.Exists(Path.Combine(buildPathWithPostfix, CustomAssetPackUtility.kBuildProcessorDataFilename));
                        padBuilt &= File.Exists(Path.Combine(buildPathWithPostfix, CustomAssetPackUtility.kCustomAssetPackDataFilename));
                    }
                }
                if (!padBuilt)
                {
                    throw new Exception(kAddressableMustBeBuiltMessage);
                }
            }

            // Still need to add files which are not copied to custom asset packs to the Streaming Assets
            m_FilesAssignedToAssetPacks.Clear();
            AssignBundleFilesToAssetPacks("");
            AssignInstallTimeFilesToAssetPacks("");
            AddFilesToStreamingAssets(buildPlayerContext, "");
            if (!TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting)
            {
                return;
            }
            foreach (var tc in PlayerSettings.Android.textureCompressionFormats)
            {
                var postfix = TextureCompressionTargetingHelper.TcfPostfix(tc);
                AssignBundleFilesToAssetPacks(postfix);
                AssignInstallTimeFilesToAssetPacks(postfix);
                AddFilesToStreamingAssets(buildPlayerContext, postfix);
            }
        }
    }
}
