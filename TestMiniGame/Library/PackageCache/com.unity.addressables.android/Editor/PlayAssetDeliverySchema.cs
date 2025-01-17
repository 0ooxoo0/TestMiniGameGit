using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// Schema used for asset groups which should be packed to Android asset packs.
    /// </summary>
    [DisplayName("Play Asset Delivery")]
    public class PlayAssetDeliverySchema : AddressableAssetGroupSchema
    {
        [SerializeField]
        DeliveryType m_AssetPackDeliveryType = DeliveryType.FastFollow;
        /// <summary>
        /// Represents the asset pack delivery type for current Group.
        /// </summary>
        public DeliveryType AssetPackDeliveryType
        {
            get
            {
                return m_AssetPackDeliveryType;
            }
            set
            {
                if (m_AssetPackDeliveryType != value)
                {
                    m_AssetPackDeliveryType = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        string m_CustomAssetPackName = "";
        /// <summary>
        /// Represents the custom asset pack that will contain this group's bundled content if IncludeInCustomAssetPack is true.
        /// </summary>
        public string CustomAssetPackName
        {
            get
            {
                return m_CustomAssetPackName;
            }
            set
            {
                if (m_CustomAssetPackName != value)
                {
                    m_CustomAssetPackName = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        bool m_IncludeInCustomAssetPack = false;
        /// <summary>
        /// Controls whether to include content in the specified custom asset pack.
        /// We use <see cref="BuildScriptPlayAssetDelivery"/> to assign content to custom asset packs.
        /// </summary>
        public bool IncludeInCustomAssetPack
        {
            get { return m_IncludeInCustomAssetPack; }
            set
            {
                m_IncludeInCustomAssetPack = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        CustomAssetPackSettings m_Settings;
        /// <summary>
        /// Object that stores custom asset packs information.
        /// </summary>
        public CustomAssetPackSettings Settings
        {
            get
            {
                if (!CustomAssetPackSettings.SettingsExists)
                {
                    IncludeInCustomAssetPack = false;
                    CustomAssetPackName = "";
                    m_Settings = null;
                }
                if (m_Settings == null)
                {
                    m_Settings = CustomAssetPackSettings.GetSettings(true);
                }
                return m_Settings;
            }
        }

        GUIContent m_DeliveryTypeGUI =
            new GUIContent("Delivery Type", "Asset pack delivery type");

        GUIContent m_CustomAssetPackGUI =
            new GUIContent("Custom Asset Pack", "Custom asset pack name that will contain this group's bundled content.");

        GUIContent m_IncludeInCustomAssetPackGUI =
            new GUIContent("Include In Custom Asset Pack", "Controls whether to include this group's bundled content to the specified custom asset pack instead of creating separate asset pack specific for this group.");

        void ShowAssetPacks(SerializedObject so, List<AddressableAssetGroupSchema> otherSchemas = null)
        {
            List<CustomAssetPackEditorInfo> customAssetPacks = Settings.CustomAssetPacks;

            var deliveryType = AssetPackDeliveryType;
            var prop = so.FindProperty("m_AssetPackDeliveryType");
            if (otherSchemas != null)
            {
                ShowMixedValue(prop, otherSchemas, typeof(int), "m_AssetPackDeliveryType");
            }

            EditorGUI.BeginDisabledGroup(IncludeInCustomAssetPack && Settings.CustomAssetPacks.Count > 0);
            EditorGUI.BeginChangeCheck();
            var newValue = (DeliveryType)EditorGUILayout.EnumPopup(m_DeliveryTypeGUI, deliveryType);
            if (EditorGUI.EndChangeCheck())
            {
                AssetPackDeliveryType = newValue;
                if (otherSchemas != null)
                {
                    foreach (var s in otherSchemas)
                    {
                        var padSchema = s as PlayAssetDeliverySchema;
                        padSchema.AssetPackDeliveryType = newValue;
                    }
                }
            }
            EditorGUI.showMixedValue = false;

            if (IncludeInCustomAssetPack)
            {
                var index = Settings.CustomAssetPacks.FindIndex(pack => pack.AssetPackName == CustomAssetPackName);
                if (index != -1)
                {
                    deliveryType = Settings.CustomAssetPacks[index].DeliveryType;
                }
            }
            if (deliveryType == DeliveryType.None)
            {
                EditorGUILayout.HelpBox("Will still be included to the Streaming Assets.", MessageType.Info);
            }
            EditorGUI.EndDisabledGroup();

            prop = so.FindProperty("m_IncludeInCustomAssetPack");
            if (otherSchemas != null)
            {
                ShowMixedValue(prop, otherSchemas, typeof(bool), "m_IncludeInCustomAssetPack");
            }
            EditorGUI.BeginChangeCheck();
            var newIncludeInCustomAssetPack = EditorGUILayout.Toggle(m_IncludeInCustomAssetPackGUI, IncludeInCustomAssetPack);
            if (EditorGUI.EndChangeCheck())
            {
                IncludeInCustomAssetPack = newIncludeInCustomAssetPack;
                if (otherSchemas != null)
                {
                    foreach (var s in otherSchemas)
                    {
                        var padSchema = s as PlayAssetDeliverySchema;
                        padSchema.IncludeInCustomAssetPack = newIncludeInCustomAssetPack;
                    }
                }
            }
            EditorGUI.showMixedValue = false;

            if (!IncludeInCustomAssetPack)
            {
                return;
            }

            if (customAssetPacks.Count == 0)
            {
                EditorGUILayout.HelpBox("At least one custom asset pack should be created using \"Manage Custom Asset Packs\".", MessageType.Warning);
                CustomAssetPackName = "";
            }
            var current = 0;
            var displayOptions = new string[customAssetPacks.Count];
            for (int i = 0; i < customAssetPacks.Count; i++)
            {
                displayOptions[i] = $"{customAssetPacks[i].AssetPackName} ({customAssetPacks[i].DeliveryType})";
                if (customAssetPacks[i].AssetPackName == CustomAssetPackName)
                {
                    current = i;
                }
            }

            prop = so.FindProperty("m_CustomAssetPackName");
            if (otherSchemas != null)
            {
                ShowMixedValue(prop, otherSchemas, typeof(string), "m_CustomAssetPackName");
            }

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup(m_CustomAssetPackGUI, current, displayOptions);
            if (EditorGUI.EndChangeCheck() || (current < customAssetPacks.Count && customAssetPacks[current].AssetPackName != CustomAssetPackName))
            {
                CustomAssetPackName = customAssetPacks[newIndex].AssetPackName;
                if (otherSchemas != null)
                {
                    foreach (var s in otherSchemas)
                    {
                        var padSchema = s as PlayAssetDeliverySchema;
                        padSchema.CustomAssetPackName = customAssetPacks[newIndex].AssetPackName;
                    }
                }
            }
            EditorGUI.showMixedValue = false;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Manage Custom Asset Packs", "Minibutton"))
                {
                    EditorGUIUtility.PingObject(Settings);
                    Selection.activeObject = Settings;
                }
            }
        }

        /// <inheritdoc/>
        public override void OnGUI()
        {
            var so = new SerializedObject(this);
            ShowAssetPacks(so);
            so.ApplyModifiedProperties();
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            var so = new SerializedObject(this);
            ShowAssetPacks(so, otherSchemas);
            so.ApplyModifiedProperties();
        }
    }
}
