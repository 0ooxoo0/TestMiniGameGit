---
uid: addressables-for-android-runtime
---

# Manage asset packs at runtime


You can use Addressables content packed into asset packs without modifying any Player code. Also, you need not remove [AssetBundle cache](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/remote-content-assetbundle-cache.html) because the Android system deletes obsolete asset packs from the device when the application updates.


## Load and remove asset packs


When you [load Addressable assets](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/load-assets.html) that are packed into **On Demand** asset packs, the Player tries to install the required asset packs from Google Play. Depending on the network connectivity and the asset pack size, there might be a significant delay before the Addressable asset can be instantiated and displayed. To avoid this delay, you can force the required asset pack to load beforehand using [AndroidAssetPacks.DownloadAssetPackAsync](https://docs.unity3d.com/ScriptReference/Android.AndroidAssetPacks.DownloadAssetPackAsync.html) method. If you do not need any **Fast Follow** or **On Demand** asset packs anymore, you can forcefully remove those from the device using [AndroidAssetPacks.RemoveAssetPack](https://docs.unity3d.com/ScriptReference/Android.AndroidAssetPacks.RemoveAssetPack.html) method. Please note that, if the Player tries to load an already removed asset pack, the Android system automatically installs the same asset pack again on the device.


>[!Note]
> To use [AndroidAssetPacks](https://docs.unity3d.com/ScriptReference/Android.AndroidAssetPacks.html) methods to manually manage asset packs, you must know asset pack names for Addressable groups. The easiest way is to name your groups (or [Custom Asset Packs](custom-asset-packs-settings.md)) to comply with the asset pack name requirement by Google. Google’s requirement states that the asset pack names must start with a letter and can only contain letters, numbers, and underscores. If you name your Addressable group (or custom asset pack) to comply with Google's requirement, the Addressables for Android  package uses the same group name for the asset pack. Otherwise, the package automatically generates asset pack names by removing forbidden characters. If required, the package may add **Group** prefix and some numeric postfix. In this case, you can find the actual names inside the generated App Bundle or the exported Gradle project.
