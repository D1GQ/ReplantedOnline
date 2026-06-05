using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Utilities.Modded;

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
    public void Handle(ReloadedClientData sender, PacketReader packetReader)
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