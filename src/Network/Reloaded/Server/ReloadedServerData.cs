using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Reloaded.Server;

/// <summary>
/// Contains server metadata and lobby information for IP-based games.
/// Manages lobby properties like name, player counts, joinability, and versioning.
/// </summary>
internal sealed class ReloadedServerData : IDisposable
{
    internal const string SERVER_NAME_KEY = "server_name";
    internal const string PLAYER_COUNT_KEY = "player_count";
    internal const string MAX_PLAYER_COUNT_KEY = "max_player_count";
    internal const string IS_JOINABLE_KEY = "is_joinable";
    internal const string MOD_VERSION_KEY = "mod_version";
    internal const string GAME_CODE_KEY = "game_code";

    internal ReloadedServerData() { }

    /// <summary>
    /// Creates server data for a direct IP connection.
    /// </summary>
    internal ReloadedServerData(IPAddress ipAddress, int port, ID? hostId = null)
    {
        HostAddress = ipAddress;
        GamePort = port;
        HostId = hostId ?? new ID(new IPEndPoint(ipAddress, port), IdType.IPEndPoint);
        LobbyId = ID.CreateRandomULong();
        Data = [];
    }

    /// <summary>
    /// Builds server data for a direct connection from a host string (IP or hostname) and port.
    /// Resolves DNS if the host is not a literal IP address.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the host cannot be parsed or resolved.</exception>
    internal static ReloadedServerData ForDirectConnection(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be null or empty.", nameof(host));

        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");

        IPAddress? ip = null;

        // Try parse as literal IP first (fast path).
        if (IPAddress.TryParse(host.Trim(), out var parsed))
        {
            ip = parsed;
        }
        else
        {
            // Fall back to DNS resolution. Prefer IPv4 for now (matches existing code paths).
            try
            {
                var addresses = Dns.GetHostAddresses(host.Trim());
                ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                  ?? addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Could not resolve host '{host}': {ex.Message}", nameof(host), ex);
            }
        }

        if (ip == null)
            throw new ArgumentException($"Could not resolve host '{host}' to any IP address.", nameof(host));

        return new ReloadedServerData(ip, port);
    }

    /// <summary>Gets or sets the ID of the host member.</summary>
    internal ID HostId { get; set; } = ID.Null;

    /// <summary>Gets or sets the unique lobby identifier.</summary>
    internal ID LobbyId { get; set; } = ID.Null;

    /// <summary>Gets or sets the port number for game connections.</summary>
    internal int GamePort { get; set; }

    /// <summary>Gets or sets the host's IP address.</summary>
    internal IPAddress HostAddress { get; set; } = default!;

    /// <summary>Custom key-value data associated with the lobby.</summary>
    internal Dictionary<string, string> Data = [];

    internal void SetServerName(string serverName) => Data[SERVER_NAME_KEY] = serverName;
    internal string GetServerName() => Data.TryGetValue(SERVER_NAME_KEY, out var value) ? value : string.Empty;

    internal void SetPlayerCount(int playerCount) => Data[PLAYER_COUNT_KEY] = playerCount.ToString();
    internal int GetPlayerCount() => Data.TryGetValue(PLAYER_COUNT_KEY, out var value) && int.TryParse(value, out var count) ? count : 0;

    internal void SetMaxPlayerCount(int maxPlayerCount) => Data[MAX_PLAYER_COUNT_KEY] = maxPlayerCount.ToString();
    internal int GetMaxPlayerCount() => Data.TryGetValue(MAX_PLAYER_COUNT_KEY, out var value) && int.TryParse(value, out var count) ? count : 0;

    internal void SetIsJoinable(bool isJoinable) => Data[IS_JOINABLE_KEY] = isJoinable.ToString();
    internal bool GetIsJoinable() => Data.TryGetValue(IS_JOINABLE_KEY, out var value) && bool.TryParse(value, out var joinable) && joinable;

    internal void SetModVersion(string modVersion) => Data[MOD_VERSION_KEY] = modVersion;
    internal string GetModVersion() => Data.TryGetValue(MOD_VERSION_KEY, out var value) ? value : string.Empty;

    internal void SetGameCode(string gameCode) => Data[GAME_CODE_KEY] = gameCode;
    internal string GetGameCode() => Data.TryGetValue(GAME_CODE_KEY, out var value) ? value : string.Empty;

    /// <summary>
    /// Resets all server data to default values.
    /// </summary>
    internal void Reset()
    {
        HostId = ID.Null;
        LobbyId = ID.Null;
        GamePort = 0;
        HostAddress = null!;
        Data.Clear();
    }

    /// <summary>
    /// Serializes the server data for broadcast discovery.
    /// </summary>
    internal void SerializeBroadcast(PacketWriter packetWriter)
    {
        packetWriter.WriteID(HostId);
        packetWriter.WriteID(LobbyId);
        packetWriter.WriteInt(GamePort);
        packetWriter.WriteString(HostAddress?.ToString() ?? "127.0.0.1");

        packetWriter.WritePackedInt(Data.Count);
        foreach (var data in Data)
        {
            packetWriter.WriteString(data.Key);
            packetWriter.WriteString(data.Value);
        }
    }

    /// <summary>
    /// Deserializes server data from a broadcast packet.
    /// </summary>
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
        int dataCount = packetReader.ReadPackedInt();
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

    public void Dispose() { }
}