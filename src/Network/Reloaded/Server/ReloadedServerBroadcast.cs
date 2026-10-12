using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Reloaded.Server;

/// <summary>
/// Handles LAN broadcast discovery for IP-based lobbies.
/// Only used when the connection mode includes LAN broadcast.
/// </summary>
internal sealed class ReloadedServerBroadcast : IDisposable
{
    private readonly ReloadedServer _server;
    private const int BROADCAST_PORT = 14242;
    private const int BROADCAST_INTERVAL_MS = 2000;
    private const int DISCOVERY_DURATION_SECONDS = 3;
    private const int SERVER_TIMEOUT_SECONDS = 6;

    private readonly object _discoveredLock = new();
    private readonly object _discoveryLock = new();

    internal readonly UdpClient BroadcastClient;

    internal CancellationTokenSource ListenCTS = default!;
    internal CancellationTokenSource BroadcastCTS = default!;

    internal readonly Dictionary<ID, DiscoveredServerInfo> DiscoveredServers = [];

    private TaskCompletionSource<ReloadedServerData> _discoveryCompletionSource = default!;

    internal class DiscoveredServerInfo
    {
        public ReloadedServerData ServerData { get; set; } = default!;
        public DateTime LastSeen { get; set; }
    }

    internal ReloadedServerBroadcast(ReloadedServer server)
    {
        _server = server;
        BroadcastClient = new();
        BroadcastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        BroadcastClient.Client.Bind(new IPEndPoint(IPAddress.Any, BROADCAST_PORT));
        BroadcastClient.EnableBroadcast = true;
        ListenCTS = new();
        Task.Run(ListenForBroadcasts, ListenCTS.Token);
        Task.Run(CleanupOldServers, ListenCTS.Token);
    }

    internal void StartBroadcasting()
    {
        BroadcastCTS = new();
        Task.Run(BroadcastServer, BroadcastCTS.Token);
    }

    internal void StopBroadcasting()
    {
        BroadcastCTS?.Cancel();
    }

    internal async Task<ReloadedServerData?> DiscoverFirstLobby()
    {
        lock (_discoveryLock)
        {
            _discoveryCompletionSource = new TaskCompletionSource<ReloadedServerData>();
        }

        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < DISCOVERY_DURATION_SECONDS)
        {
            ReloadedServerData? firstServer = null;
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

    internal async Task<List<ReloadedServerData>> DiscoverAllLobbies()
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

    internal void ClearDiscoveredServers()
    {
        lock (_discoveredLock)
        {
            DiscoveredServers.Clear();
        }
    }

    internal ReloadedServerData? GetDiscoveredServer(ID lobbyId)
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

    internal async Task BroadcastServer()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);

        while (!BroadcastCTS.Token.IsCancellationRequested)
        {
            try
            {
                var packetWriter = PacketWriter.Get();
                _server.ServerData.SerializeBroadcast(packetWriter);
                byte[] buffer = packetWriter.GetBytes();
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
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedServerBroadcast), $"Error: {ex}");
                await Task.Delay(1000, BroadcastCTS.Token);
            }
        }
    }

    private async Task ListenForBroadcasts()
    {
        while (!ListenCTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await BroadcastClient.ReceiveAsync();

                if (result.Buffer.Length == 0) continue;

                ReloadedServerData serverData;
                var packetReader = PacketReader.Get(result.Buffer);
                serverData = new ReloadedServerData();

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

                if (serverData.GetModVersion() != ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED)
                {
                    continue;
                }

                if (serverData.GamePort < 1 || serverData.GamePort > 65535)
                {
                    ReplantedOnlineMod.Logger.Warning(typeof(ReloadedServerBroadcast), $"Invalid port {serverData.GamePort} from {serverData.GetServerName()}");
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
                    TaskCompletionSource<ReloadedServerData> tcs;
                    lock (_discoveryLock)
                    {
                        tcs = _discoveryCompletionSource;
                    }
                    tcs?.TrySetResult(serverData);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException ex) when (
                ex.SocketErrorCode == SocketError.OperationAborted ||
                ex.SocketErrorCode == SocketError.Interrupted ||
                ex.SocketErrorCode == SocketError.ConnectionReset)
            { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedServerBroadcast), $"Listen error: {ex}");
                await Task.Delay(1000, ListenCTS.Token);
            }
        }
    }

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
                        DiscoveredServers.Remove(serverId);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedServerBroadcast), $"Cleanup error: {ex}");
            }
        }
    }

    public void Dispose()
    {
        StopBroadcasting();
        ListenCTS?.Cancel();
        BroadcastClient?.Close();
        ListenCTS?.Dispose();
        BroadcastCTS?.Dispose();
    }
}