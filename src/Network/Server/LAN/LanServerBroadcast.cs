using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Handles LAN broadcast discovery for lobbies.
/// </summary>
internal sealed class LanServerBroadcast : IDisposable
{
    private readonly LanServer _server;
    private const int BROADCAST_PORT = 14242;
    private const int BROADCAST_INTERVAL_MS = 2000;
    private const int DISCOVERY_DURATION_SECONDS = 3;
    private const int SERVER_TIMEOUT_SECONDS = 6;

    private readonly object _discoveredLock = new();
    private readonly object _discoveryLock = new();

    /// <summary>
    /// UDP client used for broadcasting and receiving server advertisements.
    /// </summary>
    internal readonly UdpClient BroadcastClient = new();

    /// <summary>
    /// Cancellation token source for the listen operation.
    /// </summary>
    internal CancellationTokenSource ListenCTS;

    /// <summary>
    /// Cancellation token source for the broadcast operation.
    /// </summary>
    internal CancellationTokenSource BroadcastCTS;

    /// <summary>
    /// Dictionary of currently discovered servers on the local network.
    /// </summary>
    internal readonly Dictionary<ID, DiscoveredServerInfo> DiscoveredServers = [];

    private TaskCompletionSource<LanServerData> _discoveryCompletionSource;

    /// <summary>
    /// Contains information about a discovered server.
    /// </summary>
    internal class DiscoveredServerInfo
    {
        /// <summary>
        /// The server's lobby data.
        /// </summary>
        public LanServerData ServerData { get; set; }

        /// <summary>
        /// The last time a broadcast was received from this server.
        /// </summary>
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the LanServerBroadcast class.
    /// </summary>
    /// <param name="replantedServer">The parent LanServer instance.</param>
    internal LanServerBroadcast(LanServer replantedServer)
    {
        _server = replantedServer;
        BroadcastClient = new();
        BroadcastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        BroadcastClient.Client.Bind(new IPEndPoint(IPAddress.Any, BROADCAST_PORT));
        BroadcastClient.EnableBroadcast = true;
        ListenCTS = new();
        Task.Run(ListenForBroadcasts, ListenCTS.Token);
        Task.Run(CleanupOldServers, ListenCTS.Token);
    }

    /// <summary>
    /// Starts broadcasting server information to the local network.
    /// </summary>
    internal void StartBroadcasting()
    {
        BroadcastCTS = new();
        Task.Run(BroadcastServer, BroadcastCTS.Token);
    }

    /// <summary>
    /// Stops broadcasting server information.
    /// </summary>
    internal void StopBroadcasting()
    {
        BroadcastCTS?.Cancel();
    }

    /// <summary>
    /// Discovers the first available lobby on the local network.
    /// </summary>
    /// <returns>A task that resolves to the first discovered server data, or null if none found.</returns>
    internal async Task<LanServerData> DiscoverFirstLobby()
    {
        lock (_discoveryLock)
        {
            _discoveryCompletionSource = new TaskCompletionSource<LanServerData>();
        }

        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < DISCOVERY_DURATION_SECONDS)
        {
            LanServerData firstServer = null;
            lock (_discoveredLock)
            {
                var validServer = DiscoveredServers.Values
                    .FirstOrDefault(s => (DateTime.UtcNow - s.LastSeen).TotalSeconds < SERVER_TIMEOUT_SECONDS);
                if (validServer != null)
                {
                    firstServer = validServer.ServerData;
                }
            }
            if (firstServer != null)
            {
                return firstServer;
            }
            await Task.Delay(100);
        }

        return null;
    }

