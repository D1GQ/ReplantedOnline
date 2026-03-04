using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Interfaces;

/// <summary>
/// Network transport interface using the unified ID wrapper.
/// </summary>
internal interface INetworkTransport : IDisposable
{
    /// <summary>
    /// Gets the unique identifier assigned to the local client instance.
    /// </summary>
    ID LocalClientId { get; }

    /// <summary>
    /// Called every frame to process network events and callbacks.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    void Tick(float deltaTime);

    /// <summary>
    /// Checks if a P2P packet is available to read.
    /// </summary>
    /// <param name="msgSize">Output parameter that receives the size of the available packet in bytes.</param>
    /// <param name="channel">The channel to check for packets on.</param>
    /// <returns>True if a packet is available, false otherwise.</returns>
    bool IsP2PPacketAvailable(out uint msgSize, PacketChannel channel = PacketChannel.Main);

    /// <summary>
    /// Sends a P2P packet to a specific client.
    /// </summary>
    /// <param name="clientId">The ID of the target client.</param>
    /// <param name="data">The byte array containing the packet data.</param>
    /// <param name="length">Length of data to send. Use -1 to send the entire array.</param>
    /// <param name="channel">The channel to send the packet on.</param>
    /// <param name="sendType">The reliability type of the packet.</param>
    /// <returns>True if the packet was sent successfully, false otherwise.</returns>
    bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable);

    /// <summary>
    /// Reads an incoming P2P packet.
    /// </summary>
    /// <param name="buffer">The buffer to read the packet data into.</param>
    /// <param name="channel">The channel to read the packet from.</param>
    /// <returns>True if a packet was successfully read, false otherwise.</returns>
    bool ReadP2PPacket(P2PPacketBuffer buffer, PacketChannel channel = PacketChannel.Main);

    /// <summary>
    /// Gets data associated with a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="pchKey">The key of the data to retrieve.</param>
    /// <returns>The value associated with the key, or empty string if not found.</returns>
    string GetLobbyData(ID lobbyId, string pchKey);

    /// <summary>
    /// Sets data for a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="pchKey">The key of the data to set.</param>
    /// <param name="pchValue">The value to set.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool SetLobbyData(ID lobbyId, string pchKey, string pchValue);

    /// <summary>
    /// Deletes data from a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="pchKey">The key of the data to delete.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool DeleteLobbyData(ID lobbyId, string pchKey);

    /// <summary>
    /// Requests a refresh of lobby data from the server.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <returns>True if request was sent successfully, false otherwise.</returns>
    bool RequestLobbyData(ID lobbyId);

    /// <summary>
    /// Gets member data for a specific client in a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="clientId">The ID of the client.</param>
    /// <param name="pchKey">The key of the data to retrieve.</param>
    /// <returns>The value associated with the key, or empty string if not found.</returns>
    string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey);

    /// <summary>
    /// Sets member data for the current client in a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="pchKey">The key of the data to set.</param>
    /// <param name="pchValue">The value to set.</param>
    void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue);

    /// <summary>
    /// Gets the number of members currently in a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <returns>The number of members in the lobby.</returns>
    int GetNumLobbyMembers(ID lobbyId);

    /// <summary>
    /// Gets a lobby member by index.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="memberIndex">The index of the member to retrieve.</param>
    /// <returns>The ID of the member at the specified index.</returns>
    ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex);

    /// <summary>
    /// Gets the display name of a client.
    /// </summary>
    /// <param name="clientId">The ID of the client.</param>
    /// <returns>The client's display name.</returns>
    string GetMemberName(ID clientId);

    /// <summary>
    /// Sets the maximum number of members allowed in a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="maxMembers">The maximum number of members.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool SetLobbyMemberLimit(ID lobbyId, int maxMembers);

    /// <summary>
    /// Accepts a P2P session request from a client.
    /// </summary>
    /// <param name="clientId">The ID of the client requesting the session.</param>
    /// <returns>True if accepted successfully, false otherwise.</returns>
    bool AcceptP2PSessionWithUser(ID clientId);

    /// <summary>
    /// Closes a P2P session with a client.
    /// </summary>
    /// <param name="clientId">The ID of the client to close the session with.</param>
    /// <returns>True if closed successfully, false otherwise.</returns>
    bool CloseP2PSessionWithUser(ID clientId);

    /// <summary>
    /// Creates a new lobby with the specified maximum number of players.
    /// </summary>
    /// <param name="maxPlayers"></param>
    void CreateLobby(int maxPlayers);

    /// <summary>
    /// Joins a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby to join.</param>
    void JoinLobby(ID lobbyId);

    /// <summary>
    /// Leaves the current lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby to leave.</param>
    void LeaveLobby(ID lobbyId);

    /// <summary>
    /// Sets whether a lobby is joinable.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="bLobbyJoinable">True to make the lobby joinable, false otherwise.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool SetLobbyJoinable(ID lobbyId, bool bLobbyJoinable);

    /// <summary>
    /// Sets the type of a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="lobbyType">The lobby type to set.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool SetLobbyType(ID lobbyId, LobbyType lobbyType);

    /// <summary>
    /// Gets the owner of a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <returns>The ID of the lobby owner.</returns>
    ID GetLobbyOwner(ID lobbyId);
}