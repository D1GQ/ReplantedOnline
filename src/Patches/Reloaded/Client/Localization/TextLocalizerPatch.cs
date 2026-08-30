using HarmonyLib;
using Il2CppTekly.Localizations;
using ReplantedOnline.Managers.Modded;

namespace ReplantedOnline.Patches.Reloaded.Client.Localization;

[HarmonyPatch]
internal static class TextLocalizerPatch
{
    [HarmonyPatch(typeof(TextLocalizer), nameof(TextLocalizer.LocalizeText))]
    [HarmonyPrefix]
    private static bool FormattedLocalizationIdBinder_BindValue_Prefix(TextLocalizer __instance)
    {
        if (string.IsNullOrEmpty(__instance.Id))
        {
            if (__instance?.Text != null)
            {
                __instance.Text.SetText(string.Empty);
            }

            return false;
        }

        if (LocalizationManager.TryGetLocalization(__instance.Id, out var localization))
        {
            __instance.Text.SetText(localization);
            return false;
        }

        return true;
    }
}
