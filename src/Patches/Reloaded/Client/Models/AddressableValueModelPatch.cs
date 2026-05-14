using HarmonyLib;
using Il2CppReloaded.DataModels;
using ReplantedOnline.Data.Asset;

namespace ReplantedOnline.Patches.Reloaded.Client.Models;

[HarmonyPatch]
internal static class AddressableValueModelPatch
{
    [HarmonyPatch(typeof(AddressableSpriteValueModel), nameof(AddressableSpriteValueModel.LoadSpriteReference))]
    [HarmonyPrefix]
    private static bool AddressableSpriteValueModel_LoadSpriteReference_Prefix(AddressableSpriteValueModel __instance)
    {
        // Load custom sprite references
        if (CustomAssetReference.IsValid(__instance.Reference))
        {
            var operation = __instance.Reference.LoadAssetAsync();
            if (operation.IsValid())
            {
                __instance.OnSpriteLoaded(operation);
            }
            __instance.Reference.ReleaseAsset();

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(AddressablePrefabValueModel), nameof(AddressablePrefabValueModel.LoadPrefabReference))]
    [HarmonyPrefix]
    private static bool AddressablePrefabValueModel_LoadPrefabReference_Prefix(AddressablePrefabValueModel __instance)
    {
        // Load custom sprite references
        if (CustomAssetReference.IsValid(__instance.Reference))
        {
            var operation = __instance.Reference.LoadAssetAsync();
            if (operation.IsValid())
            {
                __instance.OnPrefabLoaded(operation);
            }
            __instance.Reference.ReleaseAsset();

            return false;
        }

        return true;
    }
}
