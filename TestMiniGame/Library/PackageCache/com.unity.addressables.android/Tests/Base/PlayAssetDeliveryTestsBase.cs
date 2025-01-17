using NUnit.Framework;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Android;
#endif
using UnityEngine.AddressableAssets.Android;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine;
using UnityEngine.AddressableAssets;

internal abstract class PlayAssetDeliveryTestsBase
{
    protected const string kCustomAssetPackName = "TestPack";
    protected const string kWrongCustomAssetPackName = "NotExistingTestPack";

    protected string TextureName(int index) => $"testTexture{index}.png";

#if UNITY_EDITOR || UNITY_ANDROID
    protected abstract Tuple<string, string> GroupName(int index);
    protected abstract string CustomAssetPackName(int index);
    protected abstract DeliveryType GenerateDeliveryType(int index);
#endif

    protected virtual bool PackTogether(int index) { return true; }

    protected const string kTestFolder = "TestFolder";

    protected static string kSingleTestAssetFolder => Path.Combine("Assets", kTestFolder);

    protected const string kGradleProject = "Test";

    protected const string kTestScene = "test.unity";

    protected abstract int NumberOfGroups { get; }
    protected abstract int NumberOfUnitedGroups { get; }
    protected int TotalNumberOfGroups => NumberOfGroups + NumberOfUnitedGroups;

#if UNITY_EDITOR
    protected string CreateTexture(string path)
    {
        var data = ImageConversion.EncodeToPNG(new Texture2D(32, 32));
        File.WriteAllBytes(path, data);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        return AssetDatabase.AssetPathToGUID(path);
    }

