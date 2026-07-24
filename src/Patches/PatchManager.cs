using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Patches.Misc;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Patches;

/// <summary>
/// Manages Harmony patches.
/// </summary>
internal static class PatchManager
{
    private static readonly HarmonyLib.Harmony harmony = new(ReplantedOnlineMod.ModInfo.MOD_GUID);

    /// <summary>
    /// Applies all initial Harmony patches.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all patches were applied successfully; 
    /// <see langword="false"/> if any patch failed to apply.
    /// </returns>
    internal static bool PatchAll()
    {
        try
        {
            harmony.PatchAll();
            DebugLoggerPatch.Patch(harmony);
            Il2CppInteropExceptionLogPatch.Patch(harmony);
            DetourHookAttribute.InstallAll();
            NativeDetourHook.InstallAll();
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.BigError(typeof(PatchManager), ex.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies late stage patches that should be applied after other mods have initialized.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all late patches were applied successfully;
    /// <see langword="false"/> if any patch failed to apply.
    /// </returns>
    internal static bool PatchAllLate()
    {
        try
        {
            UniverseLibPatch.Patch(harmony);
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.BigError(typeof(PatchManager), ex.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Updates all patchs.
    /// </summary>
    internal static void UpdatePatchs()
    {
        SeedChooserPatch.UpdateSeedChooserScreen(Instances.GameplayActivity?.SeedChooserScreen);
    }
}