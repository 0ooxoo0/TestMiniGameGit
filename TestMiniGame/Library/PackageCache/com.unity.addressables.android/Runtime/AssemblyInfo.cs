using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCompany("Unity Technologies")]

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Addressables.Android.Editor")]
[assembly: InternalsVisibleTo("Unity.Addressables.Android.Editor.Tests")]
#endif
[assembly: InternalsVisibleTo("Unity.Addressables.Android.Base.Tests")]
[assembly: InternalsVisibleTo("Unity.Addressables.Android.Tests")]
