using MelonLoader;
using ReplantedOnline.Exceptions;
using System.Reflection;

namespace ReplantedOnline.Patches;

internal static class Il2CppInteropExceptionLogPatch
{
    // Store MelonLoader's internal Il2CppInterop logger so we can use it later
    private static MelonLogger.Instance _logger = GetIl2CppInteropLogger();

    private static MelonLogger.Instance GetIl2CppInteropLogger()
    {
        try
        {
            // Get the type by its full name including assembly
            var exceptionLogType = Type.GetType("MelonLoader.Fixes.Il2CppInteropExceptionLog, MelonLoader");

            if (exceptionLogType == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find Il2CppInteropExceptionLog type");
                return null;
            }

            // Get the private static field _logger from that type
            var loggerField = exceptionLogType.GetField("_logger",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (loggerField == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find _logger field");
                return null;
            }

            // Get the actual value of the static field (null means static field)
            var logger = loggerField.GetValue(null);

            if (logger == null)
            {
                ReplantedOnlineMod.Logger.Error("Logger field was null");
                return null;
            }

            // Cast it to the type we can use
            return (MelonLogger.Instance)logger;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"Failed to get Il2CppInterop logger: {ex}");
            return null;
        }
    }

    // Main entry point - called from our mod to apply all patches
    internal static void Patch(HarmonyLib.Harmony Harmony)
    {
        // First remove MelonLoader's patch, then add our own
        UnpatchMelonLoaderExceptionLog(Harmony);
        InstallOurExceptionLog(Harmony);
    }

    // Remove MelonLoader's existing patch that logs all exceptions
    private static void UnpatchMelonLoaderExceptionLog(HarmonyLib.Harmony Harmony)
    {
        try
        {
            // Find the Il2CppInterop.HarmonySupport assembly that contains the original method
            // This is already loaded by MelonLoader, we just need to find it
            var harmonySupportAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.HarmonySupport");

            if (harmonySupportAssembly == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find Il2CppInterop.HarmonySupport assembly");
                return;
            }

            // Get the type that contains the original ReportException method
            var detourMethodPatcherType = harmonySupportAssembly.GetType("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher");
            if (detourMethodPatcherType == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find Il2CppDetourMethodPatcher type");
                return;
            }

            // Get the original ReportException method (the one that catches exceptions from native->managed calls)
            var reportException = detourMethodPatcherType.GetMethod("ReportException",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (reportException == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find ReportException method");
                return;
            }

            // Find MelonLoader's patch method that they applied to ReportException
            // This is the prefix that logs "During invoking native->managed trampoline"
            var melonPatch = HarmonyLib.AccessTools.Method("MelonLoader.Fixes.Il2CppInteropExceptionLog:ReportException_Prefix");

            if (melonPatch == null)
            {
                ReplantedOnlineMod.Logger.Error("Could not find MelonLoader's ReportException_Prefix");
                return;
            }

            // Remove MelonLoader's patch from the original method
            // Now ReportException will do nothing when called
            Harmony.Unpatch(reportException, melonPatch);
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"Failed to unpatch MelonLoader exception log: {ex}");
        }
    }

    // Add our own filtered version of the exception logging
    private static void InstallOurExceptionLog(HarmonyLib.Harmony harmony)
    {
        try
        {
            // Find the same assembly and type as above
            var harmonySupportAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.HarmonySupport");

            var detourMethodPatcherType = harmonySupportAssembly.GetType("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher");

            // Get the same original ReportException method
            var reportException = detourMethodPatcherType.GetMethod("ReportException",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Get our own prefix method that will run instead of MelonLoader's
            var ourPrefix = typeof(Il2CppInteropExceptionLogPatch).GetMethod(nameof(OurReportException_Prefix),
                BindingFlags.NonPublic | BindingFlags.Static);

            // Apply our patch - now our method will be called when ReportException fires
            harmony.Patch(reportException, new HarmonyLib.HarmonyMethod(ourPrefix));

        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"Failed to install our exception log: {ex}");
        }
    }

    // Our replacement for MelonLoader's logging prefix
    // This runs when an exception occurs in a native->managed trampoline
    private static bool OurReportException_Prefix(Exception __0)
    {
        // If it's our control-flow SilentException, do nothing (suppress the log)
        if (__0 is SilentPatchException)
        {
            return false;
        }

        // For any other exception, log it using MelonLoader's own logger
        // This maintains the original behavior for non-SilentExceptions
        _logger.Error("During invoking native->managed trampoline", __0);

        return false;
    }
}