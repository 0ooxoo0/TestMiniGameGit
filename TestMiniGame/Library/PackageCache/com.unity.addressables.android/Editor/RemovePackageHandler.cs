using System;
using System.IO;
using System.Linq;
using UnityEngine.AddressableAssets.Android;
using UnityEditor.PackageManager;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Android
{
    [InitializeOnLoad]
    static class RemovePackageHandler
    {
        static RemovePackageHandler()
        {
            Action<PackageRegistrationEventArgs> packageRegisteringEventHandler = (packageRegistrationDiffEventArgs) =>
            {
                if (packageRegistrationDiffEventArgs.removed.FirstOrDefault(p => p.name == "com.unity.addressables.android") == null)
                {
                    return;
                }

                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    return;
                }
                var i = settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings);
                if (i != -1)
                {
                    settings.RemoveInitializationObject(i);
                }
                AssetDatabase.DeleteAsset(Path.Combine(CustomAssetPackUtility.RootDirectory, "InitObjects", $"{typeof(PlayAssetDeliveryInitializationSettings).Name}.asset"));

                i = settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery);
                if (i != -1)
                {
                    settings.RemoveDataBuilder(i);
                }
                AssetDatabase.DeleteAsset(Path.Combine(CustomAssetPackUtility.RootDirectory, "DataBuilders", $"{typeof(BuildScriptPlayAssetDelivery).Name}.asset"));

                i = settings.GroupTemplateObjects.FindIndex(t => (t as IGroupTemplate).Name == PlayAssetDeliverySetup.kAssetPackContentTemplateName);
                if (i != -1)
                {
                    settings.RemoveGroupTemplateObject(i);
                }
                AssetDatabase.DeleteAsset(Path.Combine(settings.GroupTemplateFolder, $"{PlayAssetDeliverySetup.kAssetPackContentTemplateName}.asset"));

                foreach (var group in settings.groups)
                {
                    if (group.HasSchema<PlayAssetDeliverySchema>())
                    {
                        group.RemoveSchema<PlayAssetDeliverySchema>();
                    }
                }

                AssetDatabase.DeleteAsset(CustomAssetPackUtility.BuildRootDirectory);
                BuildScriptPlayAssetDelivery.ClearTextureCompressionTargetedContent();
            };

            UnityEditor.PackageManager.Events.registeringPackages -= packageRegisteringEventHandler; // avoiding adding handler multiple times
            UnityEditor.PackageManager.Events.registeringPackages += packageRegisteringEventHandler;
        }
    }
}
