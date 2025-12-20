using MelonLoader;
using ReplantedOnline;
using System.Reflection;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: MelonInfo(typeof(ReplantedOnlineMod), ModInfo.MOD_NAME, $"{ModInfo.MOD_VERSION}-{ModInfo.MOD_RELEASE}-{ModInfo.RELEASE_DATE}", ModInfo.CREATOR, ModInfo.GITHUB)]
[assembly: MelonGame(ModInfo.PVZR.COMPANY, ModInfo.PVZR.GAME)]
[assembly: MelonAdditionalDependencies(ModInfo.BloomEngine.BLOOM_ENGINE_DEPENDENCY)]
[assembly: HarmonyDontPatchAll]
[assembly: AssemblyTitle(nameof(ReplantedOnline))]
[assembly: AssemblyProduct(nameof(ReplantedOnline))]
[assembly: AssemblyVersion(ModInfo.MOD_VERSION)]
[assembly: AssemblyFileVersion(ModInfo.MOD_VERSION)]
