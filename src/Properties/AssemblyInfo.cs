using MelonLoader;
using ReplantedOnline;
using System.Reflection;

[assembly: AssemblyTitle(nameof(ReplantedOnline))]
[assembly: AssemblyProduct(nameof(ReplantedOnline))]
[assembly: AssemblyVersion(ReplantedOnlineMod.ModInfo.MOD_VERSION)]
[assembly: AssemblyFileVersion(ReplantedOnlineMod.ModInfo.MOD_VERSION)]

[assembly: MelonInfo(typeof(ReplantedOnlineMod), ReplantedOnlineMod.ModInfo.MOD_NAME, ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED, ReplantedOnlineMod.ModInfo.CREATOR, ReplantedOnlineMod.ModInfo.GITHUB)]
[assembly: MelonGame(ReplantedOnlineMod.ModInfo.Replanted.COMPANY, ReplantedOnlineMod.ModInfo.Replanted.GAME)]
[assembly: MelonColor(ReplantedOnlineMod.ModInfo.ModColor.A, ReplantedOnlineMod.ModInfo.ModColor.R, ReplantedOnlineMod.ModInfo.ModColor.G, ReplantedOnlineMod.ModInfo.ModColor.B)]
[assembly: MelonAdditionalDependencies(ReplantedOnlineMod.ModInfo.BloomEngine.BLOOM_ENGINE_NAME)]
[assembly: MelonOptionalDependencies(ReplantedOnlineMod.ModInfo.DiscordRPC.DISCORD_RPC_NAME)]
[assembly: HarmonyDontPatchAll]