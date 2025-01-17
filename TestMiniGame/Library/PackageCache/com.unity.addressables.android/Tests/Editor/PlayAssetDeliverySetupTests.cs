using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Android;
using UnityEngine.AddressableAssets.Android;
using UnityEngine;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliverySetupTests
{
    [Test]
    public void InitPlayAssetDeliveryWhenAddressablesNotInitializedFailed()
    {
        Assert.AreEqual(null, AddressableAssetSettingsDefaultObject.Settings);
        PlayAssetDeliverySetup.InitPlayAssetDelivery();
        LogAssert.Expect(LogType.Warning, "No Addressable settings file exists.  Open 'Window/Asset Management/Addressables/Groups' for more info.");
    }

    [Test]
    public void PlayAssetDeliveryNotInitializedWorks([Values(true, false)] bool createInitObject)
    {
        AddressableAssetSettingsDefaultObject.Settings =
            AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
            AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (createInitObject)
        {
            settings.AddInitializationObject(PlayAssetDeliveryTestsBase.CreateScriptAsset<PlayAssetDeliveryInitializationSettings>("InitObjects"));
        }
        else
        {
            settings.AddDataBuilder(PlayAssetDeliveryTestsBase.CreateScriptAsset<BuildScriptPlayAssetDelivery>("DataBuilders"));
        }

        Assert.IsTrue(PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized());
    }

    [Test]
    public void InitPlayAssetDeliveryWorks([Values(true, false)] bool addPADSchema)
    {
        AddressableAssetSettingsDefaultObject.Settings =
            AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
            AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group1 = settings.CreateGroup("Group1", true, false, false, null, typeof(BundledAssetGroupSchema));
        var group2 = settings.CreateGroup("Group2", true, false, false, null, typeof(BundledAssetGroupSchema));
        var group3 = settings.CreateGroup("Group2", true, false, false, null, typeof(ContentUpdateGroupSchema));

        Assert.IsTrue(PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized());
        PlayAssetDeliverySetup.ForcePADToExistingAddressablesGroup = addPADSchema;
        PlayAssetDeliverySetup.InitPlayAssetDelivery();
        LogAssert.Expect(LogType.Log, "Addressables are initialized to use with Play Asset Delivery.");
        Assert.IsFalse(PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized());

        Assert.AreEqual(addPADSchema, group1.HasSchema<PlayAssetDeliverySchema>());
        Assert.AreEqual(addPADSchema, group2.HasSchema<PlayAssetDeliverySchema>());
        Assert.IsFalse(group3.HasSchema<PlayAssetDeliverySchema>()); // PAD schema is added only when there is BundledAssetGroupSchema

        Assert.AreNotEqual(-1, settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery));
        Assert.AreNotEqual(-1, settings.InitializationObjects.FindIndex(o => o is PlayAssetDeliveryInitializationSettings));

        var groupTemplatePath = Path.Combine(settings.GroupTemplateFolder, $"{PlayAssetDeliverySetup.kAssetPackContentTemplateName}.asset");
        Assert.IsTrue(File.Exists(groupTemplatePath));
        var template = AssetDatabase.LoadAssetAtPath(groupTemplatePath, typeof(ScriptableObject)) as AddressableAssetGroupTemplate;
        Assert.AreEqual(PlayAssetDeliverySetup.kAssetPackContentTemplateName, template.Name);
        Assert.IsTrue(template.HasSchema(typeof(BundledAssetGroupSchema)));
        Assert.IsTrue(template.HasSchema(typeof(PlayAssetDeliverySchema)));
    }

    [Test]
    public void WarningAboutTooManyAssetPacksDisplayed()
    {
        AddressableAssetSettingsDefaultObject.Settings =
            AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
            AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        for (var i = 0; i < PlayAssetDeliverySetup.kMaxAssetPacksNumber - 1; ++i)
        {
            settings.CreateGroup($"Group{i}", true, false, false, null, typeof(BundledAssetGroupSchema));
        }
        Assert.IsTrue(PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized());
        PlayAssetDeliverySetup.ForcePADToExistingAddressablesGroup = true;
        PlayAssetDeliverySetup.InitPlayAssetDelivery();
        LogAssert.Expect(LogType.Warning, PlayAssetDeliverySetup.kTooManyAssetPacksMessage);
        LogAssert.Expect(LogType.Log, "Addressables are initialized to use with Play Asset Delivery.");
        Assert.IsFalse(PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized());
    }

    [TearDown]
    public void CleanupAddressables()
    {
        PlayAssetDeliveryTestsBase.DeleteDirectoryFromAssets(CustomAssetPackUtility.RootDirectory);
        PlayAssetDeliveryTestsBase.DeleteDirectoryFromAssets(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);
        AssetDatabase.Refresh();
    }
}
