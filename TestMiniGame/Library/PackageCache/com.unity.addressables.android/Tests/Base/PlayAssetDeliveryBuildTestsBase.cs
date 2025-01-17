using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.AddressableAssets.Android;

internal abstract class PlayAssetDeliveryBuildTestsBase : PlayAssetDeliveryTestsBase
{
    protected override int NumberOfGroups => 5;
    protected override int NumberOfUnitedGroups => 3;

#if UNITY_EDITOR
    protected readonly TextureCompressionFormat[] kSingleFormat = new[] { TextureCompressionFormat.ETC2 };
    protected readonly string[] kSingleFormatPostfix = new[] { "" };
    protected readonly TextureCompressionFormat[] kMultiFormats = new[] { TextureCompressionFormat.ETC2, TextureCompressionFormat.ASTC };
    protected readonly string[] kMultiFormatPostfixes = new[] { "", "#tcf_etc2", "#tcf_astc" };

    protected const string kRemoteGroupName = "RemoteGroup";
#endif

#if UNITY_EDITOR || UNITY_ANDROID
    protected override Tuple<string, string> GroupName(int index)
    {
        switch (index)
        {
            case 0:
                return Tuple.Create($"TestGroup{index}", CustomAssetPackUtility.kAddressablesAssetPackName);
            case 1:
            case 2:
            case 4:
            case 7:
                return Tuple.Create($"TestGroup{index}", $"TestGroup{index}");
            case 5:
            case 6:
                return Tuple.Create($"TestGroup{index}", kCustomAssetPackName);
            default:
                return Tuple.Create($"TestGroup{index}", "");
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
        switch (index)
        {
            case 0:
                return DeliveryType.InstallTime;
            case 1:
            case 7:
                return DeliveryType.FastFollow;
            case 2:
            case 4:
            case 5:
            case 6:
                return DeliveryType.OnDemand;
            default:
                return DeliveryType.None;
        }
    }
#endif

    protected override bool PackTogether(int index)
    {
        return (index != 4);
    }
}
