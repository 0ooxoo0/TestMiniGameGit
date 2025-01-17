#if UNITY_ANDROID
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// When building for Android with asset packs support copies generated addressables asset bundles to asset packs inside gradle project.
    /// </summary>
    public class PlayAssetDeliveryModifyProjectScript : AndroidProjectFilesModifier
    {
        void AddBundlesFilesToContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var buildProcessorDataPath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", CustomAssetPackUtility.kBuildProcessorDataFilename);
            if (!File.Exists(buildProcessorDataPath))
            {
                return;
            }
            var contents = File.ReadAllText(buildProcessorDataPath);
            var data = JsonUtility.FromJson<BuildProcessorData>(contents);
            foreach (BuildProcessorDataEntry entry in data.Entries)
            {
                projectFilesContext.AddFileToCopy(entry.BundleBuildPath, entry.AssetPackPath);
            }
        }

        static string CreateAddressableAssetPackAssetsPath(string postfix)
        {
            return Path.Combine(CustomAssetPackUtility.kAddressablesAssetPackName, $"{CustomAssetPackUtility.CustomAssetPacksAssetsPath}{postfix}");
        }

        static void AddInstallTimeFilesToContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var targetPath = CreateAddressableAssetPackAssetsPath(postfix);
            var sourcePath = $"{Addressables.BuildPath}{postfix}";
            if (!Directory.Exists(sourcePath))
            {
                // using default texture compression variant
                sourcePath = Addressables.BuildPath;
            }
            foreach (var mask in CustomAssetPackUtility.InstallTimeFilesMasks)
            {
                var files = Directory.EnumerateFiles(sourcePath, mask, SearchOption.AllDirectories).ToList();
                foreach (var f in files)
                {
                    var dest = Path.Combine(targetPath, Path.GetRelativePath(sourcePath, f));
                    projectFilesContext.AddFileToCopy(f, dest);
                }
            }
        }

        static void AddCustomAssetPackDataContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var targetPath = Path.Combine(CreateAddressableAssetPackAssetsPath(postfix), CustomAssetPackUtility.kCustomAssetPackDataFilename);
            var sourcePath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", CustomAssetPackUtility.kCustomAssetPackDataFilename);
            projectFilesContext.AddFileToCopy(sourcePath, targetPath);
        }

        /// <summary>
        /// Setup copy operations for addressables asset bundles to the asset packs inside gradle project.
        /// Stores information required for new build.gradle files and for modifying existing gradle files.
        /// </summary>
        public override AndroidProjectFilesModifierContext Setup()
        {
            var projectFilesContext = new AndroidProjectFilesModifierContext();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
                !TextureCompressionTargetingHelper.UseAssetPacks ||
                PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized() ||
                !File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath) ||
                (TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting && !TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault))
            {
                // gradle project must be modified only when using asset packs, play asset delivery is supported and addressables are generated for PAD,
                // this should be done during the last (or the only) texture compression related iteration
                projectFilesContext.SetData("UseAssetPacks", false);
                PlayAssetDeliveryPostGenerateGradleAndroidProject.Reset();
                return projectFilesContext;
            }

            var contents = File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataEditorPath);
            var customPackData = JsonUtility.FromJson<CustomAssetPackData>(contents);
            foreach (CustomAssetPackDataEntry entry in customPackData.Entries)
            {
                projectFilesContext.Outputs.AddBuildGradleFile(Path.Combine(entry.AssetPackName, "build.gradle"));
            }
            projectFilesContext.SetData("AssetPacks", customPackData);
            projectFilesContext.SetData("UseAssetPacks", true);

            AddCustomAssetPackDataContext(projectFilesContext, "");
            AddBundlesFilesToContext(projectFilesContext, "");
            AddInstallTimeFilesToContext(projectFilesContext, "");
            if (TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting)
            {
                foreach (var textureCompression in PlayerSettings.Android.textureCompressionFormats)
                {
                    var postfix = TextureCompressionTargetingHelper.TcfPostfix(textureCompression);
                    AddCustomAssetPackDataContext(projectFilesContext, postfix);
                    AddBundlesFilesToContext(projectFilesContext, postfix);
                    AddInstallTimeFilesToContext(projectFilesContext, postfix);
                }
            }

            projectFilesContext.Dependencies.DependencyFiles = new[]
            {
                CustomAssetPackUtility.CustomAssetPacksDataEditorPath
            };

            PlayAssetDeliveryPostGenerateGradleAndroidProject.Setup(customPackData);

            return projectFilesContext;
        }

        internal static string GenerateAssetPacksGradleContents(string assetPackString, CustomAssetPackData customPackData)
        {
            var assetPacks = new StringBuilder();
            foreach (var entry in customPackData.Entries)
            {
                var assetPackEntry = $"\":{entry.AssetPackName}\"";
                if (assetPackString.IndexOf(assetPackEntry, StringComparison.InvariantCulture) == -1)
                {
                    assetPacks.Append(", ").Append(assetPackEntry);
                }
            }
            // if assetPackString contains no asset packs, leading comma must be skipped
            var start = assetPackString.Contains(':', StringComparison.InvariantCulture) || assetPacks.Length == 0 ? 0 : 2;
            return assetPackString += assetPacks.ToString(start, assetPacks.Length - start);
        }

        /// <summary>
        /// Create build.gradle files for the new asset packs. Adds required dependencies to the existing gradle files.
        /// </summary>
        /// <param name="projectFiles">An object representing gradle project files</param>
        public override void OnModifyAndroidProjectFiles(AndroidProjectFiles projectFiles)
        {
            if (!projectFiles.GetData<bool>("UseAssetPacks"))
            {
                return;
            }

            var customPackData = projectFiles.GetData<CustomAssetPackData>("AssetPacks");

            foreach (var entry in customPackData.Entries)
            {
                var buildGradle = new ModuleBuildGradleFile();
                buildGradle.ApplyPluginList.AddPluginByName("com.android.asset-pack");
                buildGradle.AddElement(new Block("assetPack", $"{{\n\tpackName = \"{entry.AssetPackName}\"\n\tdynamicDelivery {{\n\t\tdeliveryType = \"{CustomAssetPackUtility.DeliveryTypeToGradleString(entry.DeliveryType)}\"\n\t}}\n}}"));
                projectFiles.SetBuildGradleFile(Path.Combine(entry.AssetPackName, "build.gradle"), buildGradle);
            }

            if (!PlayAssetDeliveryPostGenerateGradleAndroidProject.UpdateSettingsGradle)
            {
                foreach (var entry in customPackData.Entries)
                {
                    projectFiles.GradleSettings.IncludeList.AddPluginByName($":{entry.AssetPackName}");
                }
            }

            if (!PlayAssetDeliveryPostGenerateGradleAndroidProject.UpdateLauncherBuildGradle)
            {
                var assetPackString = projectFiles.LauncherBuildGradle.Android.AssetPacks.GetRaw() ?? "";
                assetPackString = GenerateAssetPacksGradleContents(assetPackString, customPackData);
                projectFiles.LauncherBuildGradle.Android.AssetPacks.SetRaw(assetPackString);
            }
        }
    }

    internal class PlayAssetDeliveryPostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject
    {
        internal const string kSettingsTemplateName = "settingsTemplate.gradle";
        internal const string kLauncherGradleTemplateName = "launcherTemplate.gradle";
        internal static string PluginsAndroidPath => Path.Combine("Assets", "Plugins", "Android");
        internal static string SettingsTemplatePath => Path.Combine(PluginsAndroidPath, kSettingsTemplateName);
        internal static string LauncherGradleTemplatePath => Path.Combine(PluginsAndroidPath, kLauncherGradleTemplateName);

        internal static bool UpdateLauncherBuildGradle { get; private set; } = false;
        internal static bool UpdateSettingsGradle { get; private set; } = false;
        static CustomAssetPackData CustomPackData { get; set; } = null;

        internal const string kAssetPacksMissing = "Asset packs entry (**PLAY_ASSET_PACKS**) is missing from Custom Launcher Gradle Template. Asset packs are not included to the AAB.";
        string GradleFileMissingMsg(string path) => $"File {path} is missing from the generated project. Asset packs are not included to the AAB.";

        static readonly Regex s_AssetPacksContent = new Regex(@"(?<begin>^.*assetPacks\s*=\s*\[\s*)(?<assetPacks>.*)(?<end>\s*\].*$)", RegexOptions.Compiled | RegexOptions.Singleline);

        public int callbackOrder => 0;

        internal static void Reset()
        {
            CustomPackData = null;
            UpdateLauncherBuildGradle = false;
            UpdateSettingsGradle = false;
        }

        internal static void Setup(CustomAssetPackData customPackData)
        {
            CustomPackData = customPackData;
            UpdateLauncherBuildGradle = File.Exists(LauncherGradleTemplatePath);
            UpdateSettingsGradle = File.Exists(SettingsTemplatePath);
        }

        public void OnPostGenerateGradleAndroidProject(string projectPath)
        {
            if (CustomPackData == null)
            {
                return;
            }

            if (UpdateSettingsGradle)
            {
                var settingsGradlePath = Path.Combine(projectPath, "..", "settings.gradle");
                if (!File.Exists(settingsGradlePath))
                {
                    // throwing exception from here doesn't break build process, so just adding error to the Editor log
                    Debug.LogError(GradleFileMissingMsg(settingsGradlePath));
                }
                else
                {
                    var settingsContent = File.ReadAllText(settingsGradlePath);
                    var includeAssetPacks = new StringBuilder();
                    foreach (var entry in CustomPackData.Entries)
                    {
                        var assetPackEntry = $"\':{entry.AssetPackName}\'";
                        if (!settingsContent.Contains(assetPackEntry, StringComparison.InvariantCulture))
                        {
                            includeAssetPacks.Append("\ninclude ").Append(assetPackEntry);
                        }
                    }
                    if (includeAssetPacks.Length > 0)
                    {
                        settingsContent += includeAssetPacks.ToString();
                        File.WriteAllText(settingsGradlePath, settingsContent);
                    }
                }
            }

            if (UpdateLauncherBuildGradle)
            {
                var launcherGradlePath = Path.Combine(projectPath, "..", "launcher", "build.gradle");
                if (!File.Exists(launcherGradlePath))
                {
                    // throwing exception from here doesn't break build process, so just adding error to the Editor log
                    Debug.LogError(GradleFileMissingMsg(launcherGradlePath));
                    return;
                }
                var launcherBuildContent = File.ReadAllText(launcherGradlePath);
                var match = s_AssetPacksContent.Match(launcherBuildContent);
                if (!match.Success)
                {
                    // throwing exception from here doesn't break build process, so just adding error to the Editor log
                    Debug.LogError(kAssetPacksMissing);
                    return;
                }
                var assetPackString = match.Groups["assetPacks"].Value;
                assetPackString = PlayAssetDeliveryModifyProjectScript.GenerateAssetPacksGradleContents(assetPackString, CustomPackData);
                launcherBuildContent = match.Groups["begin"].Value + assetPackString + match.Groups["end"].Value;
                File.WriteAllText(launcherGradlePath, launcherBuildContent);
            }
        }
    }
}
#endif
