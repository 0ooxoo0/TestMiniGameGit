using NUnit.Framework;
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
internal class CustomAssetPackSettingsTests
{
    const string kAssetPackName = "CustomAssetPack";
    const string kNewAssetPackName = "NewCustomAssetPack";
    const string kGroupName = "TestGroup";
    CustomAssetPackSettings m_CustomAssetPackSettings;

    [SetUp]
    public void InitCustomAssetPackSettings()
    {
        AddressableAssetSettingsDefaultObject.Settings =
            AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
            AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        m_CustomAssetPackSettings = CustomAssetPackSettings.GetSettings(true);
    }

    [TearDown]
    public void CleanupCustomAssetPackSettings()
    {
        PlayAssetDeliveryTestsBase.DeleteDirectoryFromAssets(CustomAssetPackUtility.RootDirectory);
        PlayAssetDeliveryTestsBase.DeleteDirectoryFromAssets(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ChangeToReservedNameAddsPostfix()
    {
        m_CustomAssetPackSettings.AddUniqueAssetPack();
        m_CustomAssetPackSettings.SetAssetPackName(0, CustomAssetPackUtility.kAddressablesAssetPackName);
        Assert.AreEqual($"{CustomAssetPackUtility.kAddressablesAssetPackName}1", m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
    }

    [Test]
    public void ChangeToExistingNameAddsPostfix()
    {
        m_CustomAssetPackSettings.AddUniqueAssetPack();
        m_CustomAssetPackSettings.SetAssetPackName(0, kAssetPackName);
        Assert.AreEqual(kAssetPackName, m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        m_CustomAssetPackSettings.AddUniqueAssetPack();
        var name = m_CustomAssetPackSettings.CustomAssetPacks[1].AssetPackName;
        m_CustomAssetPackSettings.SetAssetPackName(1, kAssetPackName);
        Assert.AreEqual($"{kAssetPackName}1", m_CustomAssetPackSettings.CustomAssetPacks[1].AssetPackName);
    }

    [Test]
    public void CantChangeToInvalidName()
    {
        m_CustomAssetPackSettings.AddUniqueAssetPack();
        var oldName = m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName;
        foreach (var name in new[] { "My@Test#Pack0", "_TestPack", "0TestPack" })
        {
            m_CustomAssetPackSettings.SetAssetPackName(0, name);
            LogAssert.Expect(LogType.Error, $"Cannot name custom asset pack '{name}'. All characters must be alphanumeric or an underscore. Also the first character must be a letter.");
            Assert.AreEqual(oldName, m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        }
    }

    [Test]
    public void CanChangeToValidName()
    {
        m_CustomAssetPackSettings.AddUniqueAssetPack();
        foreach (var name in new[] { "MyTestPack", "My_TestPack", "MyTestPack123" })
        {
            m_CustomAssetPackSettings.SetAssetPackName(0, name);
            Assert.AreEqual(name, m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        }
    }

    [Test]
    public void ChangeNameWorks()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        for (int i = 0; i < 3; ++i)
        {
            m_CustomAssetPackSettings.AddAssetPack($"{kAssetPackName}{i}", DeliveryType.FastFollow);
        }
        Assert.AreEqual(3, m_CustomAssetPackSettings.CustomAssetPacks.Count);
        AssetDatabase.SaveAssets();
        AddressableAssetGroup[] groups = new AddressableAssetGroup[6];
        for (int i = 0; i < 6; ++i)
        {
            groups[i] = settings.CreateGroup($"{kGroupName}{i}", true, false, false, null, typeof(BundledAssetGroupSchema), typeof(PlayAssetDeliverySchema));
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            assetPackSchema.IncludeInCustomAssetPack = true;
            assetPackSchema.CustomAssetPackName = $"{kAssetPackName}{i % 3}";
        }
        // changing name using name
        m_CustomAssetPackSettings.SetAssetPackName($"{kAssetPackName}1", kNewAssetPackName);
        Assert.AreEqual($"{kAssetPackName}0", m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        Assert.AreEqual(kNewAssetPackName, m_CustomAssetPackSettings.CustomAssetPacks[1].AssetPackName);
        Assert.AreEqual($"{kAssetPackName}2", m_CustomAssetPackSettings.CustomAssetPacks[2].AssetPackName);
        for (int i = 0; i < 6; ++i)
        {
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            Assert.AreEqual(i % 3 == 1 ? kNewAssetPackName : $"{kAssetPackName}{i % 3}", assetPackSchema.CustomAssetPackName);
        }
        // changing name using index
        m_CustomAssetPackSettings.SetAssetPackName(1, kAssetPackName);
        Assert.AreEqual($"{kAssetPackName}0", m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        Assert.AreEqual(kAssetPackName, m_CustomAssetPackSettings.CustomAssetPacks[1].AssetPackName);
        Assert.AreEqual($"{kAssetPackName}2", m_CustomAssetPackSettings.CustomAssetPacks[2].AssetPackName);
        for (int i = 0; i < 6; ++i)
        {
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            Assert.AreEqual(i % 3 == 1 ? kAssetPackName : $"{kAssetPackName}{i % 3}", assetPackSchema.CustomAssetPackName);
        }
        // changing using wrong name fails
        m_CustomAssetPackSettings.SetAssetPackName($"{kAssetPackName}3", kNewAssetPackName);
        LogAssert.Expect(LogType.Error, $"Asset pack with name '{kAssetPackName}3' not found");
    }

    [Test]
    public void ChangeDeliveryTypeWorks()
    {
        for (int i = 0; i < 3; ++i)
        {
            m_CustomAssetPackSettings.AddAssetPack($"{kAssetPackName}{i}", DeliveryType.FastFollow);
        }
        Assert.AreEqual(3, m_CustomAssetPackSettings.CustomAssetPacks.Count);
        // change delivery type using name
        m_CustomAssetPackSettings.SetDeliveryType($"{kAssetPackName}1", DeliveryType.OnDemand);
        Assert.AreEqual(DeliveryType.FastFollow, m_CustomAssetPackSettings.CustomAssetPacks[0].DeliveryType);
        Assert.AreEqual(DeliveryType.OnDemand, m_CustomAssetPackSettings.CustomAssetPacks[1].DeliveryType);
        Assert.AreEqual(DeliveryType.FastFollow, m_CustomAssetPackSettings.CustomAssetPacks[2].DeliveryType);
        // change delivery type using index
        m_CustomAssetPackSettings.SetDeliveryType(1, DeliveryType.InstallTime);
        Assert.AreEqual(DeliveryType.FastFollow, m_CustomAssetPackSettings.CustomAssetPacks[0].DeliveryType);
        Assert.AreEqual(DeliveryType.InstallTime, m_CustomAssetPackSettings.CustomAssetPacks[1].DeliveryType);
        Assert.AreEqual(DeliveryType.FastFollow, m_CustomAssetPackSettings.CustomAssetPacks[2].DeliveryType);
        // change delivery type using wrong name fails
        m_CustomAssetPackSettings.SetDeliveryType($"{kAssetPackName}3", DeliveryType.OnDemand);
        LogAssert.Expect(LogType.Error, $"Asset pack with name '{kAssetPackName}3' not found");
    }

    [Test]
    public void RemoveCustomAssetPackWorks()
    {
        for (int i = 0; i < 3; ++i)
        {
            m_CustomAssetPackSettings.AddAssetPack($"{kAssetPackName}{i}", DeliveryType.FastFollow);
        }
        Assert.AreEqual(3, m_CustomAssetPackSettings.CustomAssetPacks.Count);
        AssetDatabase.SaveAssets();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup[] groups = new AddressableAssetGroup[6];
        for (int i = 0; i < 6; ++i)
        {
            groups[i] = settings.CreateGroup($"{kGroupName}{i}", true, false, false, null, typeof(BundledAssetGroupSchema), typeof(PlayAssetDeliverySchema));
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            assetPackSchema.AssetPackDeliveryType = DeliveryType.OnDemand;
            assetPackSchema.IncludeInCustomAssetPack = true;
            assetPackSchema.CustomAssetPackName = $"{kAssetPackName}{i % 3}";
        }
        // remove using name
        m_CustomAssetPackSettings.RemoveAssetPack($"{kAssetPackName}1");
        Assert.AreEqual(2, m_CustomAssetPackSettings.CustomAssetPacks.Count);
        Assert.AreEqual($"{kAssetPackName}0", m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        Assert.AreEqual($"{kAssetPackName}2", m_CustomAssetPackSettings.CustomAssetPacks[1].AssetPackName);
        for (int i = 0; i < 6; ++i)
        {
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            if (i % 3 == 1)
            {
                Assert.AreEqual(false, assetPackSchema.IncludeInCustomAssetPack);
                Assert.AreEqual(DeliveryType.FastFollow, assetPackSchema.AssetPackDeliveryType);
            }
            else
            {
                Assert.AreEqual(true, assetPackSchema.IncludeInCustomAssetPack);
                Assert.AreEqual($"{kAssetPackName}{i % 3}", assetPackSchema.CustomAssetPackName);
            }
        }
        // remove using index
        m_CustomAssetPackSettings.RemovePackAtIndex(0);
        Assert.AreEqual(1, m_CustomAssetPackSettings.CustomAssetPacks.Count);
        Assert.AreEqual($"{kAssetPackName}2", m_CustomAssetPackSettings.CustomAssetPacks[0].AssetPackName);
        for (int i = 0; i < 6; ++i)
        {
            var assetPackSchema = groups[i].GetSchema<PlayAssetDeliverySchema>();
            if (i % 3 != 2)
            {
                Assert.AreEqual(false, assetPackSchema.IncludeInCustomAssetPack);
                Assert.AreEqual(DeliveryType.FastFollow, assetPackSchema.AssetPackDeliveryType);
            }
            else
            {
                Assert.AreEqual(true, assetPackSchema.IncludeInCustomAssetPack);
                Assert.AreEqual($"{kAssetPackName}{i % 3}", assetPackSchema.CustomAssetPackName);
            }
        }
        // remove using wrong name fails
        m_CustomAssetPackSettings.RemoveAssetPack($"{kAssetPackName}3");
        LogAssert.Expect(LogType.Error, $"Asset pack with name '{kAssetPackName}3' not found");
    }

}
