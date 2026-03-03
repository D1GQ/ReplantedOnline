using HarmonyLib;
using Il2CppSteamworks;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Patches.Steam;

[HarmonyPatch]
internal static class MatchmakingPatch
{
    /// <summary>
    /// Bans the specified player from the current Steam lobby if the caller is the lobby host.
    /// </summary>
    internal static void Ban(this ID clientId)
    {
        if (NetLobby.AmInLobby() && NetLobby.AmLobbyHost())
        {
            NetLobby.NetworkTransport.SetLobbyData(NetLobby.LobbyData.LobbyId, $"ban:{clientId}", bool.TrueString);
        }
    }

    /// <summary>
    /// Determines whether the specified player is marked as banned in the current Steam lobby.
    /// </summary>
    /// <returns>true if the player is banned in the current lobby; otherwise, false.</returns>
    internal static bool IsBanned(this ID clientId)
    {
        if (NetLobby.AmInLobby())
        {
            return NetLobby.NetworkTransport.GetLobbyData(NetLobby.LobbyData.LobbyId, $"ban:{clientId}") == bool.TrueString;
        }

        return false;
    }

    private static List<SteamId> GetFilteredMembers(SteamId lobbyId)
    {
        int realCount = SteamMatchmaking.Internal.GetNumLobbyMembersOriginal(lobbyId);
        var filtered = new List<SteamId>();

        for (int i = 0; i < realCount; i++)
        {
            SteamId memberId = SteamMatchmaking.Internal.GetLobbyMemberByIndexOriginal(lobbyId, i);
            if (!IsBanned(memberId))
            {
                filtered.Add(memberId);
            }
        }

        return filtered;
    }

    [HarmonyPatch(typeof(ISteamMatchmaking), nameof(ISteamMatchmaking.GetNumLobbyMembers))]
    [HarmonyPrefix]
    private static bool GetNumLobbyMembers_Prefix(SteamId steamIDLobby, ref int __result)
    {
        if (!NetLobby.AmInLobby() || NetLobby.LobbyData.LobbyId != steamIDLobby) return true;

        var filtered = GetFilteredMembers(steamIDLobby);
        __result = filtered.Count;

        return false;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ISteamMatchmaking), nameof(ISteamMatchmaking.GetNumLobbyMembers))]
    internal static int GetNumLobbyMembersOriginal(this ISteamMatchmaking __instance, SteamId steamIDLobby)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(ISteamMatchmaking), nameof(ISteamMatchmaking.GetLobbyMemberByIndex))]
    [HarmonyPrefix]
    private static bool GetLobbyMemberByIndex_Prefix(SteamId steamIDLobby, int iMember, ref SteamId __result)
    {
        if (!NetLobby.AmInLobby() || NetLobby.LobbyData.LobbyId != steamIDLobby) return true;

        var filtered = GetFilteredMembers(steamIDLobby);

        if (iMember >= 0 && iMember < filtered.Count)
        {
            __result = filtered[iMember];
            return false;
        }

        __result = default;
        return false;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ISteamMatchmaking), nameof(ISteamMatchmaking.GetLobbyMemberByIndex))]
    internal static SteamId GetLobbyMemberByIndexOriginal(this ISteamMatchmaking __instance, SteamId steamIDLobby, int iMember)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(ISteamMatchmaking), nameof(ISteamMatchmaking.SetLobbyMemberLimit))]
    [HarmonyPrefix]
    private static void SetLobbyMemberLimit_Prefix(SteamId steamIDLobby, ref int cMaxMembers)
    {
        if (!NetLobby.AmInLobby() || NetLobby.LobbyData.LobbyId != steamIDLobby) return;

        int realTotalMembers = SteamMatchmaking.Internal.GetNumLobbyMembersOriginal(steamIDLobby);
        int visibleMembers = GetFilteredMembers(steamIDLobby).Count;
        int ignoredCount = realTotalMembers - visibleMembers;

        cMaxMembers += ignoredCount;

        if (cMaxMembers < visibleMembers)
        {
            cMaxMembers = visibleMembers;
        }
    }
}