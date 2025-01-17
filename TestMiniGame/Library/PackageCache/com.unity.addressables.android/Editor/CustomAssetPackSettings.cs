using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// Custom asset pack information.
    /// </summary>
    [Serializable]
    public class CustomAssetPackEditorInfo
    {
        /// <summary>
        /// Custom asset pack name.
        /// </summary>
        public string AssetPackName;

        /// <summary>
        /// Custom asset pack delivery type.
        /// </summary>
        public DeliveryType DeliveryType;

        /// <summary>
        /// Create a new CustomAssetPackEditorInfo object.
        /// </summary>
        /// <param name="assetPackName">Custom asset pack name.</param>
        /// <param name="deliveryType">Custom asset pack delivery type.</param>
        public CustomAssetPackEditorInfo(string assetPackName, DeliveryType deliveryType)
        {
            AssetPackName = assetPackName;
            DeliveryType = deliveryType;
        }
    }

    /// <summary>
    /// Stores information (name and delivery type) for all custom asset packs.
    /// </summary>
    public class CustomAssetPackSettings : ScriptableObject
    {
        const string kDefaultConfigObjectName = "CustomAssetPackSettings";
        static string kDefaultConfigAssetPath => Path.Combine(CustomAssetPackUtility.RootDirectory, $"{kDefaultConfigObjectName}.asset");

        const string kDefaultPackName = "AddressablesCustomPack";
        const DeliveryType kDefaultDeliveryType = DeliveryType.FastFollow;

        readonly Regex kValidAssetPackName = new Regex(@"^[A-Za-z][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        [SerializeField]
        List<CustomAssetPackEditorInfo> m_CustomAssetPacks = new List<CustomAssetPackEditorInfo>();

        /// <summary>
        /// Store all custom asset pack information.
        /// </summary>
        public List<CustomAssetPackEditorInfo> CustomAssetPacks => m_CustomAssetPacks;

        // Temporary set to store names of all custom asset packs and names of asset packs generated from Addressables groups. It is being used
        // during building Addressables for Play Asset Delivery to ensure that names of asset packs are unique. This set is being initialized with
        // names of all CustomAssetPacks when the build starts and then new names for asset packs generated from Addressables groups are added to it.
        HashSet<string> m_AllCustomAssetPacksNames;

        void AddCustomAssetPackInternal(string assetPackName, DeliveryType deliveryType)
        {
            CustomAssetPacks.Add(new CustomAssetPackEditorInfo(assetPackName, deliveryType));
            EditorUtility.SetDirty(this);
        }

        internal void AddUniqueAssetPack()
        {
            var assetPackName = GenerateUniqueName(kDefaultPackName);
            AddCustomAssetPackInternal(assetPackName, kDefaultDeliveryType);
        }

        internal void SetAssetPackName(int index, string assetPackName)
        {
            if (index < 0 || index >= CustomAssetPacks.Count)
            {
                return;
            }
            if (!kValidAssetPackName.IsMatch(assetPackName))
            {
                Debug.LogError($"Cannot name custom asset pack '{assetPackName}'. All characters must be alphanumeric or an underscore. Also the first character must be a letter.");
            }
            else
            {
                var newName = GenerateUniqueName(assetPackName);
                var schemaToUpdate = GetSchemasWhichUseCustomAssetPack(CustomAssetPacks[index].AssetPackName);
                foreach (var schema in schemaToUpdate)
                {
                    schema.CustomAssetPackName = newName;
                }
                CustomAssetPacks[index].AssetPackName = newName;
                EditorUtility.SetDirty(this);
            }
        }

        internal void SetDeliveryType(int index, DeliveryType deliveryType)
        {
            if (index < 0 || index >= CustomAssetPacks.Count)
            {
                return;
            }
            CustomAssetPacks[index].DeliveryType = deliveryType;
            EditorUtility.SetDirty(this);
        }

        internal void RemovePackAtIndex(int index)
        {
            if (index < 0 || index >= CustomAssetPacks.Count)
            {
                return;
            }
            var schemaToUpdate = GetSchemasWhichUseCustomAssetPack(CustomAssetPacks[index].AssetPackName);
            foreach (var schema in schemaToUpdate)
            {
                schema.AssetPackDeliveryType = CustomAssetPacks[index].DeliveryType;
                schema.IncludeInCustomAssetPack = false;
            }
            CustomAssetPacks.RemoveAt(index);
            EditorUtility.SetDirty(this);
        }

        int GetAssetPackIndex(string name)
        {
            var index = CustomAssetPacks.FindIndex(p => p.AssetPackName == name);
            if (index == -1)
            {
                Debug.LogError($"Asset pack with name '{name}' not found");
            }
            return index;
        }

        /// <summary>
        /// Creates a new asset pack using name and delivery type.
        /// </summary>
        /// <remarks>
        /// If the new name is invalid (contains non alphanumeric, underscore characters, or doesn't start with a letter), asset pack is not created.
        /// If the new name already exists, new unique name is generated by adding numeric postfix.
        /// </remarks>
        /// <param name="assetPackName">New asset pack name.</param>
        /// <param name="deliveryType">Delivery type for the new asset pack.</param>
        public void AddAssetPack(string assetPackName, DeliveryType deliveryType)
        {
            if (!kValidAssetPackName.IsMatch(assetPackName))
            {
                Debug.LogError($"Cannot name custom asset pack '{assetPackName}'. All characters must be alphanumeric or an underscore. Also the first character must be a letter.");
            }
            else
            {
                var newName = GenerateUniqueName(assetPackName);
                AddCustomAssetPackInternal(newName, deliveryType);
            }
        }

        /// <summary>
        /// Change asset pack name for the custom asset pack.
        /// </summary>
        /// <remarks>
        /// If asset pack with assetPackName doesn't exist, this method does nothing.
        /// If new name is invalid (contains non alphanumeric+underscore characters, or doesn't start with a letter) name is not changed.
        /// If new name already exists new unique name is generated by adding numeric postfix.
        /// </remarks>
        /// <param name="assetPackName">Asset pack name which should be changed.</param>
        /// <param name="newAssetPackName">New asset pack name.</param>
        public void SetAssetPackName(string assetPackName, string newAssetPackName)
        {
            var index = GetAssetPackIndex(assetPackName);
            if (index != -1)
            {
                SetAssetPackName(index, newAssetPackName);
            }
        }

        /// <summary>
        /// Change delivery type for the custom asset pack.
        /// </summary>
        /// <remarks>
        /// If asset pack with assetPackName doesn't exist, this method does nothing.
        /// </remarks>
        /// <param name="assetPackName">Asset pack name.</param>
        /// <param name="deliveryType">New delivery type.</param>
        public void SetDeliveryType(string assetPackName, DeliveryType deliveryType)
        {
            var index = GetAssetPackIndex(assetPackName);
            if (index != -1)
            {
                SetDeliveryType(index, deliveryType);
            }
        }

        /// <summary>
        /// Removes custom asset pack.
        /// </summary>
        /// <remarks>
        /// If asset pack with assetPackName doesn't exist, this method does nothing.
        /// </remarks>
        /// <param name="assetPackName">Asset pack name to remove.</param>
        public void RemoveAssetPack(string assetPackName)
        {
            var index = GetAssetPackIndex(assetPackName);
            if (index != -1)
            {
                RemovePackAtIndex(index);
            }
        }

        /// <summary>
        /// Returns true if CustomAssetPackSettings asset exists.
        /// </summary>
        public static bool SettingsExists => !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(kDefaultConfigAssetPath));

        /// <summary>
        /// Returns CustomAssetPackSettings asset.
        /// </summary>
        /// <param name="create">Force creating CustomAssetPackSettings asset if it doesn't exist.</param>
        /// <returns>CustomAssetPackSettings asset.</returns>
        public static CustomAssetPackSettings GetSettings(bool create)
        {
            var settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(kDefaultConfigAssetPath);
            if (create && settings == null)
            {
                settings = CreateInstance<CustomAssetPackSettings>();

                if (!AssetDatabase.IsValidFolder(CustomAssetPackUtility.RootDirectory))
                {
                    Directory.CreateDirectory(CustomAssetPackUtility.RootDirectory);
                }
                AssetDatabase.CreateAsset(settings, kDefaultConfigAssetPath);
                settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(kDefaultConfigAssetPath);

                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        bool CustomAssetPackNameExists(string name)
        {
            return name == CustomAssetPackUtility.kAddressablesAssetPackName || CustomAssetPacks.FindIndex(p => p.AssetPackName == name) != -1;
        }

        bool AssetPackNameExists(string name)
        {
            return m_AllCustomAssetPacksNames.Contains(name);
        }

        string GenerateUniqueName(string baseName, Func<string, bool> exists)
        {
            int counter = 1;
            var newName = baseName;
            while (exists(newName))
            {
                newName = baseName + counter;
                counter++;
                if (counter == 50)
                {
                    // There can be up to 50 asset packs in an Android App Bundle
                    throw new OverflowException("Too many asset packs are created.");
                }
            }
            return newName;
        }

        string GenerateUniqueName(string baseName)
        {
            return GenerateUniqueName(baseName, CustomAssetPackNameExists);
        }

        internal void ResetGeneratingUniqueAssetPacksNames()
        {
            m_AllCustomAssetPacksNames = new HashSet<string>(CustomAssetPacks.Select(p => p.AssetPackName));
            m_AllCustomAssetPacksNames.Add(CustomAssetPackUtility.kAddressablesAssetPackName);
        }

        internal string GenerateUniqueAssetPackName(string baseName)
        {
            var newName = GenerateUniqueName(baseName, AssetPackNameExists);
            m_AllCustomAssetPacksNames.Add(newName);
            return newName;
        }

        List<PlayAssetDeliverySchema> GetSchemasWhichUseCustomAssetPack(string name)
        {
            var schemaList = new List<PlayAssetDeliverySchema>();
            var groups = AddressableAssetSettingsDefaultObject.Settings?.groups;
            if (groups == null)
            {
                return schemaList;
            }
            foreach (var group in groups)
            {
                if (group.HasSchema<PlayAssetDeliverySchema>())
                {
                    var schema = group.GetSchema<PlayAssetDeliverySchema>();
                    if (schema.Settings == this && schema.IncludeInCustomAssetPack && schema.CustomAssetPackName == name)
                    {
                        schemaList.Add(schema);
                    }
                }
            }
            return schemaList;
        }
    }
}
