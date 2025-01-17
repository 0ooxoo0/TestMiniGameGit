using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Android;
using UnityEngine;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryAndroidBuildTestsBase : PlayAssetDeliveryBuildTestsBase
{
    [OneTimeSetUp]
    public void InitAndroidBuild()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    protected BuildPlayerOptions CreateSceneAndBuildPlayerOptions()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var scenePath = Path.Combine(kSingleTestAssetFolder, kTestScene);
        EditorSceneManager.SaveScene(scene, scenePath, true);
        var options = new BuildPlayerOptions();
        options.scenes = new[] { scenePath };
        options.targetGroup = BuildTargetGroup.Android;
        options.target = BuildTarget.Android;
        options.options = BuildOptions.None;
        return options;
    }

    protected void BuildPlayAssetDeliveryAndGradleProject(bool oneStep, bool buildAppBundle, bool splitAppBinary, bool exportProject,
        TextureCompressionFormat[] tcFormats, string[] tcPostfixes)
    {
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
        PlayerSettings.Android.splitApplicationBinary = buildAppBundle ? splitAppBinary : false; // when building APK with split binary on, all bundles are moved inside obb file, nothing to check for us in this case
        PlayerSettings.Android.textureCompressionFormats = tcFormats;
        var useAssetPacks = buildAppBundle && (splitAppBinary || tcFormats.Length > 1);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!oneStep)
        {
            var builder = GetBuilderOfType(settings, typeof(BuildScriptPlayAssetDelivery));
            var builderInput = new AddressablesDataBuilderInput(settings);

            ModifyGroupsBeforeBuild();
            var result = builder.BuildData<AddressableAssetBuildResult>(builderInput);
            if (!useAssetPacks)
            {
                LogAssert.Expect(LogType.Warning, "Addressable content built, but Play Asset Delivery will be used only when building App Bundle with Split Application Binary option checked (or when using Texture Compression Targeting).");
            }

            CheckGroupsAfterBuild();
            Assert.IsTrue(string.IsNullOrEmpty(result.Error));

            ValidateBuildFolderWithTCFT(tcPostfixes);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        }
        else
        {
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            if (useAssetPacks)
            {
                // When building addressables with Player, BuildScriptPlayAssetDelivery will be used only if asset packs are generated
                ModifyGroupsBeforeBuild();
            }
        }

        var options = CreateSceneAndBuildPlayerOptions();
        options.locationPathName = $"{kGradleProject}{(exportProject ? "" : (buildAppBundle ? ".aab" : ".apk"))}";
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Assert.AreEqual(BuildResult.Succeeded, summary.result);

        if (oneStep)
        {
            if (useAssetPacks)
            {
                CheckGroupsAfterBuild();
                ValidateBuildFolderWithTCFT(tcPostfixes);
            }
            else
            {
                ValidateBuildFolderWithoutPAD("Android");
            }
        }
        if (!exportProject)
        {
            return;
        }
        if (useAssetPacks)
        {
            ValidateGradleProjectWithAssetPacks(kGradleProject, tcPostfixes);
        }
        else
        {
            ValidateGradleProjectWithoutAssetPacks(kGradleProject, false);
        }
    }
}
