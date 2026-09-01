using HarmonyLib;
using Il2CppReloaded.Binders;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Patches.Reloaded.Client.Localization;

[HarmonyPatch]
internal static class FormattedLocalizationIdBinderPatch
{
    [HarmonyPatch(typeof(FormattedLocalizationIdBinder), nameof(FormattedLocalizationIdBinder.BindValue))]
    [HarmonyPrefix]
    private static bool FormattedLocalizationIdBinder_BindValue_Prefix(FormattedLocalizationIdBinder __instance, string value)
    {
        if (LocalizationManager.TryGetLocalization(value, out var localization))
        {
            __instance.m_text.SetText(localization);
            return false;
        }

        if (value.StartsWith(ReplantedOnlineMod.Constants.Reloaded.REDIRECT_ALMANAC_PREFIX))
        {
            var customSeedTypeIntString = value[ReplantedOnlineMod.Constants.Reloaded.REDIRECT_ALMANAC_PREFIX.Length..];
            var customSeedType = (CustomSeedType)(SeedType)int.Parse(customSeedTypeIntString);

            if (CustomPlantDefinition.TryGetAlmanacDescription(customSeedType, out var description))
            {
                __instance.m_text.SetText(description);
                return false;
            }
        }

        return true;
    }
}
