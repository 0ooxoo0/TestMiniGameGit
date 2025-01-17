#if UNITY_ANDROID
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Android;
using UnityEngine;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryCustomTemplatesTests : PlayAssetDeliveryAndroidBuildTestsBase
{
    static string InternalTemplateDirectory => Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "Tools", "GradleTemplates");

    [SetUp]
    public void CreatePluginsFolder()
    {
        Directory.CreateDirectory(PlayAssetDeliveryPostGenerateGradleAndroidProject.PluginsAndroidPath);
    }

    [TearDown]
    public void CleanupTemplates()
    {
        DeleteDirectoryFromAssets(Path.Combine("Assets", "Plugins"));
    }

    [Test]
    public void CanBuildAabOrGradleProjectWithCustomTemplates([Values(false, true)] bool exportProject)
    {
        var internalTemplate = Path.Combine(InternalTemplateDirectory, PlayAssetDeliveryPostGenerateGradleAndroidProject.kLauncherGradleTemplateName);
        var content = File.ReadAllText(internalTemplate);
        File.WriteAllText(PlayAssetDeliveryPostGenerateGradleAndroidProject.LauncherGradleTemplatePath, content);
        internalTemplate = Path.Combine(InternalTemplateDirectory, PlayAssetDeliveryPostGenerateGradleAndroidProject.kSettingsTemplateName);
        content = File.ReadAllText(internalTemplate);
        File.WriteAllText(PlayAssetDeliveryPostGenerateGradleAndroidProject.SettingsTemplatePath, content);
        AssetDatabase.Refresh();

        BuildPlayAssetDeliveryAndGradleProject(true, true, true, exportProject, kSingleFormat, kSingleFormatPostfix);
    }

    [Test]
    public void BuildGradleProjectWithCustomTemplatesWithoutAssetPacksFails()
    {
        var internalTemplate = Path.Combine(InternalTemplateDirectory, PlayAssetDeliveryPostGenerateGradleAndroidProject.kLauncherGradleTemplateName);
        var content = File.ReadAllText(internalTemplate);
        content = content.Replace("**PLAY_ASSET_PACKS**", "");
        File.WriteAllText(PlayAssetDeliveryPostGenerateGradleAndroidProject.LauncherGradleTemplatePath, content);
        AssetDatabase.Refresh();

        PlayerSettings.Android.textureCompressionFormats = kSingleFormat;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        PlayerSettings.Android.splitApplicationBinary = true;
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        var options = CreateSceneAndBuildPlayerOptions();
        options.locationPathName = kGradleProject;
        var report = BuildPipeline.BuildPlayer(options);
        LogAssert.Expect(LogType.Error, PlayAssetDeliveryPostGenerateGradleAndroidProject.kAssetPacksMissing);
    }
}
#endif
