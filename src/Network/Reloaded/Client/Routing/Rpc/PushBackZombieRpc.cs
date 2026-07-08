using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

[RegisterRpc(RpcType.PushBackZombie)]
internal sealed class PushBackZombieRpc : IRpcMessage<Zombie>
{
    /// <inheritdoc/>
    public void Send(Zombie zombie)
    {
        var zombieNetworked = zombie.GetNetworked();
        if (zombieNetworked == null) return;
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteNetworkObject(zombieNetworked);
        NetworkManager.Packet<RpcPacket>.Singleton.Send(RpcType.PushBackZombie, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader)
    {
        if (sender.AmHost)
        {
            var zombieNetworked = packetReader.ReadNetworkObject<ZombieNetworked>();
            if (zombieNetworked != null)
            {
                ArenaEvents.PushBackZombie(zombieNetworked.Zombie!);
            }
        }
    }
}