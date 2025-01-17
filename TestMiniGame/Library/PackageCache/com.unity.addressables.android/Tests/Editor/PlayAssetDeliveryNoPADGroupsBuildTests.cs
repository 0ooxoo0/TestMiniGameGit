using NUnit.Framework;
using System;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine.AddressableAssets.Android;

[RequirePlatformSupport(BuildTarget.Android)]
internal class PlayAssetDeliveryNoPADGroupsBuildTests : PlayAssetDeliveryAndroidBuildTestsBase
{
    protected override int NumberOfGroups => 0;
    protected override int NumberOfUnitedGroups => 0;

    protected override Tuple<string, string> GroupName(int index)
    {
        return Tuple.Create($"TestGroup{index}", $"TestGroup{index}");
    }

    protected override DeliveryType GenerateDeliveryType(int index)
    {
        return DeliveryType.None;
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenAabOrGradleProject([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, true, true, exportProject, kSingleFormat, kSingleFormatPostfix);
    }

    [Test]
    public void CanBuildPlayAssetDeliveryThenAabOrGradleProjectWithTCFT([Values(false, true)] bool oneStep, [Values(false, true)] bool exportProject)
    {
        BuildPlayAssetDeliveryAndGradleProject(oneStep, true, true, exportProject, kMultiFormats, kMultiFormatPostfixes);
    }
}
