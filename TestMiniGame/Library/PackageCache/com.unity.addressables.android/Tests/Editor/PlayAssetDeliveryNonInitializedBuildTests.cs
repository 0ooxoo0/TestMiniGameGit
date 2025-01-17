using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Android;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryNonInitializedBuildTests : PlayAssetDeliveryAndroidBuildTestsBase
{
    protected void BuildAddressablesAndGradleProject(bool oneStep, bool buildAppBundle, bool splitAppBinary, bool exportProject)
    {
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
        PlayerSettings.Android.splitApplicationBinary = buildAppBundle ? splitAppBinary : false; // when building APK with split binary on, all bundles are moved inside obb file, nothing to check for us in this case
        PlayerSettings.Android.textureCompressionFormats = kSingleFormat;
        var useAssetPacks = buildAppBundle && splitAppBinary;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        // removing PlayAssetDeliveryInitializationSettings to ensure that BuildScriptPlayAssetDelivery handles this case
        var index = settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings);
        settings.RemoveInitializationObject(index);

        if (!oneStep)
        {
            var builder = GetBuilderOfType(settings, typeof(BuildScriptPlayAssetDelivery));
            var builderInput = new AddressablesDataBuilderInput(settings);

            var result = builder.BuildData<AddressableAssetBuildResult>(builderInput);
            LogAssert.Expect(LogType.Warning, $"Addressables are not initialized to use with Play Asset Delivery. Open '{PlayAssetDeliverySetup.kInitPlayAssetDeliveryMenuItem}'.");

            Assert.IsTrue(string.IsNullOrEmpty(result.Error));

            ValidateBuildFolderWithoutPAD("Android");
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        }
        else
        {
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            // removing BuildScriptPlayAssetDelivery to ensure that this case is handled while buiding Addressables with Player
            index = settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery);
            settings.RemoveDataBuilder(index);
            settings.RemoveInitializationObject(index);
        }

        var options = CreateSceneAndBuildPlayerOptions();
        options.locationPathName = $"{kGradleProject}{(exportProject ? "" : (buildAppBundle ? ".aab" : ".apk"))}";
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Assert.AreEqual(BuildResult.Succeeded, summary.result);

        if (useAssetPacks)
        {
            LogAssert.Expect(LogType.Warning, $"Addressables are not initialized to use with Play Asset Delivery. Open '{PlayAssetDeliverySetup.kInitPlayAssetDeliveryMenuItem}'.");
        }
        if (oneStep)
        {
            ValidateBuildFolderWithoutPAD("Android");
        }
        if (!exportProject)
        {
            return;
        }
        ValidateGradleProjectWithoutAssetPacks(kGradleProject, useAssetPacks);
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenApkOrGradleProject([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildAddressablesAndGradleProject(oneStep, false, false, exportProject);
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenAabOrGradleProject([Values(false, true)] bool oneStep, [Values(false, true)] bool splitAppBinary, [Values(false, true)] bool exportProject)
    {
        BuildAddressablesAndGradleProject(oneStep, true, splitAppBinary, exportProject);
    }
}