    protected string CreateAsset(string path, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        //this is to ensure that bundles are different for every run.
        go.transform.localPosition = UnityEngine.Random.onUnitSphere;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go, false);
        return AssetDatabase.AssetPathToGUID(path);
    }

    protected static IDataBuilder GetBuilderOfType(AddressableAssetSettings settings, Type modeType)
    {
        foreach (var db in settings.DataBuilders)
        {
            if (db.GetType() == modeType)
            {
                return db as IDataBuilder;
            }
        }

        throw new Exception("DataBuilder not found");
    }

    internal static void DeleteDirectoryFromAssets(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }
        var metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(dir);
        Directory.Delete(dir, true);
        if (File.Exists(metaFilePath))
        {
            File.Delete(metaFilePath);
        }
    }

    internal static T CreateScriptAsset<T>(string subfolder) where T : ScriptableObject
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

    void ValidateAssetPack(string androidPackName, DeliveryType type, string gradleProject)
    {
        var assetPackFolderName = Path.Combine(gradleProject, androidPackName);
        Assert.IsTrue(Directory.Exists(assetPackFolderName), $"{androidPackName} doesn't exist");
        var buildFilePath = Path.Combine(assetPackFolderName, "build.gradle");
        Assert.IsTrue(File.Exists(buildFilePath), $"{buildFilePath} doesn't exist");
        var buildFile = File.ReadAllText(buildFilePath);
        Assert.IsTrue(buildFile.IndexOf($"packName = \"{androidPackName}\"") >= 0);
#if UNITY_ANDROID
        var deliveryTypeString = CustomAssetPackUtility.DeliveryTypeToGradleString(type);
        Assert.IsTrue(buildFile.IndexOf($"deliveryType = \"{deliveryTypeString}\"") >= 0);
#else
        throw new Exception("Active Editor Platform has to be Android");
#endif
    }

    void ValidateSeparateAssets(string path, int group)
    {
        var texturesFolder = Path.Combine(path, $"{GroupName(group).Item1}_assets_assets".ToLower(), kTestFolder.ToLower());
        Assert.IsTrue(Directory.Exists(texturesFolder));
        var textureFiles = Directory.GetFiles(texturesFolder);
        Assert.IsTrue(Array.Exists(textureFiles, p => Path.GetFileName(p).StartsWith(TextureName(group).ToLower())));
        Assert.IsTrue(Array.Exists(textureFiles, p => Path.GetFileName(p).StartsWith($"second_{TextureName(group)}".ToLower())));
    }

    protected void ValidateGroupsInBuildFolder(string buildPath)
    {
        if (TotalNumberOfGroups == 0)
        {
            return;
        }
        var bundleFiles = Directory.GetFiles(buildPath);
        for (int i = 0; i < TotalNumberOfGroups; ++i)
        {
            if (PackTogether(i))
            {
                Assert.IsTrue(Array.Exists(bundleFiles, p => Path.GetFileName(p).StartsWith($"{GroupName(i).Item1}_assets_all_".ToLower())));
            }
            else
            {
                ValidateSeparateAssets(buildPath, i);
            }
        }
        // this can be _unitybuiltinassets or _unitybuiltinshaders files, depending on addressables package version
        Assert.AreEqual(NumberOfGroups > 0, Array.Exists(bundleFiles, p => Path.GetFileName(p).IndexOf("unitybuiltin") >= 0));
    }

    void ValidateCatalog(string path, bool checkProvider, bool padProvider = false)
    {
        Assert.IsTrue(
            File.Exists(Path.Combine(path, "catalog.bin")) ||
            File.Exists(Path.Combine(path, "catalog.json")) ||
            File.Exists(Path.Combine(path, "catalog.bundle")),
            "Catalog file is missing");
        // checking for provider only inside json file
        if (checkProvider && File.Exists(Path.Combine(path, "catalog.json")))
        {
            var catalogFile = File.ReadAllText(Path.Combine(path, "catalog.json"));
            Assert.AreEqual(catalogFile.IndexOf("PlayAssetDeliveryAssetBundleProvider") >= 0, padProvider);
        }
    }

    protected void ValidateBuildFolderWithoutPAD(string platform)
    {
        Assert.IsFalse(Directory.Exists(CustomAssetPackUtility.BuildRootDirectory));
        ValidateCatalog(Addressables.BuildPath, true, false);
        Assert.IsTrue(File.Exists(Path.Combine(Addressables.BuildPath, "settings.json")));
        Assert.IsTrue(File.Exists(Path.Combine(Addressables.BuildPath, "AddressablesLink", "link.xml")));
        ValidateGroupsInBuildFolder(Path.Combine(Addressables.BuildPath, platform));
    }

    protected void ValidateBuildFolderWithTCFT(string[] postfixes)
    {
        foreach (var postfix in postfixes)
        {
            var jsonFilesPath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}");
            Assert.IsTrue(File.Exists(Path.Combine(jsonFilesPath, CustomAssetPackUtility.kBuildProcessorDataFilename)));
            Assert.IsTrue(File.Exists(Path.Combine(jsonFilesPath, CustomAssetPackUtility.kCustomAssetPackDataFilename)));

            if (postfixes.Length > 1 && postfix == postfixes[1])
            {
                // default variant, folder with postfix shouldn't exist
                Assert.IsFalse(Directory.Exists($"{Addressables.BuildPath}{postfix}"));
                continue;
            }
            var pathWithPostfix = $"{Addressables.BuildPath}{postfix}";
            var padProvider = AddressableAssetSettingsDefaultObject.Settings.groups.FindIndex(g => g.HasSchema<PlayAssetDeliverySchema>()) != -1;
            ValidateCatalog(pathWithPostfix, true, padProvider);
            Assert.IsTrue(File.Exists(Path.Combine(pathWithPostfix, "settings.json")));
            ValidateGroupsInBuildFolder(Path.Combine(pathWithPostfix, "Android"));
        }
        Assert.IsTrue(File.Exists(Path.Combine(CustomAssetPackUtility.BuildRootDirectory, "AddressablesLink", "link.xml")));
    }

    protected void ValidateGradleProjectWithAssetPacks(string gradleProject, string[] postfixes)
    {
        var settingsGradle = File.ReadAllText(Path.Combine(gradleProject, "settings.gradle"));
        var launcherGradle = File.ReadAllText(Path.Combine(gradleProject, "launcher", "build.gradle"));

        // ensure that AddressablesAssetPack is created (to store common data) even if there are no install-time groups
        ValidateAssetPack(CustomAssetPackUtility.kAddressablesAssetPackName, DeliveryType.InstallTime, gradleProject);
        Assert.IsTrue(settingsGradle.IndexOf($"include \':{CustomAssetPackUtility.kAddressablesAssetPackName}\'") >= 0);
        Assert.IsTrue(launcherGradle.IndexOf($"\":{CustomAssetPackUtility.kAddressablesAssetPackName}\"") >= 0);

        for (int i = 0; i < TotalNumberOfGroups; ++i)
        {
            var androidPackName = GroupName(i).Item2;
            var deliveryType = GenerateDeliveryType(i);
            if (string.IsNullOrEmpty(androidPackName))
            {
                androidPackName = "UnityStreamingAssetsPack";
                deliveryType = DeliveryType.InstallTime;
            }
            ValidateAssetPack(androidPackName, deliveryType, gradleProject);
            Assert.IsTrue(settingsGradle.IndexOf($"include \':{androidPackName}\'") >= 0);
            Assert.IsTrue(launcherGradle.IndexOf($"\":{androidPackName}\"") >= 0);

            string defaultBundle = "";
            foreach (var postfix in postfixes)
            {
                var assetsPath = Path.Combine(gradleProject, androidPackName, $"{CustomAssetPackUtility.CustomAssetPacksAssetsPath}{postfix}", "Android");
                if (PackTogether(i))
                {
                    var bundleFiles = Directory.GetFiles(assetsPath);
                    var bundleFile = Array.Find(bundleFiles, p => Path.GetFileName(p).StartsWith($"{GroupName(i).Item1}_assets_all_".ToLower()));
                    Assert.IsTrue(!string.IsNullOrEmpty(bundleFile));
                    if (postfix == "")
                    {
                        defaultBundle = Path.GetFileName(bundleFile);
                    }
                    else if (postfixes.Length > 1 && postfix == postfixes[1])
                    {
                        Assert.AreEqual(Path.GetFileName(bundleFile), defaultBundle);
                    }
                }
                else
                {
                    ValidateSeparateAssets(assetsPath, i);
                }
            }
        }
        foreach (var postfix in postfixes)
        {
            var jsonFilesPath = Path.Combine(gradleProject, CustomAssetPackUtility.kAddressablesAssetPackName, $"{CustomAssetPackUtility.CustomAssetPacksAssetsPath}{postfix}");
            ValidateCatalog(jsonFilesPath, false);
            Assert.IsTrue(File.Exists(Path.Combine(jsonFilesPath, "settings.json")));
            Assert.IsTrue(File.Exists(Path.Combine(jsonFilesPath, CustomAssetPackUtility.kCustomAssetPackDataFilename)));
            if (NumberOfGroups > 0)
            {
                var bundleFiles = Directory.GetFiles(Path.Combine(jsonFilesPath, "Android"));
                // this can be _unitybuiltinassets or _unitybuiltinshaders files, depending on addressables package version
                Assert.IsTrue(Array.Exists(bundleFiles, p => Path.GetFileName(p).IndexOf("unitybuiltin") >= 0));
            }
        }
    }

    protected void ValidateGradleProjectWithoutAssetPacks(string gradleProject, bool streamingAssets)
    {
        Assert.AreEqual(streamingAssets, Directory.Exists(Path.Combine(gradleProject, "UnityStreamingAssetsPack")));
        var aaPath = Path.Combine(gradleProject, streamingAssets ? "UnityStreamingAssetsPack" : "unityLibrary", "src/main/assets/aa/");
        ValidateCatalog(aaPath, false);
        Assert.IsTrue(File.Exists(Path.Combine(aaPath, "settings.json")));
        Assert.IsFalse(File.Exists(Path.Combine(aaPath, CustomAssetPackUtility.kCustomAssetPackDataFilename)));
        if (TotalNumberOfGroups == 0)
        {
            return;
        }
        var bundleFiles = Directory.GetFiles(Path.Combine(aaPath, "Android"));
        for (int i = 0; i < TotalNumberOfGroups; ++i)
        {
            var androidPackName = GroupName(i).Item2;
            if (!string.IsNullOrEmpty(androidPackName))
            {
                var assetPackFolderName = Path.Combine(gradleProject, androidPackName);
                Assert.IsFalse(Directory.Exists(assetPackFolderName));
            }
            if (PackTogether(i))
            {
                Assert.IsTrue(Array.Exists(bundleFiles, p => Path.GetFileName(p).StartsWith($"{GroupName(i).Item1}_assets_all_".ToLower())));
            }
            else
            {
                ValidateSeparateAssets(Path.Combine(aaPath, "Android"), i);
            }
        }
        // this can be _unitybuiltinassets or _unitybuiltinshaders files, depending on addressables package version
        Assert.AreEqual(NumberOfGroups > 0, Array.Exists(bundleFiles, p => Path.GetFileName(p).IndexOf("unitybuiltin") >= 0));
    }

    protected void ModifyGroupsBeforeBuild()
    {
        foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
        {
            if (!group.HasSchema<BundledAssetGroupSchema>() || !group.HasSchema<PlayAssetDeliverySchema>())
            {
                continue;
            }
            // check that Asset Bundle Provider is not set for PAD, force set Build and Load paths to Remote
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.AreEqual(typeof(AssetBundleProvider), schema.AssetBundleProviderType.Value);
            schema.BuildPath.SetVariableByName(group.Settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(group.Settings, AddressableAssetSettings.kRemoteLoadPath);
        }
    }

    protected void CheckGroupsAfterBuild()
    {
        foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
        {
            if (!group.HasSchema<BundledAssetGroupSchema>() || !group.HasSchema<PlayAssetDeliverySchema>())
            {
                continue;
            }
            // check that BundledAssetGroupSchema are restored after build
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.AreEqual(typeof(AssetBundleProvider), schema.AssetBundleProviderType.Value);
            Assert.AreEqual(AddressableAssetSettings.kRemoteBuildPath, schema.BuildPath.GetName(group.Settings));
            Assert.AreEqual(AddressableAssetSettings.kRemoteLoadPath, schema.LoadPath.GetName(group.Settings));
        }
    }

    protected void InitAddressables()
    {
        if (AddressableAssetSettingsDefaultObject.Settings != null && Directory.Exists(kSingleTestAssetFolder))
        {
            // already initialized
            return;
        }
        var testFolder = AssetDatabase.CreateFolder("Assets", kTestFolder);
        var emptyFolder = AssetDatabase.CreateFolder($"Assets/{kTestFolder}", "EmptyFolder");
        AddressableAssetSettingsDefaultObject.Settings =
            AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
            AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings) == -1)
        {
            settings.AddInitializationObject(CreateScriptAsset<PlayAssetDeliveryInitializationSettings>("InitObjects"));
        }
        if (settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery) == -1)
        {
            settings.AddDataBuilder(CreateScriptAsset<BuildScriptPlayAssetDelivery>("DataBuilders"));
        }

        for (int i = 0; i < NumberOfGroups; ++i)
        {
            var group = settings.CreateGroup(GroupName(i).Item1, true, false, false, null, typeof(BundledAssetGroupSchema), typeof(PlayAssetDeliverySchema));
            settings.CreateOrMoveEntry(CreateTexture(Path.Combine(kSingleTestAssetFolder, TextureName(i))), group, false, false);
            // adding second texture so it's possible to check addressing the same bundle second time
            settings.CreateOrMoveEntry(CreateTexture(Path.Combine(kSingleTestAssetFolder, $"second_{TextureName(i)}")), group, false, false);
            var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
            assetPackSchema.AssetPackDeliveryType = GenerateDeliveryType(i);
            if (i == 0)
            {
                // adding prefab object so unitybuiltinshaders/assets bundle can be generated
                var a = CreateAsset(Path.Combine(kSingleTestAssetFolder, "prefabWithMaterial.prefab"), "cube");
                settings.CreateOrMoveEntry(a, group, false, false);
            }
            if (!PackTogether(i))
            {
                var bundledAssetsSchema = group.GetSchema<BundledAssetGroupSchema>();
                bundledAssetsSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            }
        }
        var customAssetPacks = CustomAssetPackSettings.GetSettings(true);
        customAssetPacks.AddAssetPack(kCustomAssetPackName, GenerateDeliveryType(NumberOfGroups));
        AssetDatabase.SaveAssets();
        for (int i = 0; i < NumberOfUnitedGroups; ++i)
        {
            int index = i + NumberOfGroups;
            var group = settings.CreateGroup(GroupName(index).Item1, true, false, false, null, typeof(BundledAssetGroupSchema), typeof(PlayAssetDeliverySchema));
            settings.CreateOrMoveEntry(CreateTexture(Path.Combine(kSingleTestAssetFolder, TextureName(index))), group, false, false);
            var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
            // this type should be ignored, unless the custom asset pack with CustomAssetPackName doesn't exist
            assetPackSchema.AssetPackDeliveryType = DeliveryType.FastFollow;
            assetPackSchema.IncludeInCustomAssetPack = true;
            assetPackSchema.CustomAssetPackName = CustomAssetPackName(index);
        }

        //add empty folder to a group te ensure that the build does not fail
        var emptyFolderEntry = settings.CreateOrMoveEntry(emptyFolder, settings.groups[0]);
        Assert.NotNull(emptyFolderEntry);
    }

    protected void CleanupAddressables()
    {
        DeleteDirectoryFromAssets("ServerData");
        DeleteDirectoryFromAssets(Path.Combine("Assets", kTestFolder));
        DeleteDirectoryFromAssets(CustomAssetPackUtility.RootDirectory);
        DeleteDirectoryFromAssets(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);
        AssetDatabase.Refresh();
        if (Directory.Exists(kGradleProject))
        {
            Directory.Delete(kGradleProject, true);
        }
        if (File.Exists($"{kGradleProject}.aab"))
        {
            File.Delete($"{kGradleProject}.aab");
        }
        if (File.Exists($"{kGradleProject}.apk"))
        {
            File.Delete($"{kGradleProject}.apk");
        }
    }

    [SetUp]
    public void InitEditorTests()
    {
        InitAddressables();
    }

    [TearDown]
    public void CleanupEditorTests()
    {
        CleanupAddressables();
    }
#endif
}
