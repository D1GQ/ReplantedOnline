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
        private const int HEARTBEAT_MAX_AGE = 10;

        /// <summary>
        /// Interval in seconds between heartbeat packet sends.
        /// </summary>
        private const int HEARTBEAT_INTERVAL = 1;

        /// <summary>
        /// Timer that controls the frequency of heartbeat packet sends.
        /// </summary>
        private readonly UnityTimer sendTimer = new();

        /// <summary>
        /// Incrementing timestamp counter for tracking individual heartbeat requests.
        /// </summary>
        private uint _currentTimeStamp = 0;

        /// <summary>
        /// Dictionary mapping heartbeat timestamps to their request send times.
        /// </summary>
        private readonly Dictionary<uint, DateTime> _heartbeatRequests = [];

        /// <summary>
        /// Dictionary mapping client IDs to their most recent heartbeat response delay.
        /// </summary>
        private readonly Dictionary<ID, float> _heartbeatDelaysLookup = [];

        /// <summary>
        /// Dictionary tracking which clients have pending heartbeat requests.
        /// </summary>
        private readonly Dictionary<ID, uint> _clientPendingHeartbeats = [];

        /// <summary>
        /// Dictionary tracking the last time each client responded to a heartbeat.
        /// </summary>
        private readonly Dictionary<ID, DateTime> _lastResponseTime = [];

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

            if (sendTimer.AccumulatedTime > HEARTBEAT_INTERVAL)
            {
                sendTimer.Reset();
                SendHeartbeat();
            }

            CheckForTimeouts();
        }

        /// <summary>
        /// Sends a heartbeat request to all connected clients.
        /// </summary>
        private void SendHeartbeat()
        {
            Packet<HeartbeatRequestPacket>.Singleton.Send(_currentTimeStamp);
            _heartbeatRequests[_currentTimeStamp] = DateTime.Now;

            foreach (var client in ReloadedLobby.LobbyData!.AllClients.Values)
            {
                _clientPendingHeartbeats[client.ClientId] = _currentTimeStamp;
            }

            _currentTimeStamp++;
        }

        /// <summary>
        /// Checks for and processes any client heartbeat timeouts.
        /// </summary>
        private void CheckForTimeouts()
        {
            var cutoff = DateTime.Now.AddSeconds(-HEARTBEAT_MAX_AGE);

            List<ID> timedOutClients = [];
            foreach (var kvp in _lastResponseTime)
            {
                if (kvp.Value < cutoff)
                {
                    timedOutClients.Add(kvp.Key);
                }
            }

            foreach (var clientId in timedOutClients)
            {
                if (ReloadedLobby.LobbyData!.AllClients.TryGetValue(clientId, out var clientData))
                {
                    OnClientHeartbeatTimeout?.Invoke(clientData);
                    _heartbeatDelaysLookup.Remove(clientId);
                    _lastResponseTime.Remove(clientId);
                    _clientPendingHeartbeats.Remove(clientId);
                }
            }

            List<uint> oldHeartbeats = [];
            foreach (var kvp in _heartbeatRequests)
            {
                if (kvp.Value < cutoff)
                {
                    bool isStillPending = false;
                    foreach (var pending in _clientPendingHeartbeats)
                    {
                        if (pending.Value == kvp.Key)
                        {
                            isStillPending = true;
                            break;
                        }
                    }

                    if (!isStillPending)
                    {
                        oldHeartbeats.Add(kvp.Key);
                    }
                }
            }

            foreach (var timestamp in oldHeartbeats)
            {
                _heartbeatRequests.Remove(timestamp);
            }
        }

        /// <summary>
        /// Handles an incoming heartbeat response from a client.
        /// </summary>
        /// <param name="sender">The client that sent the heartbeat response.</param>
        /// <param name="timeStamp">The timestamp of the original heartbeat request.</param>
        internal void HandleHeartbeat(ReloadedClientData sender, uint timeStamp)
        {
            if (!_heartbeatRequests.TryGetValue(timeStamp, out var requestTime))
                return;

            var delay = (float)(DateTime.Now - requestTime).TotalMilliseconds;
            _heartbeatDelaysLookup[sender.ClientId] = delay;
            _lastResponseTime[sender.ClientId] = DateTime.Now;

            if (_clientPendingHeartbeats.TryGetValue(sender.ClientId, out var pendingTimestamp))
            {
                if (pendingTimestamp < timeStamp)
                {
                    _clientPendingHeartbeats[sender.ClientId] = timeStamp;

                    bool isOldHeartbeatStillPending = false;
                    foreach (var kvp in _clientPendingHeartbeats)
                    {
                        if (kvp.Value == pendingTimestamp)
                        {
                            isOldHeartbeatStillPending = true;
                            break;
                        }
                    }

                    if (!isOldHeartbeatStillPending)
                    {
                        _heartbeatRequests.Remove(pendingTimestamp);
                    }
                }
                else if (pendingTimestamp == timeStamp)
                {
                    _clientPendingHeartbeats.Remove(sender.ClientId);
                }
            }

            bool isStillPending = false;
            foreach (var kvp in _clientPendingHeartbeats)
            {
                if (kvp.Value == timeStamp)
                {
                    isStillPending = true;
                    break;
                }
            }

            if (!isStillPending)
            {
                _heartbeatRequests.Remove(timeStamp);
            }
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

            float total = 0f;
            foreach (var kvp in _heartbeatDelaysLookup)
            {
                total += kvp.Value;
            }

            return (int)(total / _heartbeatDelaysLookup.Count);
        }

        /// <summary>
        /// Disposes of the current heartbeat instance and clears the static reference.
        /// </summary>
        internal static void Dispose()
        {
            NetworkHeartbeat = null!;
        }
    }
}