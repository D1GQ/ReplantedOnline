using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc(RpcType.PushBackZombie)]
internal sealed class PushBackZombieRpc : IRpcDispatcher<Zombie>
{
    /// <inheritdoc/>
    public void Send(Zombie zombie)
    {
        var zombieNetworked = zombie.GetNetworked();
        if (zombieNetworked == null) return;
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteNetworkObject(zombieNetworked);
        NetworkDispatcher.SendRpc(RpcType.PushBackZombie, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        if (sender.AmHost)
        {
            var zombieNetworked = packetReader.ReadNetworkObject<ZombieNetworked>();
            ArenaEvents.PushBackZombie(zombieNetworked._Zombie);
        }
    }
}