    /// <summary>
    /// Discovers all available lobbies on the local network.
    /// </summary>
    /// <returns>A task that resolves to a list of all discovered server data.</returns>
    internal async Task<List<LanServerData>> DiscoverAllLobbies()
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < DISCOVERY_DURATION_SECONDS)
        {
            await Task.Delay(100);
        }

        lock (_discoveredLock)
        {
            return DiscoveredServers.Values
                .Where(s => (DateTime.UtcNow - s.LastSeen).TotalSeconds < SERVER_TIMEOUT_SECONDS)
                .Select(s => s.ServerData)
                .ToList();
        }
    }

    /// <summary>
    /// Clears the list of discovered servers.
    /// </summary>
    internal void ClearDiscoveredServers()
    {
        lock (_discoveredLock)
        {
            DiscoveredServers.Clear();
        }
    }

    /// <summary>
    /// Gets information about a specific discovered server by lobby ID.
    /// </summary>
    /// <param name="lobbyId">The lobby ID to look up.</param>
    /// <returns>The server data if found and not expired, null otherwise.</returns>
    internal LanServerData GetDiscoveredServer(ID lobbyId)
    {
        lock (_discoveredLock)
        {
            if (DiscoveredServers.TryGetValue(lobbyId, out var info))
            {
                if ((DateTime.UtcNow - info.LastSeen).TotalSeconds < SERVER_TIMEOUT_SECONDS)
                {
                    return info.ServerData;
                }
                else
                {
                    DiscoveredServers.Remove(lobbyId);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Broadcasts server information periodically to the local network.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task BroadcastServer()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);

        while (!BroadcastCTS.Token.IsCancellationRequested)
        {
            try
            {
                byte[] buffer;
                var packetWriter = PacketWriter.Get();
                _server.ServerData.SerializeBroadcast(packetWriter);
                buffer = packetWriter.GetByteBuffer();
                packetWriter.Recycle();
                await BroadcastClient.SendAsync(buffer, buffer.Length, broadcastEndpoint);
                await Task.Delay(BROADCAST_INTERVAL_MS, BroadcastCTS.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[Broadcast] Error: {ex}");
                await Task.Delay(1000, BroadcastCTS.Token);
            }
        }
    }

    /// <summary>
    /// Listens for broadcast packets from other servers on the local network.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ListenForBroadcasts()
    {
        while (!ListenCTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await BroadcastClient.ReceiveAsync();

                if (result.Buffer.Length == 0) continue;

                LanServerData serverData;
                var packetReader = PacketReader.Get(result.Buffer);
                serverData = new LanServerData();

                try
                {
                    serverData.DeserializeBroadcast(packetReader);
                    packetReader.Recycle();
                }
                catch
                {
                    packetReader.Recycle();
                    continue;
                }

                if (serverData.LobbyId == _server.ServerData.LobbyId) continue;

                if (serverData.GetModVersion() != ModInfo.MOD_VERSION_FORMATTED)
                {
                    continue;
                }

                if (serverData.GamePort < 1 || serverData.GamePort > 65535)
                {
                    ReplantedOnlineMod.Logger.Warning($"[Broadcast] Invalid port {serverData.GamePort} from {serverData.GetServerName()}");
                    continue;
                }

                bool isNewServer = false;
                bool serverChanged = false;

                lock (_discoveredLock)
                {
                    if (!DiscoveredServers.ContainsKey(serverData.LobbyId))
                    {
                        DiscoveredServers[serverData.LobbyId] = new DiscoveredServerInfo
                        {
                            ServerData = serverData,
                            LastSeen = DateTime.UtcNow
                        };
                        isNewServer = true;
                    }
                    else
                    {
                        var existing = DiscoveredServers[serverData.LobbyId];
                        if (existing.ServerData.GamePort != serverData.GamePort)
                        {
                            serverChanged = true;
                        }
                        existing.ServerData = serverData;
                        existing.LastSeen = DateTime.UtcNow;
                    }
                }

                if (isNewServer || serverChanged)
                {
                    TaskCompletionSource<LanServerData> tcs;
                    lock (_discoveryLock)
                    {
                        tcs = _discoveryCompletionSource;
                    }
                    tcs?.TrySetResult(serverData);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode == SocketError.OperationAborted ||
                ex.SocketErrorCode == SocketError.Interrupted ||
                ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[Broadcast] Listen error: {ex}");
                await Task.Delay(1000, ListenCTS.Token);
            }
        }
    }

    /// <summary>
    /// Periodically removes servers that haven't been seen within the timeout period.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CleanupOldServers()
    {
        while (!ListenCTS.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, ListenCTS.Token);

                List<ID> expiredServers = new List<ID>();
                lock (_discoveredLock)
                {
                    foreach (var kvp in DiscoveredServers)
                    {
                        if ((DateTime.UtcNow - kvp.Value.LastSeen).TotalSeconds >= SERVER_TIMEOUT_SECONDS)
                        {
                            expiredServers.Add(kvp.Key);
                        }
                    }

                    foreach (var serverId in expiredServers)
                    {
                        var server = DiscoveredServers[serverId];
                        DiscoveredServers.Remove(serverId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[Broadcast] Cleanup error: {ex}");
            }
        }
    }

    /// <summary>
    /// Disposes of all resources used by the LanServerBroadcast.
    /// </summary>
    public void Dispose()
    {
        StopBroadcasting();
        ListenCTS?.Cancel();
        BroadcastClient?.Close();
        ListenCTS?.Dispose();
        BroadcastCTS?.Dispose();
    }
}