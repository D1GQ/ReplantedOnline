using HarmonyLib;
using ReplantedOnline.Data.Asset;
using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Patches.Unity;

[HarmonyPatch]
internal static class AssetReferencePatch
{
    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
    [HarmonyPostfix]
    private static void AssetReference_RuntimeKeyIsValid_Postfix(AssetReference __instance, ref bool __result)
    {
        if (CustomAssetReference.IsValid(__instance))
        {
            __result = true;
        }
    }
}
