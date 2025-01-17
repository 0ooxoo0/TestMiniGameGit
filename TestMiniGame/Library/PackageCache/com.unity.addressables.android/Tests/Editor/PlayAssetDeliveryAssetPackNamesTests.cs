using NUnit.Framework;
using System;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Android;
using UnityEngine.AddressableAssets.Android;
using UnityEngine;
using UnityEngine.TestTools;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryAssetPackNamesTests : PlayAssetDeliveryTestsBase
{
    protected override int NumberOfGroups => 7;
    protected override int NumberOfUnitedGroups => 3;

#if UNITY_EDITOR || UNITY_ANDROID
    protected override Tuple<string, string> GroupName(int index)
    {
        switch (index)
        {
            case 0:
                // non valid characters are removed
                return Tuple.Create("My@Test#Pack0", "MyTestPack0");
            case 1:
                // non valid characters are removed, '1' added because the result is the same as kCustomAssetPackName
                return Tuple.Create("!Test#Pack", "TestPack1");
            case 2:
                // non valid characters are removed, 'Group' is added at the beginning because the result starts with '_'
                return Tuple.Create("_Test$=Pack", "Group_TestPack");
            case 3:
                // non valid characters are removed, 'Group' is used as a name because the result is empty
                return Tuple.Create("!&$", "Group");
            case 4:
                // non valid characters are removed, 'Group1' is used as a name because the result is empty and 'Group' already exists
                return Tuple.Create("-()+", "Group1");
            case 5:
                // non valid characters are removed, 'Group' added at the beginning because the result starts with digit
                return Tuple.Create("!0TestPack[]", "Group0TestPack");
            case 6:
                // AddressablesAssetPack is reserved
                return Tuple.Create("AddressablesAssetPack", "AddressablesAssetPack1");
            case 7:
                return Tuple.Create("g1", kCustomAssetPackName);
            case 8:
                return Tuple.Create("g2", kCustomAssetPackName);
            case 9:
                return Tuple.Create("g3", "g3");
            default:
                return Tuple.Create("", "");
        }
    }

    protected override string CustomAssetPackName(int index)
    {
        if (index < NumberOfGroups)
        {
            return "";
        }
        if (index == NumberOfGroups + NumberOfUnitedGroups - 1)
        {
            return kWrongCustomAssetPackName;
        }
        return kCustomAssetPackName;
    }

    protected override DeliveryType GenerateDeliveryType(int index)
    {
        return DeliveryType.FastFollow;
    }
#endif

    [Test]
    public void EnsureGroupNamesAreCorrect()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.Android.splitApplicationBinary = true;
        PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ETC2 };

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var builder = GetBuilderOfType(settings, typeof(BuildScriptPlayAssetDelivery));
        var builderInput = new AddressablesDataBuilderInput(settings);
        var result = builder.BuildData<AddressableAssetBuildResult>(builderInput);

        Assert.IsTrue(string.IsNullOrEmpty(result.Error));
        LogAssert.Expect(LogType.Warning, $"Group '{GroupName(9).Item1}' supposed to be included to the '{kWrongCustomAssetPackName}' custom asset pack which doesn't exist. Separate asset pack for this group will be created.");

        ValidateBuildFolderWithTCFT(new[] { "" });
    }
}
