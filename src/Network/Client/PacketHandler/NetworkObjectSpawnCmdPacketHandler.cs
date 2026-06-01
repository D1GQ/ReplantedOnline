using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Network.Routing.Packet.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler(PacketHandlerType.NetworkObjectSpawnCmd)]
internal sealed class NetworkObjectSpawnCmdPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!ReloadedLobby.AmLobbyHost()) return;

        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Instance.Deserialize(packet);

        if (Validate(sender, message, packet))
        {
            NetworkDispatcher.SendPacket(PacketWriter.Get(packetReader.GetByteBuffer()), PacketHandlerType.NetworkObjectSpawn, PacketChannel.Main, true, sender.ClientId);
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSpawnCmdPacketHandler), $"{sender.Name}: Is requesting to spawn network object {message.NetworkId}, Prefab: {message.PrefabId}");
        }
        else
        {
            NetworkDispatcher.SendRejectNetworkObject(message.NetworkId, sender.ClientId);
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
