using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Android;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
internal class PlayAssetDeliveryBuildWindowsTests : PlayAssetDeliveryBuildTestsBase
{
    const string kWindowsPlayerFolder = "WindowsTest";
    const string kWindowsPlayerExe = "test";
    const string kPlatformName = "StandaloneWindows64";

    [OneTimeSetUp]
    public void InitWindowsBuild()
    {
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
    }

    [TearDown]
    public void CleanupWindowsBuild()
    {
        if (Directory.Exists(kWindowsPlayerFolder))
        {
            Directory.Delete(kWindowsPlayerFolder, true);
        }
    }

    BuildPlayerOptions CreateSceneAndBuildPlayerOptions()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var scenePath = Path.Combine(kSingleTestAssetFolder, kTestScene);
        EditorSceneManager.SaveScene(scene, scenePath, true);
        var options = new BuildPlayerOptions();
        options.scenes = new[] { scenePath };
        options.targetGroup = BuildTargetGroup.Standalone;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;
        options.locationPathName = Path.Combine(kWindowsPlayerFolder, $"{kWindowsPlayerExe}.exe");
        return options;
    }

    void ValidatePlayer()
    {
        var addressablesPath = Path.Combine(kWindowsPlayerFolder, $"{kWindowsPlayerExe}_Data", "StreamingAssets", Addressables.StreamingAssetsSubFolder);
        Assert.IsTrue(
            File.Exists(Path.Combine(addressablesPath, "catalog.bin")) ||
            File.Exists(Path.Combine(addressablesPath, "catalog.json")) ||
            File.Exists(Path.Combine(addressablesPath, "catalog.bundle")),
            "Catalog file is missing");
        Assert.IsTrue(File.Exists(Path.Combine(addressablesPath, "settings.json")));
        Assert.IsTrue(File.Exists(Path.Combine(addressablesPath, "AddressablesLink", "link.xml")));
        ValidateGroupsInBuildFolder(Path.Combine(addressablesPath, kPlatformName));
    }

    [Test]
    public void CanBuildPlayAssetDeliveryAndWindowsPlayer([Values(true, false)] bool oneStep)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!oneStep)
        {
            var builder = GetBuilderOfType(settings, typeof(BuildScriptPlayAssetDelivery));
            var builderInput = new AddressablesDataBuilderInput(settings);
            var result = builder.BuildData<AddressableAssetBuildResult>(builderInput);
            LogAssert.Expect(LogType.Warning, $"Build target is not set to Android. No custom asset packs will be created.");
            Assert.IsTrue(string.IsNullOrEmpty(result.Error));
            ValidateBuildFolderWithoutPAD(kPlatformName);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        }
        else
        {
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        }
        var options = CreateSceneAndBuildPlayerOptions();
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Assert.AreEqual(BuildResult.Succeeded, summary.result);

        if (oneStep)
        {
            ValidateBuildFolderWithoutPAD("StandaloneWindows64");
        }
        ValidatePlayer();
    }
}
