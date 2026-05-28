using HarmonyLib;
using Il2CppSteamworks;
using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Managers.Modded;

namespace ReplantedOnline.Patches.Steam;

[HarmonyPatch]
internal static class SteamClientPatch
{
    private static uint _currentAppId;
    private static bool _isShuttingDown = false;

    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.Init))]
    [HarmonyPrefix]
    private static bool SteamClient_Init_Prefix()
    {
        SetApp(BloomEngineManager.BloomConfigs.AppServerConfig.Value);
        return false;
    }

    /// <summary>
    /// Sets the Steam App ID, handles shutdown of existing Steam client, environment variables, and steam_appid.txt creation.
    /// </summary>
    /// <param name="appId">The target App ID to set for Steam.</param>
    internal static void SetApp(AppIds appId)
    {
        SetApp(appId, 0);
    }

    /// <summary>
    /// Sets the Steam App ID, handles shutdown of existing Steam client, environment variables, and steam_appid.txt creation.
    /// </summary>
    /// <param name="appId">The target App ID to set for Steam.</param>
    /// <param name="attempt">Current retry attempt number, recursive up to 100 times.</param>
    private static void SetApp(AppIds appId, int attempt)
    {
        if (attempt >= 100)
        {
            return;
        }

        try
        {
            if (appId == AppIds.Spacewar)
            {
                appId = (AppIds)480;
            }

            _currentAppId = (uint)appId;

            if (SteamClient.initialized && !_isShuttingDown)
            {
                _isShuttingDown = true;
                SteamClient.Shutdown();
                _isShuttingDown = false;
            }

            Environment.SetEnvironmentVariable("SteamAppId", _currentAppId.ToString());
            Environment.SetEnvironmentVariable("SteamGameId", _currentAppId.ToString());
            File.WriteAllText("steam_appid.txt", _currentAppId.ToString());
            InitOriginal(_currentAppId, true);
        }
        catch
        {
            SetApp(appId, ++attempt);
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.Init))]
    private static void InitOriginal(uint appid, bool asyncCallbacks = true)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.AppId), MethodType.Getter)]
    [HarmonyPostfix]
    private static void SteamClient_AppId_Getter_Postfix(ref AppId __result)
    {
        if (__result != _currentAppId && SteamClient.initialized)
        {
            __result = _currentAppId;
        }
    }

    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.AppId), MethodType.Setter)]
    [HarmonyPrefix]
    private static void SteamClient_AppId_Setter_Prefix(ref AppId value)
    {
        if (value != _currentAppId && SteamClient.initialized)
        {
            value = _currentAppId;
        }
    }

    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.RestartAppIfNecessary))]
    [HarmonyPrefix]
    private static void SteamClient_RestartAppIfNecessary_Prefix(ref uint appid, ref bool __result)
    {
        appid = _currentAppId;
    }
}