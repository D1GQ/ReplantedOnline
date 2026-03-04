using Il2CppSteamworks.Data;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Server.LAN;

namespace ReplantedOnline.Structs;

/// <summary>
/// Represents essential information about a lobby, including its unique identifier and the owner's identifier.
/// Used for LAN lobby management.
/// </summary>
internal struct LanLobbyData
{
    /// <summary>
    /// Gets a null/empty lobby data instance.
    /// </summary>
    internal static LanLobbyData Null { get; } = new(ID.Null, ID.Null);

    /// <summary>
    /// Gets the unique identifier of the lobby.
    /// </summary>
    internal readonly ID Id;

    /// <summary>
    /// Gets the unique identifier of the lobby owner/host.
    /// </summary>
    internal readonly ID OwnerId;

    /// <summary>
    /// Gets or sets a value indicating whether the lobby is currently joinable.
    /// </summary>
    internal bool IsJoinable;

    /// <summary>
    /// Gets or sets the maximum number of players allowed in the lobby.
    /// </summary>
    internal int MaxPlayers;

    /// <summary>
    /// Gets or sets the mod version string for compatibility checking.
    /// </summary>
    internal string ModVersion;

    /// <summary>
    /// Gets or sets the game code used for lobby identification and password protection.
    /// </summary>
    internal string GameCode;

    /// <summary>
    /// Gets or sets the display name of the lobby.
    /// </summary>
    internal string Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanLobbyData"/> struct from a Steam lobby.
    /// </summary>
    /// <param name="lobby">The Steam lobby to extract data from.</param>
    internal LanLobbyData(Lobby lobby)
    {
        Id = lobby.Id.AsID();
        OwnerId = lobby.Owner.Id.AsID();
        IsJoinable = true;
        MaxPlayers = lobby.MaxMembers;
        ModVersion = lobby.GetData(ReplantedOnlineMod.Constants.MOD_VERSION_KEY);
        GameCode = lobby.GetData(ReplantedOnlineMod.Constants.GAME_CODE_KEY);
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LanLobbyData"/> struct with the specified parameters.
    /// </summary>
    /// <param name="lobbyId">The unique identifier of the lobby.</param>
    /// <param name="ownerId">The unique identifier of the lobby owner.</param>
    /// <param name="isJoinable">Whether the lobby is joinable.</param>
    /// <param name="maxPlayers">Maximum number of players allowed.</param>
    /// <param name="modVersion">Mod version for compatibility.</param>
    /// <param name="gameCode">Game code for lobby access.</param>
    /// <param name="name">Display name of the lobby.</param>
    internal LanLobbyData(ID lobbyId, ID ownerId, bool isJoinable = true, int maxPlayers = 2, string modVersion = "", string gameCode = "", string name = "")
    {
        Id = lobbyId;
        OwnerId = ownerId;
        IsJoinable = isJoinable;
        MaxPlayers = maxPlayers;
        ModVersion = modVersion;
        GameCode = gameCode;
        Name = name;
    }

    /// <summary>
    /// Creates a new <see cref="LanLobbyData"/> instance from a LAN server presence broadcast.
    /// </summary>
    /// <param name="presence">The LAN server presence data.</param>
    /// <returns>A new lobby data instance populated from the presence information.</returns>
    internal static LanLobbyData CreateLobbyDataFromPresence(LanServerPresence presence)
    {
        return new LanLobbyData(
            lobbyId: presence.LobbyId,
            ownerId: presence.ServerId,
            isJoinable: presence.IsJoinable,
            maxPlayers: presence.MaxPlayers,
            modVersion: presence.ModVersion,
            gameCode: presence.GameCode,
            name: presence.ServerName
        );
    }
}