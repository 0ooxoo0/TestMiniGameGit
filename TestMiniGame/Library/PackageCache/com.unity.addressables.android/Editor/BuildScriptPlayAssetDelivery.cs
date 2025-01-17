using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// In addition to the Default Build Script behavior (building AssetBundles), this script assigns Android bundled content to
    /// "install-time", fast-follow" or "on-demand" asset packs using data from <see cref="PlayAssetDeliverySchema"/>.
    /// All "install-time" content is being assigned to the 'AddressablesAssetPack' which is guaranteed to be "install-time".
    ///
    /// We will generate some files to create asset packs in Gradle project using <see cref="PlayAssetDeliveryModifyProjectScript"/> and
    /// to store build and runtime data. These files are located in 'Assets/PlayAssetDelivery/Build/aa*' folders:
    /// * Create a 'BuildProcessorData.json' file to store the build paths and Gradle project paths for bundles that should be copied to asset packs.
    /// When building Player this will be used by the <see cref="PlayAssetDeliveryModifyProjectScript"/> to copy bundles to their corresponding Gradle project paths.
    /// * Create a 'CustomAssetPacksData.json' file to store custom asset pack information to be used at runtime. See <see cref="PlayAssetDeliveryInitialization"/>.
    /// Also when building Player Addressables 'catalog.json' and 'settings.json' files are being relocated to the 'AddressablesAssetPack'.
    ///
    /// Any content of Addressables.BuildPath directory which is not assigned to asset packs will be included in the streaming assets pack.
    /// This happens with the groups which don't have <see cref="PlayAssetDeliverySchema"/> and Build path in their Content Packing &amp; Loading schema is set to Local,
    /// and also with the groups with delivery type set to "None" in <see cref="PlayAssetDeliverySchema"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPlayAssetDelivery.asset", menuName = "Addressables/Custom Build/Play Asset Delivery")]
    public class BuildScriptPlayAssetDelivery : BuildScriptPackedMode
    {
        /// <inheritdoc/>
        public override string Name
        {
            get { return "Play Asset Delivery"; }
        }

        Dictionary<string, string> m_AssetPacksNames = new Dictionary<string, string>();

        void AddResult<TResult>(ref TResult combined, TResult result) where TResult : IDataBuilderResult
        {
            combined.Duration += result.Duration;
            combined.LocationCount += result.LocationCount;
            combined.OutputPath = result.OutputPath;
            if (!string.IsNullOrEmpty(result.Error))
            {
                if (string.IsNullOrEmpty(combined.Error))
                {
                    combined.Error = result.Error;
                }
                else
                {
                    combined.Error += $"\n{result.Error}";
                }
            }
            if (result.FileRegistry != null)
            {
                if (combined.FileRegistry == null)
                {
                    combined.FileRegistry = combined.FileRegistry;
                }
                else
                {
                    foreach (var f in result.FileRegistry.GetFilePaths())
                    {
                        combined.FileRegistry.AddFile(f);
                    }
                }
            }
        }

        Dictionary<AddressableAssetGroup, string> m_BuildPathRestore = new Dictionary<AddressableAssetGroup, string>();
        Dictionary<AddressableAssetGroup, string> m_LoadPathRestore = new Dictionary<AddressableAssetGroup, string>();
        Dictionary<AddressableAssetGroup, Type> m_AssetBundleProviderRestore = new Dictionary<AddressableAssetGroup, Type>();
        AddressableAssetGroup m_DefaultGroup;
        bool m_BuildRemoteCatalog;
        bool m_CompressTexturesOnImport;
        CustomAssetPackSettings m_CustomAssetPackSettings;

        string GetProfilePathNameWithFallback(AddressableAssetSettings settings, string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (settings.profileSettings.GetProfileDataByName(name) != null)
                {
                    return name;
                }
            }
            return null;
        }

        void PrepareGroupsForPlayAssetDelivery(AddressableAssetSettings settings)
        {
            // trying "LocalBuildPath" and "LocalLoadPath" is required for compatibility with previous Addressables versions (1.17.x or before)
            var localBuildPath = GetProfilePathNameWithFallback(settings, new[] { AddressableAssetSettings.kLocalBuildPath, "LocalBuildPath" });
            var localLoadPath = GetProfilePathNameWithFallback(settings, new[] { AddressableAssetSettings.kLocalLoadPath, "LocalLoadPath" });
            foreach (var group in settings.groups)
            {
                if (group.HasSchema<PlayAssetDeliverySchema>() && group.HasSchema<BundledAssetGroupSchema>())
                {
                    var schema = group.GetSchema<BundledAssetGroupSchema>();
                    // force Local values for Build and Load paths for Play Asset Delivery
                    if (localBuildPath != null && schema.BuildPath.GetName(group.Settings) != localBuildPath)
                    {
                        m_BuildPathRestore[group] = schema.BuildPath.Id;
                        schema.BuildPath.SetVariableByName(group.Settings, localBuildPath);
                    }
                    if (localLoadPath != null && schema.LoadPath.GetName(group.Settings) != localLoadPath)
                    {
                        m_LoadPathRestore[group] = schema.LoadPath.Id;
                        schema.LoadPath.SetVariableByName(group.Settings, localLoadPath);
                    }
                    // force PlayAssetDeliveryAssetBundleProvider as AssetBundleProviderType
                    if (schema.AssetBundleProviderType.Value != typeof(PlayAssetDeliveryAssetBundleProvider))
                    {
                        m_AssetBundleProviderRestore[group] = schema.AssetBundleProviderType.Value;
                        schema.AssetBundleProviderType = new SerializedType() { Value = typeof(PlayAssetDeliveryAssetBundleProvider) };
                    }
                }
            }
            // Default group must use local build path, otherwise shared bundles will be generated using remote path.
            // If we can't find such group to make it default, this means that no groups are targeted for PlayAssetDelivery and
            // all groups use remote build path. In this case shared bundles will be generated using remote build path along with all other bundles.
            m_DefaultGroup = settings.DefaultGroup;
            var defaultGroupSchema = m_DefaultGroup.GetSchema<BundledAssetGroupSchema>();
            if (defaultGroupSchema?.BuildPath.GetName(m_DefaultGroup.Settings) != AddressableAssetSettings.kLocalBuildPath)
            {
                var index = settings.groups.FindIndex(g => g.GetSchema<BundledAssetGroupSchema>()?.BuildPath.GetName(g.Settings) == AddressableAssetSettings.kLocalBuildPath);
                if (index != -1)
                {
                    settings.DefaultGroup = settings.groups[index];
                }
            }

            m_BuildRemoteCatalog = settings.BuildRemoteCatalog;
            var defaultGroup = settings.DefaultGroup;
            if (defaultGroup.GetSchema<BundledAssetGroupSchema>()?.BuildPath.GetName(defaultGroup.Settings) == AddressableAssetSettings.kLocalBuildPath)
            {
                // Disabling building remote catalog for Play Asset Delivery
                settings.BuildRemoteCatalog = false;
            }

            // Force enable CompressTexturesOnImport
            m_CompressTexturesOnImport = EditorUserSettings.compressAssetsOnImport;
            EditorUserSettings.compressAssetsOnImport = true;
        }

        void RestoreGroups(AddressableAssetSettings settings)
        {
            // restore original Build/Load paths and BundledAssetProviderType
            foreach (var bp in m_BuildPathRestore)
            {
                var schema = bp.Key.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableById(bp.Key.Settings, bp.Value);
            }
            m_BuildPathRestore.Clear();
            foreach (var lp in m_LoadPathRestore)
            {
                var schema = lp.Key.GetSchema<BundledAssetGroupSchema>();
                schema.LoadPath.SetVariableById(lp.Key.Settings, lp.Value);
            }
            m_LoadPathRestore.Clear();
            foreach (var bap in m_AssetBundleProviderRestore)
            {
                var schema = bap.Key.GetSchema<BundledAssetGroupSchema>();
                schema.AssetBundleProviderType = new SerializedType() { Value = bap.Value };
            }
            m_AssetBundleProviderRestore.Clear();
            settings.DefaultGroup = m_DefaultGroup;
            settings.BuildRemoteCatalog = m_BuildRemoteCatalog;

            EditorUserSettings.compressAssetsOnImport = m_CompressTexturesOnImport;
        }

        /// <summary>
        /// Android specific implementation of <see cref="BuildData{TResult}"/>.
        /// When building for Android, actual builds happen for all target texture compressions.
        /// </summary>
        /// <param name="builderInput">The builderInput object used in the build</param>
        /// <typeparam name="TResult">The type of data to build</typeparam>
        /// <returns>The build data result</returns>
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            ClearPlayAssetDeliveryContent();

            // Don't prepare content for Play Asset Delivery if the build target isn't set to Android
            if (builderInput.Target != BuildTarget.Android)
            {
                Addressables.LogWarning("Build target is not set to Android. No custom asset packs will be created.");
                return base.BuildDataImplementation<TResult>(builderInput);
            }
            // Don't prepare content for Play Asset Delivery if the PAD support isn't initialized
            if (PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized())
            {
                Addressables.LogWarning($"Addressables are not initialized to use with Play Asset Delivery. Open '{PlayAssetDeliverySetup.kInitPlayAssetDeliveryMenuItem}'.");
                return base.BuildDataImplementation<TResult>(builderInput);
            }

            TResult result = AddressableAssetBuildResult.CreateResult<TResult>("", 0);
            m_AssetPacksNames.Clear();
            m_CustomAssetPackSettings = CustomAssetPackSettings.GetSettings(true);
            m_CustomAssetPackSettings.ResetGeneratingUniqueAssetPacksNames();

            PrepareGroupsForPlayAssetDelivery(builderInput.AddressableSettings);
            if (TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting)
            {
                CreateBuildOutputFolders(PlayerSettings.Android.textureCompressionFormats);
                var storedTextureCompressionFormat = EditorUserBuildSettings.androidBuildSubtarget;
                try
                {
                    foreach (var format in PlayerSettings.Android.textureCompressionFormats.AsEnumerable().Reverse())
                    {
                        EditorUserBuildSettings.androidBuildSubtarget = TextureCompressionTargetingHelper.ConvertToMobileTextureSubtarget(format);
                        AssetDatabase.Refresh();
                        AddResult(ref result, base.BuildDataImplementation<TResult>(builderInput));
                    }
                }
                finally
                {
                    EditorUserBuildSettings.androidBuildSubtarget = storedTextureCompressionFormat;
                }
            }
            else
            {
                CreateBuildOutputFolders();
                result = base.BuildDataImplementation<TResult>(builderInput);
            }
            RestoreGroups(builderInput.AddressableSettings);

            if (!TextureCompressionTargetingHelper.UseAssetPacks)
            {
                Addressables.LogWarning("Addressable content built, but Play Asset Delivery will be used only when building App Bundle with Split Application Binary option checked (or when using Texture Compression Targeting).");
            }

            return result;
        }

        /// <summary>
        /// The method that actually builds Addressables for the specific texture compression and creates json files required to prepare Android Gradle project.
        /// </summary>
        /// <param name="builderInput">The builderInput object used in the build</param>
        /// <param name="aaContext">Context object for passing data through SBP, between different sections of Addressables code</param>
        /// <typeparam name="TResult">The type of data to build</typeparam>
        /// <returns>The build data result for the specific texture compression</returns>
        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            // Build AssetBundles
            TResult result = base.DoBuild<TResult>(builderInput, aaContext);
            if (builderInput.Target != BuildTarget.Android || PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized())
            {
                return result;
            }

            // Create custom asset packs
            CreateAssetPacks(aaContext.Settings);
            return result;
        }

        /// <inheritdoc/>
        public override void ClearCachedData()
        {
            base.ClearCachedData();
            ClearPlayAssetDeliveryContent();
        }

        static void ClearPlayAssetDeliveryContent()
        {
            try
            {
                ClearJsonFiles();
                ClearTextureCompressionTargetedContent();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal static void ClearTextureCompressionTargetedContent()
        {
            var buildRootDir = Path.GetDirectoryName(Addressables.BuildPath);
            if (Directory.Exists(buildRootDir))
            {
                // Delete #tcf specific folders in Addressables build path
                var tcfBuildDirectories = Directory.EnumerateDirectories(buildRootDir, "Android#tcf_*").ToList();
                foreach (var dir in tcfBuildDirectories)
                {
                    Directory.Delete(dir, true);
                }
                // Delete folder for the default texture compression
                var defaultBuildDir = Path.Combine(buildRootDir, "Android");
                if (Directory.Exists(defaultBuildDir))
                {
                    Directory.Delete(defaultBuildDir, true);
                }
            }
        }

        static void ClearJsonFiles()
        {
            if (!Directory.Exists(CustomAssetPackUtility.BuildRootDirectory))
            {
                return;
            }
            var dirs = Directory.EnumerateDirectories(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}*");
            foreach (var dir in dirs)
            {
                // Delete "BuildProcessorData.json"
                var file = Path.Combine(dir, CustomAssetPackUtility.kBuildProcessorDataFilename);
                AssetDatabase.DeleteAsset(file);
                // Delete "CustomAssetPacksData.json"
                file = Path.Combine(dir, CustomAssetPackUtility.kCustomAssetPackDataFilename);
                AssetDatabase.DeleteAsset(file);
                AssetDatabase.DeleteAsset(dir);
            }
        }

        void CreateAssetPacks(AddressableAssetSettings settings)
        {
            var assetPackToDataEntry = new Dictionary<string, CustomAssetPackDataEntry>();
            // ensure that entry for AddressablesAssetPack is added even if there are no install-time groups,
            // this asset pack is required for json files and non-groups specific asset bundles
            assetPackToDataEntry[CustomAssetPackUtility.kAddressablesAssetPackName] = new CustomAssetPackDataEntry(CustomAssetPackUtility.kAddressablesAssetPackName, DeliveryType.InstallTime, new List<string>());

            var bundleIdToEditorDataEntry = new Dictionary<string, BuildProcessorDataEntry>();
            var bundleIdToEditorDataEntryDefault = new Dictionary<string, BuildProcessorDataEntry>();

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (!HasRequiredSchemas(settings, group))
                {
                    continue;
                }
                var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
                DeliveryType deliveryType = DeliveryType.None;
                var assetPackName = "";
                var customAssetPacksCount = m_CustomAssetPackSettings != null ? m_CustomAssetPackSettings.CustomAssetPacks.Count : 0;
                var includeInCustomAssetPack = false;
                if (assetPackSchema.IncludeInCustomAssetPack && customAssetPacksCount > 0)
                {
                    var index = m_CustomAssetPackSettings.CustomAssetPacks.FindIndex(pack => pack.AssetPackName == assetPackSchema.CustomAssetPackName);
                    if (index != -1)
                    {
                        var assetPack = m_CustomAssetPackSettings.CustomAssetPacks[index];
                        deliveryType = assetPack.DeliveryType;
                        assetPackName = assetPack.AssetPackName;
                        includeInCustomAssetPack = true;
                    }
                    else if (!string.IsNullOrEmpty(assetPackSchema.CustomAssetPackName))
                    {
                        Addressables.LogWarning($"Group '{group.name}' supposed to be included to the '{assetPackSchema.CustomAssetPackName}' custom asset pack which doesn't exist. Separate asset pack for this group will be created.");
                    }
                }
                if (!includeInCustomAssetPack)
                {
                    deliveryType = assetPackSchema.AssetPackDeliveryType;
                    if (!m_AssetPacksNames.TryGetValue(group.Name, out assetPackName))
                    {
                        assetPackName = Regex.Replace(group.Name, "[^A-Za-z0-9_]", "");
                        if (assetPackName.Length == 0 || !char.IsLetter(assetPackName[0]))
                        {
                            assetPackName = "Group" + assetPackName;
                        }
                        assetPackName = m_CustomAssetPackSettings.GenerateUniqueAssetPackName(assetPackName);
                        m_AssetPacksNames[group.Name] = assetPackName;
                    }
                }
                // install-time addressable groups are all packed to AddressablesAssetPack
                if (deliveryType == DeliveryType.InstallTime)
                {
                    assetPackName = CustomAssetPackUtility.kAddressablesAssetPackName;
                }

                if (IsAssignedToAssetPack(settings, group, deliveryType))
                {
                    CreateConfigFiles(group, assetPackName, deliveryType, assetPackToDataEntry, bundleIdToEditorDataEntry, bundleIdToEditorDataEntryDefault);
                }
            }

            var postfix = TextureCompressionTargetingHelper.TcfPostfix();

            // Create the BuildProcessorData.json, it contains information for relocating custom asset pack bundles when building a player
            Serialize(new BuildProcessorData(bundleIdToEditorDataEntry.Values), postfix, CustomAssetPackUtility.kBuildProcessorDataFilename);
            // Create the CustomAssetPacksData.json file, it contains all custom asset pack information that can be used at runtime
            var customAssetPackData = new CustomAssetPackData(assetPackToDataEntry.Values);
            Serialize(customAssetPackData, postfix, CustomAssetPackUtility.kCustomAssetPackDataFilename);

            if (TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault)
            {
                // Create the BuildProcessorData.json file for the default variant
                Serialize(new BuildProcessorData(bundleIdToEditorDataEntryDefault.Values), "", CustomAssetPackUtility.kBuildProcessorDataFilename);
                Serialize(customAssetPackData, "", CustomAssetPackUtility.kCustomAssetPackDataFilename);
            }

            var addressablesLink = Path.Combine(Addressables.BuildPath, "AddressablesLink");
            if (!TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting || TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault)
            {
                // Move link.xml to 'Assets/PlayAssetDeliveryBuild/AddressableLink' folder
                var addressablesBuildLink = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, "AddressablesLink");
                if (Directory.Exists(addressablesBuildLink))
                {
                    Directory.Delete(addressablesBuildLink, true);
                }
                Directory.Move(addressablesLink, addressablesBuildLink);
            }
            else
            {
                // Only one link.xml file is required as it would be the same for all texture compression variants
                Directory.Delete(addressablesLink, true);

                // Moving generated files to the texture compression specific directory
                Directory.Move(Addressables.BuildPath, $"{Addressables.BuildPath}{postfix}");
            }
        }

        void CreateBuildOutputFolder(string postfix)
        {
            var folderWithPostfix = $"{Addressables.StreamingAssetsSubFolder}{postfix}";
            if (!AssetDatabase.IsValidFolder(Path.Combine(CustomAssetPackUtility.BuildRootDirectory, folderWithPostfix)))
            {
                AssetDatabase.CreateFolder(CustomAssetPackUtility.BuildRootDirectory, folderWithPostfix);
            }
        }

        void CreateBuildOutputFolders(TextureCompressionFormat[] textureCompressions = null)
        {
            // Create the 'Assets/PlayAssetDeliveryBuild' directory
            if (!AssetDatabase.IsValidFolder(CustomAssetPackUtility.BuildRootDirectory))
            {
                AssetDatabase.CreateFolder(CustomAssetPackUtility.RootDirectory, CustomAssetPackUtility.kBuildFolderName);
            }
            else
            {
                ClearJsonFiles();
            }

            CreateBuildOutputFolder("");
            if (textureCompressions == null)
            {
                return;
            }
            foreach (var textureCompression in textureCompressions)
            {
                CreateBuildOutputFolder(TextureCompressionTargetingHelper.TcfPostfix(textureCompression));
            }
        }

        bool BuildPathIncludedInStreamingAssets(string buildPath)
        {
            return buildPath.StartsWith(Addressables.BuildPath) || buildPath.StartsWith(Application.streamingAssetsPath);
        }

        bool HasRequiredSchemas(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var hasBundledSchema = group.HasSchema<BundledAssetGroupSchema>();
            var hasPADSchema = group.HasSchema<PlayAssetDeliverySchema>();

            if (!hasBundledSchema && !hasPADSchema)
            {
                return false;
            }
            if (!hasBundledSchema && hasPADSchema)
            {
                Addressables.LogWarning($"Group '{group.name}' has a '{typeof(PlayAssetDeliverySchema).Name}' but not a '{typeof(BundledAssetGroupSchema).Name}'. " +
                    "It does not contain any bundled content to be assigned to an asset pack.");
                return false;
            }
            if (hasBundledSchema && !hasPADSchema)
            {
                var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                var buildPath = bundledSchema.BuildPath.GetValue(settings);
                if (BuildPathIncludedInStreamingAssets(buildPath))
                {
                    Addressables.Log($"Group '{group.name}' does not have a '{typeof(PlayAssetDeliverySchema).Name}' but its build path '{buildPath}' will be included in StreamingAssets at build time. " +
                        "The group will be assigned to the streaming asset pack unless its build path is changed.");
                }
                return false;
            }
            return true;
        }

        bool IsAssignedToAssetPack(AddressableAssetSettings settings, AddressableAssetGroup group, DeliveryType deliveryType)
        {
            if (deliveryType != DeliveryType.None)
            {
                return true;
            }
            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            var buildPath = bundledSchema.BuildPath.GetValue(settings);
            if (BuildPathIncludedInStreamingAssets(buildPath))
            {
                Addressables.LogWarning($"Group '{group.name}' has Delivery Type set to 'None' or it is included to custom asset pack which Delivery Type set to 'None'. " +
                    "The group will be assigned to the streaming assets pack.");
            }
            return false;
        }

        void CreateConfigFiles(AddressableAssetGroup group, string assetPackName, DeliveryType deliveryType, Dictionary<string, CustomAssetPackDataEntry> assetPackToDataEntry, Dictionary<string, BuildProcessorDataEntry> bundleIdToEditorDataEntry, Dictionary<string, BuildProcessorDataEntry> bundleIdToEditorDataEntryDefault)
        {
            foreach (var entry in group.entries)
            {
                if (entry.BundleFileId == null || bundleIdToEditorDataEntry.ContainsKey(entry.BundleFileId))
                {
                    continue;
                }

                var bundleBuildPath = AddressablesRuntimeProperties.EvaluateString(entry.BundleFileId).Replace("\\", "/");
                var bundleFileName = Path.GetFileName(bundleBuildPath);
                var bundleName = Path.GetFileNameWithoutExtension(bundleBuildPath);
                var postfix = TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault ? "" : TextureCompressionTargetingHelper.TcfPostfix();
                var relativePath = Path.GetRelativePath(Addressables.BuildPath, bundleBuildPath);
                bundleBuildPath = Path.Combine($"{Addressables.BuildPath}{postfix}", relativePath);

                if (!assetPackToDataEntry.ContainsKey(assetPackName))
                {
                    assetPackToDataEntry[assetPackName] = new CustomAssetPackDataEntry(assetPackName, deliveryType, new List<string>() { bundleName });
                }
                else
                {
                    // Otherwise just save the bundle to asset pack data
                    assetPackToDataEntry[assetPackName].AssetBundles.Add(bundleName);
                }

                // Store the bundle's build path and its corresponding asset pack location inside gradle project
                var assetsFolderPath = Path.Combine(assetPackName, $"{CustomAssetPackUtility.CustomAssetPacksAssetsPath}{TextureCompressionTargetingHelper.TcfPostfix()}", relativePath);
                bundleIdToEditorDataEntry.Add(entry.BundleFileId, new BuildProcessorDataEntry(bundleBuildPath, assetsFolderPath));
                if (TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault)
                {
                    assetsFolderPath = Path.Combine(assetPackName, CustomAssetPackUtility.CustomAssetPacksAssetsPath, relativePath);
                    bundleIdToEditorDataEntryDefault.Add(entry.BundleFileId, new BuildProcessorDataEntry(bundleBuildPath, assetsFolderPath));
                }
            }
        }

        void Serialize<T>(T data, string postfix, string fileName)
        {
            var contents = JsonUtility.ToJson(data);
            var jsonPath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", fileName);
            File.WriteAllText(jsonPath, contents);
        }
    }
}
