using System;
using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// Build processor information required to copy specific Addressables bundle file into Android asset pack inside of the Gradle project.
    /// </summary>
    [Serializable]
    public class BuildProcessorDataEntry
    {
        /// <summary>
        /// Path to the Addressable bundle file.
        /// </summary>
        public string BundleBuildPath;

        /// <summary>
        /// Target path inside the Gradle project.
        /// </summary>
        public string AssetPackPath;

        /// <summary>
        /// Create a new BuildProcessorDataEntry object.
        /// </summary>
        /// <param name="bundleBuildPath">Path to the Addressable bundle file.</param>
        /// <param name="assetPackPath">Target path inside the Gradle project.</param>
        public BuildProcessorDataEntry(string bundleBuildPath, string assetPackPath)
        {
            BundleBuildPath = bundleBuildPath;
            AssetPackPath = assetPackPath;
        }
    }

    /// <summary>
    /// A storage class used to gather information required to copy Addressable bundle files into Android asset packs inside the Gradle project.
    /// </summary>
    [Serializable]
    public class BuildProcessorData
    {
        /// <summary>
        /// The List of BuildProcessorDataEntry entries.
        /// </summary>
        public List<BuildProcessorDataEntry> Entries;

        /// <summary>
        /// Create a new BuildProcessorData object.
        /// </summary>
        /// <param name="entries">The List of BuildProcessorDataEntry entries.</param>
        public BuildProcessorData(IEnumerable<BuildProcessorDataEntry> entries)
        {
            Entries = new List<BuildProcessorDataEntry>(entries);
        }
    }
}
