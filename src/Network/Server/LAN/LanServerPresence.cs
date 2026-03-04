using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Represents a LAN server's presence information for discovery broadcasts.
/// This class is serialized to JSON and broadcasted over UDP for server discovery.
/// </summary>
[Serializable]
internal sealed class LanServerPresence
{
    /// <summary>
    /// Gets or sets the unique identifier of the server.
    /// </summary>
    public ID ServerId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the server.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the current number of players in the lobby.
    /// </summary>
    public int PlayerCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of players allowed in the lobby.
    /// </summary>
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the lobby.
    /// </summary>
    public ID LobbyId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lobby is currently joinable.
    /// </summary>
    public bool IsJoinable { get; set; }

    /// <summary>
    /// Gets or sets the mod version string for compatibility checking.
    /// </summary>
    public string ModVersion { get; set; }

    /// <summary>
    /// Gets or sets the game code used for lobby identification.
    /// </summary>
    public string GameCode { get; set; }

    /// <summary>
    /// Gets or sets the port number used for game communication.
    /// </summary>
    public int GamePort { get; set; }

    /// <summary>
    /// Gets or sets the IP endpoint address of the server.
    /// </summary>
    public string EndPoint { get; set; }
}