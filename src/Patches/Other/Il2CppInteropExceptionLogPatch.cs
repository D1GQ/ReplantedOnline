using MelonLoader;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Utilities;
using System.Reflection;

namespace ReplantedOnline.Patches.Other;

internal static class Il2CppInteropExceptionLogPatch
{
    // Store MelonLoader's internal Il2CppInterop logger
    private static readonly MelonLogger.Instance _logger = GetIl2CppInteropLogger();

    private static MelonLogger.Instance GetIl2CppInteropLogger()
    {
        try
        {
            var exceptionLogType = Type.GetType("MelonLoader.Fixes.Il2CppInteropExceptionLog, MelonLoader");

            if (exceptionLogType == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find Il2CppInteropExceptionLog type");
                return null;
            }

            var loggerField = exceptionLogType.GetField("_logger",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (loggerField == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find _logger field");
                return null;
            }

            var logger = loggerField.GetValue(null);

            if (logger == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Logger field was null");
                return null;
            }

            return (MelonLogger.Instance)logger;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), $"Failed to get Il2CppInterop logger: {ex}");
            return null;
        }
    }

    internal static void Patch(HarmonyLib.Harmony Harmony)
    {
        // First remove MelonLoader's patch, then add our own
        UnpatchMelonLoaderExceptionLog(Harmony);
        InstallExceptionLog(Harmony);
    }

    private static void UnpatchMelonLoaderExceptionLog(HarmonyLib.Harmony Harmony)
    {
        try
        {
            // Find the Il2CppInterop.HarmonySupport assembly that contains the original method
            var harmonySupportAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.HarmonySupport");

            if (harmonySupportAssembly == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find Il2CppInterop.HarmonySupport assembly");
                return;
            }

            // Get the type that contains the original ReportException method
            var detourMethodPatcherType = harmonySupportAssembly.GetType("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher");
            if (detourMethodPatcherType == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find Il2CppDetourMethodPatcher type");
                return;
            }

            // Get the original ReportException method
            var reportException = detourMethodPatcherType.GetMethod("ReportException",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (reportException == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find ReportException method");
                return;
            }

            // Find MelonLoader's patch method that they applied to ReportException
            var melonPatch = HarmonyLib.AccessTools.Method("MelonLoader.Fixes.Il2CppInteropExceptionLog:ReportException_Prefix");

            if (melonPatch == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), "Could not find MelonLoader's ReportException_Prefix");
                return;
            }

            // Remove MelonLoader's patch from the original method
            Harmony.Unpatch(reportException, melonPatch);
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), $"Failed to unpatch MelonLoader exception log: {ex}");
        }
    }

    private static void InstallExceptionLog(HarmonyLib.Harmony harmony)
    {
        try
        {
            var harmonySupportAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.HarmonySupport");

            var detourMethodPatcherType = harmonySupportAssembly.GetType("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher");

            var reportException = detourMethodPatcherType.GetMethod("ReportException",
                BindingFlags.NonPublic | BindingFlags.Static);

            var ourPrefix = typeof(Il2CppInteropExceptionLogPatch).GetMethod(nameof(OurReportException_Prefix),
                BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(reportException, new HarmonyLib.HarmonyMethod(ourPrefix));

        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(Il2CppInteropExceptionLogPatch), $"Failed to install our exception log: {ex}");
        }
    }

    private static bool OurReportException_Prefix(Exception __0)
    {
        // If it's SilentException, do nothing (suppress the log)
        if (__0 is SilentPatchException)
        {
            return false;
        }

        _logger.Error("During invoking native->managed trampoline", __0);

        return false;
    }
}