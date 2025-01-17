using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryAndroidBuildTests : PlayAssetDeliveryAndroidBuildTestsBase
{
    [Test]
    public void CanBuildPlayAssetDeliveryThenApkOrGradleProject([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, false, false, exportProject, kSingleFormat, kSingleFormatPostfix);
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenApkOrGradleProjectWithTCFT([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, false, false, exportProject, kMultiFormats, kMultiFormatPostfixes);
    }

    [Test]
    [Timeout(300000)]
    public void CanBuildPlayAssetDeliveryThenAabOrGradleProject([Values(false, true)] bool oneStep, [Values(false, true)] bool splitAppBinary, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, true, splitAppBinary, exportProject, kSingleFormat, kSingleFormatPostfix);
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenAabOrGradleProjectWithTCFT([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, true, true, exportProject, kMultiFormats, kMultiFormatPostfixes);
    }

    [Test]
    public void CanBuildWithoutPlayAssetDeliveryWithTCFT([Values(true, false)] bool buildForRemote)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.CreateGroup(kRemoteGroupName, true, false, false, null, typeof(BundledAssetGroupSchema));
        var spriteEntry = settings.CreateOrMoveEntry(CreateTexture(Path.Combine(kSingleTestAssetFolder, TextureName(100))), group, false, false);
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(group.Settings, buildForRemote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(group.Settings, buildForRemote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
        // testing variant when default group is not PAD, and build path is set to remote
        settings.DefaultGroup = group;

        BuildPlayAssetDeliveryAndGradleProject(false, true, true, true, kMultiFormats, kMultiFormatPostfixes);

        // default group must be restored after build
        Assert.AreEqual(settings.DefaultGroup, group);
        if (buildForRemote)
        {
            var bundleFiles = Directory.GetFiles(Path.Combine("ServerData", "Android"), $"{kRemoteGroupName}*.bundle");
            Assert.AreEqual(kMultiFormats.Length, bundleFiles.Length);
        }
        else
        {
            foreach (var postfix in kMultiFormatPostfixes)
            {
                if (postfix != kMultiFormatPostfixes[1])
                {
                    var bundleFiles = Directory.GetFiles(Path.Combine($"{Addressables.BuildPath}{postfix}", "Android"));
                    Assert.IsTrue(Array.Exists(bundleFiles, p => Path.GetFileName(p).StartsWith($"{kRemoteGroupName}_assets_all_".ToLower())));
                }
            }
        }
    }

    [Test]
    public void BuildGradleProjectWithoutBuildingAddressablesFails()
    {
        PlayerSettings.Android.textureCompressionFormats = kSingleFormat;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        PlayerSettings.Android.splitApplicationBinary = true;
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

        var options = CreateSceneAndBuildPlayerOptions();
        options.locationPathName = kGradleProject;

        // build player when no addressables groups are built should fail
        var report = BuildPipeline.BuildPlayer(options);
        Assert.AreNotEqual(BuildResult.Succeeded, report.summary.result);
        LogAssert.Expect(LogType.Exception, $"Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");
        LogAssert.Expect(LogType.Error, $"Error building Player: Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");

        var builder = GetBuilderOfType(settings, typeof(BuildScriptPlayAssetDelivery));
        var builderInput = new AddressablesDataBuilderInput(settings);
        var result = builder.BuildData<AddressableAssetBuildResult>(builderInput);
        Assert.IsTrue(string.IsNullOrEmpty(result.Error));

        // build player when addressables groups are built for single texture compression only, but there is more than texture compression in settings should fail
        PlayerSettings.Android.textureCompressionFormats = kMultiFormats;
        report = BuildPipeline.BuildPlayer(options);
        Assert.AreNotEqual(BuildResult.Succeeded, report.summary.result);
        LogAssert.Expect(LogType.Exception, $"Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");
        LogAssert.Expect(LogType.Error, $"Error building Player: Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");

        result = builder.BuildData<AddressableAssetBuildResult>(builderInput);
        Assert.IsTrue(string.IsNullOrEmpty(result.Error));

        // build player when addressables groups are built for less texture compressions than texture compressions in settings should fail
        PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ETC2, TextureCompressionFormat.ASTC, TextureCompressionFormat.DXTC };
        report = BuildPipeline.BuildPlayer(options);
        Assert.AreNotEqual(BuildResult.Succeeded, report.summary.result);
        LogAssert.Expect(LogType.Exception, $"Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");
        LogAssert.Expect(LogType.Error, $"Error building Player: Exception: {PlayAssetDeliverySecondBuildProcessor.kAddressableMustBeBuiltMessage}");

        // build player using correct texture compressions settings should succeed
        PlayerSettings.Android.textureCompressionFormats = kMultiFormats;
        report = BuildPipeline.BuildPlayer(options);
        Assert.AreEqual(BuildResult.Succeeded, report.summary.result);
        ValidateBuildFolderWithTCFT(kMultiFormatPostfixes);
        ValidateGradleProjectWithAssetPacks(kGradleProject, kMultiFormatPostfixes);
    }
}
