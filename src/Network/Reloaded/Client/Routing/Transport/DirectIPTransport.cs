using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Server.Lan;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Transport;

/// <summary>
/// Provides IP-based network transport functionality
/// </summary>
internal class DirectIPTransport : LanTransport
{
    /// <summary>
    /// Character set used for IPs.
    /// </summary>
    internal static readonly char[] IP_CHARS = "0123456789.:".ToCharArray();

    /// <summary>
    /// Joins a lobby directly by IP address and port
    /// </summary>
    /// <param name="ipAddress">The IP address of the host</param>
    /// <param name="port">The port the host is listening on</param>
    public async Task JoinByIP(string ipAddress, int port)
    {
        if (_isJoining || _hasJoined)
            return;

        Transitions.SetLoading();

        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), $"Attempting to join lobby at {ipAddress}:{port}");

            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid IP address: {ipAddress}");
                _isJoining = false;
                ShowDisconnectPopup("Invalid IP address");
                return;
            }

            if (port < 1 || port > 65535)
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid port: {port}");
                _isJoining = false;
                ShowDisconnectPopup("Invalid port number");
                return;
            }

            var endpoint = new IPEndPoint(ip, port);

            // Create server data from the endpoint
            var serverData = new LanServerData
            {
                HostAddress = ip,
                GamePort = port,
                LobbyId = ID.CreateRandomULong(), // Will be updated during handshake
                HostId = new ID(endpoint, IdType.IPEndPoint)
            };
            serverData.SetServerName($"Direct Connection to {ipAddress}");
            serverData.SetModVersion(ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED);

            // Start client and connect
            string name;
            if (ReloadedLobby.TransportMode == TransportMode.Lan)
            {
                name = "Client";
            }
            else
            {
                try
                {
                    name = SteamFriends.Internal.GetPersonaName();
                }
                catch
                {
                    name = "Client";
                }
            }
            LanServer.StartClient(name);

            // Set up handshake completion
            var handshakeTask = LanServer.Server.JoinServer(serverData);

            // Wait for handshake with timeout
            var timeout = Task.Delay(5000);
            var completed = await Task.WhenAny(handshakeTask, timeout);

            if (completed == timeout)
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), "Connection timed out");
                _isJoining = false;
                LanServer.Leave();
                ShowDisconnectPopup("Connection timed out");
                return;
            }

            var success = await handshakeTask;
            if (!success)
            {
                var reason = LanServer.Server.RejectionReasons.TryGetValue(LanServer.Server.ServerData.HostId, out var rejectionReason)
                    ? rejectionReason
                    : "Connection rejected";
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Connection rejected: {reason}");
                _isJoining = false;
                LanServer.Leave();
                ShowDisconnectPopup($"Connection rejected: {reason}");
                return;
            }

            _hasJoined = true;
            _isJoining = false;
            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), $"Successfully joined lobby at {ipAddress}:{port}");
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Error joining by IP: {ex.Message}");
            _isJoining = false;
            LanServer.Leave();
            ShowDisconnectPopup($"Error joining: {ex.Message}");
        }
    }

    /// <summary>
    /// Joins a lobby directly by IP:Port string
    /// </summary>
    /// <param name="ipAndPort">The IP:Port string (e.g., "192.168.1.100:14242")</param>
    public async Task JoinByIPString(string ipAndPort)
    {
        var parts = ipAndPort.Split(':');
        if (parts.Length != 2)
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid IP:Port format: {ipAndPort}");
            ShowDisconnectPopup("Invalid format. Use IP:Port (e.g., 192.168.1.100:14242)");
            return;
        }

        if (!int.TryParse(parts[1], out var port))
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid port in: {ipAndPort}");
            ShowDisconnectPopup("Invalid port number");
            return;
        }

        await JoinByIP(parts[0], port);
    }
}
