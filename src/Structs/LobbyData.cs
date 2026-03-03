using Il2CppSteamworks.Data;
using ReplantedOnline.Helper;

namespace ReplantedOnline.Structs;

/// <summary>
/// Represents essential information about a lobby, including its unique identifier and the owner's identifier.
/// </summary>
internal struct LobbyData
{
    internal static LobbyData Null { get; } = new(ID.Null, ID.Null);

    internal readonly ID Id;
    internal readonly ID OwnerId;
    internal bool IsJoinable;
    internal int MaxPlayers;
    internal string ModVersion;
    internal string GameCode;
    internal string Name;

    internal LobbyData(Lobby lobby)
    {
        Id = lobby.Id.AsID();
        OwnerId = lobby.Owner.Id.AsID();
        IsJoinable = true;
        MaxPlayers = lobby.MaxMembers;
        ModVersion = lobby.GetData(ReplantedOnlineMod.Constants.MOD_VERSION_KEY);
        GameCode = lobby.GetData(ReplantedOnlineMod.Constants.GAME_CODE_KEY);
        Name = string.Empty;
    }

    internal LobbyData(ID lobbyId, ID ownerId, bool isJoinable = true, int maxPlayers = 2, string modVersion = "", string gameCode = "", string name = "")
    {
        Id = lobbyId;
        OwnerId = ownerId;
        IsJoinable = isJoinable;
        MaxPlayers = maxPlayers;
        ModVersion = modVersion;
        GameCode = gameCode;
        Name = name;
    }
}