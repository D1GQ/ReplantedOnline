using HarmonyLib;

namespace ReplantedOnline.Patches;

[HarmonyPatch]
internal static class UniverseLibPatch
{
    internal static void Patch()
    {
        // Fix constant "Memory Access Violations" on older versions of Unity Explorer!
        var loadMethod = AccessTools.Method("UniverseLib.AssetBundle:LoadFromMemory", [typeof(byte[]), typeof(uint)]);

        var prefixMethod = AccessTools.Method(typeof(UniverseLibPatch), nameof(AssetBundle_LoadFromMemory_Prefix));

        ReplantedOnlineMod.harmony.Patch(loadMethod, prefix: new HarmonyMethod(prefixMethod));
    }

    private static bool AssetBundle_LoadFromMemory_Prefix(byte[] binary, uint crc, ref object __result)
    {
        ReplantedOnlineMod.Logger.Warning("Interrupting UniverseLib asset bundle load, DO NOT REPORT WARNING BELOW!!!");
        __result = null;
        return false;
    }
}