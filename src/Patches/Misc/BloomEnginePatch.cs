using BloomEngine.Config;
using BloomEngine.Config.Inputs.Base;
using BloomEngine.ModMenu;
using HarmonyLib;
using Il2CppTMPro;
using ReplantedOnline.Managers.Modded;
using UnityEngine;

namespace ReplantedOnline.Patches.Misc;

[HarmonyPatch]
internal static class BloomEnginePatch
{
    internal static void Patch(HarmonyLib.Harmony harmony)
    {
        var setupHeaderMethod = AccessTools.Method(AccessTools.TypeByName("BloomEngine.Config.UI.ConfigPanel"), "SetupHeader");

        var setupHeaderPostfix = AccessTools.Method(typeof(BloomEnginePatch), nameof(ConfigPanel_SetupHeader_Postfix));

        harmony.Patch(setupHeaderMethod, postfix: new HarmonyMethod(setupHeaderPostfix));
    }

    private static void ConfigPanel_SetupHeader_Postfix(object __instance)
    {
        var windowField = AccessTools.Field(__instance.GetType(), "window");
        var window = windowField?.GetValue(__instance) as RectTransform;
        var configField = AccessTools.Field(__instance.GetType(), "config");
        var config = configField?.GetValue(__instance) as ModConfig;
        if (window != null && config != null)
        {
            string displayNameLID = config.GetDisplayNameOriginal();

            if (LocalizationManager.TryGetLocalization(displayNameLID + "_POSTFIX", out var localized))
            {
                var header = window.Find("HeaderText").GetComponent<TextMeshProUGUI>();
                header.text = localized;
            }
        }
    }

    [HarmonyPatch(typeof(BaseConfigInput), nameof(BaseConfigInput.Name), MethodType.Getter)]
    [HarmonyPatch(typeof(BaseConfigInput), nameof(BaseConfigInput.Description), MethodType.Getter)]
    [HarmonyPatch(typeof(ModMenuEntry), nameof(ModMenuEntry.DisplayName), MethodType.Getter)]
    [HarmonyPatch(typeof(ModMenuEntry), nameof(ModMenuEntry.Description), MethodType.Getter)]
    [HarmonyPatch(typeof(ModConfig), nameof(ModConfig.DisplayName), MethodType.Getter)]
    [HarmonyPostfix]
    private static void BloomEngine_Localization_Postfix(ref string __result)
    {
        if (LocalizationManager.TryGetLocalization(__result, out var localized))
        {
            __result = localized;
        }
    }

    [HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.AddOptions), [typeof(Il2CppSystem.Collections.Generic.List<string>)])]
    [HarmonyPrefix]
    private static void TMP_Dropdown_AddOptions_Prefix(Il2CppSystem.Collections.Generic.List<string> options)
    {
        if (options == null) return;

        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (LocalizationManager.TryGetLocalization("$MOD_CONFIG_DROPDOWN_" + option.ToUpper(), out var localized))
            {
                options[i] = localized;
            }
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModConfig), nameof(ModConfig.DisplayName), MethodType.Getter)]
    private static string GetDisplayNameOriginal(this ModConfig __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}