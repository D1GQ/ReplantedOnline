using HarmonyLib;
using Il2CppReloaded.Binders;
using ReplantedOnline.Managers.Modded;

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

        return true;
    }
}
