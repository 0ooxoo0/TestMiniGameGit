---
uid: addressables-for-android-texture-compression-targeting
---

# Use Texture Compression Targeting support

If your application uses [Texture Compression Targeting](https://docs.unity3d.com/2023.2/Documentation/Manual/android-distribution-google-play.html#texture-compression-targeting), then the same Android **Texture Compression Formats** settings is applied when you build
* Addressables content using Play Asset Delivery Addressables build script or
* Addressables content while building Android Player.

Built content (including catalog files) is generated for all required texture variants and files are placed to specific subfolders in asset packs folders of the Android Gradle project. Similar to Texture Compression Targeting support in Unity, first texture compression in the list is treated as [default](https://developer.android.com/guide/playcore/asset-delivery/texture-compression#select-default-format).

Addressables groups which don't have Play Asset Delivery schema and for which Build and Load Paths are set to Remote are generated in the folder for remote assets for all texture compressions variants as well.

As the catalog and settings json files are texture compression targeted and are packed into `AddressablesAssetPack`, when the application is running on the device, it uses catalog and settings json files for the texture compression required for this device. This means that Addressables content from the remote server is downloaded for the same texture compression target. For this to work correctly, set **Bundle Naming Mode** in Content Packing & Loading schema to **Append Hash to Filename** or **Use Hash of AssetBundle** to ensure that files generated for different texture compression settings have different names.

>[!NOTE]
>[Texture Compression Targeting](https://docs.unity3d.com/Documentation/Manual/android-distribution-google-play.html#texture-compression-targeting) works only when building Android App Bundles (AAB) or Gradle project for AAB. When building an APK (or Gradle project for APK) only default texture compression is used.
