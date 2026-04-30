using MelonLoader;
using ReplantedOnline;
using System.Reflection;

[assembly: AssemblyTitle(nameof(ReplantedOnline))]
[assembly: AssemblyProduct(nameof(ReplantedOnline))]
[assembly: AssemblyVersion(ModInfo.MOD_VERSION)]
[assembly: AssemblyFileVersion(ModInfo.MOD_VERSION)]

[assembly: MelonInfo(typeof(ReplantedOnlineMod), ModInfo.MOD_NAME, ModInfo.MOD_VERSION_FORMATTED, ModInfo.CREATOR, ModInfo.GITHUB)]
[assembly: MelonGame(ModInfo.PVZR.COMPANY, ModInfo.PVZR.GAME)]
[assembly: MelonAdditionalDependencies(ModInfo.BloomEngine.BLOOM_ENGINE_NAME)]
