using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing;

/// <inheritdoc/>
internal static partial class NetworkManager
{
    /// <summary>
    /// Gets the current network heartbeat instance used for monitoring connection health.
    /// </summary>
    /// <value>The active heartbeat instance, or <c>null</c> if not initialized.</value>
    internal static Heartbeat NetworkHeartbeat { get; private set; } = null!;

    /// <summary>
    /// Manages network heartbeat operations to monitor client connection health and latency.
    /// </summary>
    internal sealed class Heartbeat
    {
        /// <summary>
        /// Occurs when a client fails to respond to a heartbeat request within the specified timeout period.
        /// </summary>
        internal static event Action<ReloadedClientData>? OnClientHeartbeatTimeout;

        /// <summary>
        /// Maximum age in seconds for heartbeat requests before they are considered stale.
        /// </summary>
        private const int HEARTBEAT_AGE_CAP_SECONDS = 15;

        /// <summary>
        /// Timer that controls the frequency of heartbeat packet sends.
        /// </summary>
        private readonly UnityTimer sendTimer = new();

        /// <summary>
        /// Incrementing timestamp counter for tracking individual heartbeat requests.
        /// </summary>
        private ulong _currentTimeStamp = 0;

        /// <summary>
        /// Dictionary mapping heartbeat timestamps to their request send times.
        /// </summary>
        private readonly Dictionary<ulong, DateTime> _heartbeatRequests = [];

        /// <summary>
        /// Dictionary mapping client IDs to their most recent heartbeat response delay.
        /// </summary>
        private readonly Dictionary<ID, float> _heartbeatDelaysLookup = [];

        /// <summary>
        /// Dictionary tracking which clients have pending heartbeat requests.
        /// </summary>
        private readonly Dictionary<ID, ulong> _clientPendingHeartbeats = [];

        /// <summary>
        /// Initializes a new instance of the heartbeat system and sets the static reference.
        /// </summary>
        internal static void Start()
        {
            NetworkHeartbeat = new();
        }

        /// <summary>
        /// Processes a single tick of the heartbeat system.
        /// </summary>
        internal void Tick()
        {
            if (ReloadedLobby.LobbyData == null)
                return;

            if (sendTimer.AccumulatedTime > 1f)
            {
                sendTimer.Reset();
                Packet<HeartbeatRequestPacket>.Singleton.Send(_currentTimeStamp);
                _heartbeatRequests[_currentTimeStamp] = DateTime.Now;

                foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
                {
                    _clientPendingHeartbeats[client.ClientId] = _currentTimeStamp;
                }

                _currentTimeStamp++;

                var cutoff = DateTime.Now.AddSeconds(-HEARTBEAT_AGE_CAP_SECONDS);
                var oldKeys = _heartbeatRequests.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();

                foreach (var key in oldKeys)
                {
                    var timedOutClients = _clientPendingHeartbeats
                        .Where(kvp => kvp.Value == key)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var clientId in timedOutClients)
                    {
                        if (ReloadedLobby.LobbyData.AllClients.TryGetValue(clientId, out var clientData))
                        {
                            OnClientHeartbeatTimeout?.Invoke(clientData);

                            _heartbeatDelaysLookup.Remove(clientId);
                        }
                    }

                    foreach (var clientId in timedOutClients)
                    {
                        _clientPendingHeartbeats.Remove(clientId);
                    }

                    _heartbeatRequests.Remove(key);
                }
            }
        }

        /// <summary>
        /// Handles an incoming heartbeat response from a client.
        /// </summary>
        /// <param name="sender">The client that sent the heartbeat response.</param>
        /// <param name="timeStamp">The timestamp of the original heartbeat request.</param>
        internal void HandleHeartbeat(ReloadedClientData sender, ulong timeStamp)
        {
            if (!_heartbeatRequests.TryGetValue(timeStamp, out var requestTime))
                return;

            var delay = (float)(DateTime.Now - requestTime).TotalMilliseconds;
            _heartbeatDelaysLookup[sender.ClientId] = delay;

            // Remove the pending heartbeat for this client
            if (_clientPendingHeartbeats.TryGetValue(sender.ClientId, out var pendingTimestamp) &&
                pendingTimestamp == timeStamp)
            {
                _clientPendingHeartbeats.Remove(sender.ClientId);
            }

            _heartbeatRequests.Remove(timeStamp);
        }

        /// <summary>
        /// Calculates the estimated average ping time across all connected clients.
        /// </summary>
        /// <returns>
        /// The average ping time in milliseconds, or 0 if no clients have responded yet.
        /// </returns>
        internal int GetEstimatedPing()
        {
            if (_heartbeatDelaysLookup.Count == 0)
                return 0;

            return (int)_heartbeatDelaysLookup.Values.Average();
        }

        /// <summary>
        /// Disposes of the current heartbeat instance and clears the static reference.
        /// </summary>
        /// <remarks>
        /// This should be called when the network manager is being shut down to ensure
        /// proper cleanup of resources.
        /// </remarks>
        internal static void Dispose()
        {
            NetworkHeartbeat = null!;
        }
    }
}