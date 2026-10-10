using BloomEngine.Config;
using BloomEngine.Config.Inputs.Base;
using BloomEngine.ModMenu;
using HarmonyLib;
using Il2CppTMPro;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Utilities.MelonLoader;
using UnityEngine;

namespace ReplantedOnline.Patches.Misc;

[HarmonyPatch]
internal static class BloomEnginePatch
{
    internal static void Patch(HarmonyLib.Harmony harmony)
    {
        try
        {
            var configPanelType = AccessTools.TypeByName("BloomEngine.Config.UI.ConfigPanel");
            if (configPanelType == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                    "Could not find type BloomEngine.Config.UI.ConfigPanel");
                return;
            }

            var setupHeaderMethod = AccessTools.Method(configPanelType, "SetupHeader");
            if (setupHeaderMethod == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                    "Could not find method ConfigPanel.SetupHeader");
                return;
            }

            var setupHeaderPostfix = AccessTools.Method(typeof(BloomEnginePatch), nameof(ConfigPanel_SetupHeader_Postfix));
            harmony.Patch(setupHeaderMethod, postfix: new HarmonyMethod(setupHeaderPostfix));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                $"Failed to patch BloomEngine: {ex}");
        }
    }

    private static void ConfigPanel_SetupHeader_Postfix(object __instance)
    {
        try
        {
            var type = __instance.GetType();

            var windowField = AccessTools.Field(type, "window");
            if (windowField == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                    $"Field 'window' not found on {type.FullName}");
                return;
            }

            var configField = AccessTools.Field(type, "config");
            if (configField == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                    $"Field 'config' not found on {type.FullName}");
                return;
            }

            var window = windowField.GetValue(__instance) as RectTransform;
            var config = configField.GetValue(__instance) as ModConfig;

            if (window == null || config == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                    $"window or config was null (window={window != null}, config={config != null})");
                return;
            }

            string displayNameLID = config.GetDisplayNameOriginal();

            if (LocalizationManager.TryGetLocalization(displayNameLID + "_POSTFIX", out var localized))
            {
                var headerTransform = window.Find("HeaderText");
                if (headerTransform == null)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                        "Could not find 'HeaderText' under window");
                    return;
                }

                var header = headerTransform.GetComponent<TextMeshProUGUI>();
                if (header == null)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                        "'HeaderText' has no TextMeshProUGUI component");
                    return;
                }

                header.text = localized;
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch), $"Exception in ConfigPanel_SetupHeader_Postfix: {ex}");
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
        try
        {
            if (string.IsNullOrEmpty(__result)) return;

            if (LocalizationManager.TryGetLocalization(__result, out var localized))
            {
                __result = localized;
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch),
                $"Exception in BloomEngine_Localization_Postfix: {ex}");
        }
    }

    [HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.AddOptions), [typeof(Il2CppSystem.Collections.Generic.List<string>)])]
    [HarmonyPrefix]
    private static void TMP_Dropdown_AddOptions_Prefix(Il2CppSystem.Collections.Generic.List<string> options)
    {
        try
        {
            if (options == null) return;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (string.IsNullOrEmpty(option)) continue;

                if (LocalizationManager.TryGetLocalization("$MOD_CONFIG_DROPDOWN_" + option.ToUpper(), out var localized))
                {
                    options[i] = localized;
                }
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(BloomEnginePatch), $"Exception in TMP_Dropdown_AddOptions_Prefix: {ex}");
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModConfig), nameof(ModConfig.DisplayName), MethodType.Getter)]
    private static string GetDisplayNameOriginal(this ModConfig __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}