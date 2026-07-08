using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.NetworkObjectSpawnCmd)]
internal sealed class NetworkObjectSpawnCmdPacket : IPacketMessage<NetworkObject>
{
    /// <inheritdoc/>
    public void Send(NetworkObject networkObj)
    {
        if (ReloadedLobby.AmLobbyHost()) return;

        PacketWriter packetWriter = PacketWriter.Get();
        Message<NetworkObjectSpawnMessage>.Singleton.Serialize(packetWriter, networkObj);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Spawn Network Object Request with ID: {networkObj.NetworkId} to host");
        NetworkManager.SendPacketTo(ReloadedLobby.LobbyData!.HostId, packetWriter, PacketType.NetworkObjectSpawnCmd, PacketChannel.Main);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!ReloadedLobby.AmLobbyHost()) return;

        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Singleton.Deserialize(packet);

        try
        {
            if (Validate(sender, message, packet))
            {
                NetworkManager.SendPacket(packetReader, PacketType.NetworkObjectSpawn, PacketChannel.Main, true, sender.ClientId);
                ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSpawnCmdPacket), $"{sender.Name}: Is requesting to spawn network object {message.NetworkId}, Prefab: {message.PrefabId}");
            }
            else
            {
                NetworkManager.Packet<NetworkObjectRejectPacket>.Singleton.Send(message.NetworkId, sender.ClientId);
            }
        }
        finally
        {
            packet.Recycle();
        }
    }

    private static bool Validate(ReloadedClientData sender, NetworkObjectSpawnMessage message, PacketReader packet)
    {
        if (sender.GetClientIndex() != message.NetworkId.ClientIndex)
        {
            return false;
        }

        if (NetworkObject.NetworkPrefabs.TryGetValue(message.PrefabId, out var prefab))
        {
            if (sender.Team is PlayerTeam.Plants)
            {
                if (prefab is ZombieNetworked)
                {
                    ZombieType zombieType = packet.ReadEnum<ZombieType>();
                    if (zombieType is not (ZombieType.Imp or ZombieType.BackupDancer))
                    {
                        return false;
                    }
                }
            }
            else if (sender.Team is PlayerTeam.Zombies)
            {
                if (prefab is PlantNetworked)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
