using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;
using System.Net;

namespace ReplantedOnline.Network.Server;

/// <summary>
/// Contains server metadata and lobby information for LAN games.
/// Manages lobby properties like name, player counts, joinability, and versioning.
/// </summary>
internal sealed class LanServerData : IDisposable
{
    internal const string SERVER_NAME_KEY = "server_name";
    internal const string PLAYER_COUNT_KEY = "player_count";
    internal const string MAX_PLAYER_COUNT_KEY = "max_player_count";
    internal const string IS_JOINABLE_KEY = "is_joinable";
    internal const string MOD_VERSION_KEY = "mod_version";
    internal const string GAME_CODE_KEY = "game_code";

    /// <summary>
    /// Gets or sets the ID of the host client.
    /// </summary>
    internal ID HostId { get; set; } = ID.Null;

    /// <summary>
    /// Gets or sets the unique lobby identifier.
    /// </summary>
    internal ID LobbyId { get; set; } = ID.Null;

    /// <summary>
    /// Gets or sets the port number for game connections.
    /// </summary>
    internal int GamePort { get; set; }

    /// <summary>
    /// Gets or sets the host's IP address.
    /// </summary>
    internal IPAddress HostAddress { get; set; }

    /// <summary>
    /// Custom key-value data associated with the lobby.
    /// </summary>
    internal Dictionary<string, string> Data = [];

    /// <summary>
    /// Sets the display name of the server.
    /// </summary>
    /// <param name="serverName">The server name to set.</param>
    internal void SetServerName(string serverName) => Data[SERVER_NAME_KEY] = serverName;

    /// <summary>
    /// Gets the display name of the server.
    /// </summary>
    /// <returns>The server name, or empty string if not set.</returns>
    internal string GetServerName() => Data.TryGetValue(SERVER_NAME_KEY, out var value) ? value : string.Empty;

    /// <summary>
    /// Sets the current number of players in the lobby.
    /// </summary>
    /// <param name="playerCount">The player count to set.</param>
    internal void SetPlayerCount(int playerCount) => Data[PLAYER_COUNT_KEY] = playerCount.ToString();

    /// <summary>
    /// Gets the current number of players in the lobby.
    /// </summary>
    /// <returns>The player count, or 0 if not set.</returns>
    internal int GetPlayerCount() => Data.TryGetValue(PLAYER_COUNT_KEY, out var value) && int.TryParse(value, out var count) ? count : 0;

    /// <summary>
    /// Sets the maximum number of players allowed in the lobby.
    /// </summary>
    /// <param name="maxPlayerCount">The maximum player count to set.</param>
    internal void SetMaxPlayerCount(int maxPlayerCount) => Data[MAX_PLAYER_COUNT_KEY] = maxPlayerCount.ToString();

    /// <summary>
    /// Gets the maximum number of players allowed in the lobby.
    /// </summary>
    /// <returns>The maximum player count, or 0 if not set.</returns>
    internal int GetMaxPlayerCount() => Data.TryGetValue(MAX_PLAYER_COUNT_KEY, out var value) && int.TryParse(value, out var count) ? count : 0;

    /// <summary>
    /// Sets whether new players can join the lobby.
    /// </summary>
    /// <param name="isJoinable">True if the lobby is joinable, false otherwise.</param>
    internal void SetIsJoinable(bool isJoinable) => Data[IS_JOINABLE_KEY] = isJoinable.ToString();

    /// <summary>
    /// Gets whether new players can join the lobby.
    /// </summary>
    /// <returns>True if joinable, false otherwise.</returns>
    internal bool GetIsJoinable() => Data.TryGetValue(IS_JOINABLE_KEY, out var value) && bool.TryParse(value, out var joinable) ? joinable : false;

    /// <summary>
    /// Sets the mod version required to join this lobby.
    /// </summary>
    /// <param name="modVersion">The mod version string.</param>
    internal void SetModVersion(string modVersion) => Data[MOD_VERSION_KEY] = modVersion;

    /// <summary>
    /// Gets the mod version required to join this lobby.
    /// </summary>
    /// <returns>The mod version string, or empty if not set.</returns>
    internal string GetModVersion() => Data.TryGetValue(MOD_VERSION_KEY, out var value) ? value : string.Empty;

    /// <summary>
    /// Sets the game code for joining this lobby.
    /// </summary>
    /// <param name="gameCode">The game code to set.</param>
    internal void SetGameCode(string gameCode) => Data[GAME_CODE_KEY] = gameCode;

    /// <summary>
    /// Gets the game code for joining this lobby.
    /// </summary>
    /// <returns>The game code, or empty if not set.</returns>
    internal string GetGameCode() => Data.TryGetValue(GAME_CODE_KEY, out var value) ? value : string.Empty;

    /// <summary>
    /// Resets all server data to default values.
    /// </summary>
    internal void Reset()
    {
        HostId = ID.Null;
        LobbyId = ID.Null;
        GamePort = 0;
        HostAddress = null;
        Data.Clear();
    }

    /// <summary>
    /// Serializes the server data for broadcast discovery.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    internal void SerializeBroadcast(PacketWriter packetWriter)
    {
        packetWriter.WriteID(HostId);
        packetWriter.WriteID(LobbyId);
        packetWriter.WriteInt(GamePort);

        // Write host IP address
        string hostAddress = HostAddress?.ToString() ?? "127.0.0.1";
        packetWriter.WriteString(hostAddress);

        packetWriter.WriteInt(Data.Count);
        foreach (var data in Data)
        {
            packetWriter.WriteString(data.Key);
            packetWriter.WriteString(data.Value);
        }
    }

    /// <summary>
    /// Deserializes server data from a broadcast packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    internal void DeserializeBroadcast(PacketReader packetReader)
    {
        HostId = packetReader.ReadID();
        LobbyId = packetReader.ReadID();
        GamePort = packetReader.ReadInt();

        string hostAddressStr = packetReader.ReadString();
        if (IPAddress.TryParse(hostAddressStr, out var address))
        {
            HostAddress = address;
        }

        Dictionary<string, string> data = [];
        int dataCount = packetReader.ReadInt();
        for (int i = 0; i < dataCount; i++)
        {
            string key = packetReader.ReadString();
            string value = packetReader.ReadString();
            data[key] = value;
        }
        Data = data;
    }

    /// <summary>
    /// Converts the server data to a ServerLobby structure.
    /// </summary>
    /// <returns>A ServerLobby object representing this lobby.</returns>
    internal ServerLobby ToServerLobby()
    {
        return new ServerLobby(
            lobbyId: LobbyId,
            ownerId: HostId,
            isJoinable: GetIsJoinable(),
            maxPlayers: GetMaxPlayerCount(),
            modVersion: GetModVersion(),
            gameCode: GetGameCode(),
            name: GetServerName()
        );
    }

    /// <summary>
    /// Disposes of resources used by the LanServerData.
    /// </summary>
    public void Dispose()
    {
    }
}