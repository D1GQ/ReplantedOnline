using Il2CppSteamworks.Data;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Structs;

/// <summary>
/// Represents essential information about a lobby, including its unique identifier and the owner's identifier.
/// </summary>
internal struct ServerLobby
{
    /// <summary>
    /// Gets a null/empty lobby data instance.
    /// </summary>
    internal static ServerLobby Null { get; } = new(ID.Null, ID.Null);

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
    /// Initializes a new instance of the <see cref="ServerLobby"/> struct from a Steam lobby.
    /// </summary>
    /// <param name="lobby">The Steam lobby to extract data from.</param>
    internal ServerLobby(Lobby lobby)
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
    /// Initializes a new instance of the <see cref="ServerLobby"/> struct with the specified parameters.
    /// </summary>
    /// <param name="lobbyId">The unique identifier of the lobby.</param>
    /// <param name="ownerId">The unique identifier of the lobby owner.</param>
    /// <param name="isJoinable">Whether the lobby is joinable.</param>
    /// <param name="maxPlayers">Maximum number of players allowed.</param>
    /// <param name="modVersion">Mod version for compatibility.</param>
    /// <param name="gameCode">Game code for lobby access.</param>
    /// <param name="name">Display name of the lobby.</param>
    internal ServerLobby(ID lobbyId, ID ownerId, bool isJoinable = true, int maxPlayers = 2, string modVersion = "", string gameCode = "", string name = "")
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