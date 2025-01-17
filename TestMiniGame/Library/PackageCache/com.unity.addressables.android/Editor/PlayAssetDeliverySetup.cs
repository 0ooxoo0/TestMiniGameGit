using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    class PlayAssetDeliverySetup
    {
        internal const string kInitPlayAssetDeliveryMenuItem = "Window/Asset Management/Addressables/Init Play Asset Delivery";
        internal const string kAssetPackContentTemplateName = "Play Asset Delivery Assets";

        internal const int kMaxAssetPacksNumber = 50;
        internal static string kTooManyAssetPacksMessage => $"The total number of asset packs in your App Bundle will be more than {kMaxAssetPacksNumber}. Google Play store accepts apps with up to {kMaxAssetPacksNumber} asset packs.\nConsider making some Addressables groups \"install-time\" or combining them into custom asset packs.";

        // required for auto-testing
        internal static bool? ForcePADToExistingAddressablesGroup { private get; set; } = null;

        static T CreateScriptAsset<T>(string subfolder) where T : ScriptableObject
        {
            var script = ScriptableObject.CreateInstance<T>();
            var path = Path.Combine(CustomAssetPackUtility.RootDirectory, subfolder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, $"{typeof(T).Name}.asset");
            if (!File.Exists(path))
            {
                AssetDatabase.CreateAsset(script, path);
            }
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static bool CreateAssetPackContentTemplate(AddressableAssetSettings aa)
        {
            var assetPath = Path.Combine(aa.GroupTemplateFolder, $"{kAssetPackContentTemplateName}.asset");

            if (File.Exists(assetPath))
            {
                return aa.AddGroupTemplateObject(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IGroupTemplate);
            }

            return aa.CreateAndAddGroupTemplate(kAssetPackContentTemplateName, "Pack assets into custom asset packs.", typeof(BundledAssetGroupSchema), typeof(PlayAssetDeliverySchema));
        }

        [MenuItem(kInitPlayAssetDeliveryMenuItem, priority = 2049)]
        internal static void InitPlayAssetDelivery()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("No Addressable settings file exists.  Open 'Window/Asset Management/Addressables/Groups' for more info.");
            }
            else
            {
                if (settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings) == -1)
                {
                    // add PlayAssetDeliveryInitialization script asset if required
                    settings.AddInitializationObject(CreateScriptAsset<PlayAssetDeliveryInitializationSettings>("InitObjects"));
                }

                if (settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery) == -1)
                {
                    // add BuildScriptPlayAssetDelivery script asset if required
                    settings.AddDataBuilder(CreateScriptAsset<BuildScriptPlayAssetDelivery>("DataBuilders"));
                }

                CreateAssetPackContentTemplate(settings);

                var displayChoice = ForcePADToExistingAddressablesGroup ?? EditorUtility.DisplayDialog("Play Asset Delivery support",
                    "Play Asset Delivery support is initialized. \nClick 'Add' to assign Play Asset Delivery schema to the existing Addressable groups. \nIf you choose to skip this step, you have the option to manually assign the schema later.",
                    "Add", "Skip");
                if (displayChoice)
                {
                    var assetPacks = 2; // at least UnityDataAssetPack and AddressablesAssetPack will be created
                    foreach (var group in settings.groups)
                    {
                        if (group.HasSchema<BundledAssetGroupSchema>() && !group.HasSchema<PlayAssetDeliverySchema>())
                        {
                            group.AddSchema<PlayAssetDeliverySchema>();
                        }
                        if (!group.HasSchema<PlayAssetDeliverySchema>())
                        {
                            continue;
                        }
                        var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
                        var deliveryType = assetPackSchema.AssetPackDeliveryType;
                        if (!assetPackSchema.IncludeInCustomAssetPack && (deliveryType == DeliveryType.FastFollow || deliveryType == DeliveryType.OnDemand))
                        {
                            ++assetPacks;
                        }
                    }
                    if (assetPacks > kMaxAssetPacksNumber)
                    {
                        if (ForcePADToExistingAddressablesGroup == null)
                        {
                            EditorUtility.DisplayDialog("Play Asset Delivery", kTooManyAssetPacksMessage, "OK");
                        }
                        Debug.LogWarning(kTooManyAssetPacksMessage);
                    }
                }

                Debug.Log("Addressables are initialized to use with Play Asset Delivery.");
            }
        }

        [MenuItem(kInitPlayAssetDeliveryMenuItem, true)]
        internal static bool PlayAssetDeliveryNotInitialized()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return true;
            }
            // Initialization required if PlayAssetDeliveryInitializationSettings or BuildScriptPlayAssetDelivery
            // are not added to the default Addressables settings
            return settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings) == -1 ||
                   settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery) == -1;
        }
    }
}
