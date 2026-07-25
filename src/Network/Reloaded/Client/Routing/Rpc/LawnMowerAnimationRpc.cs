using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

[RegisterRpc(RpcType.LawnMowerAnimation)]
internal sealed class LawnMowerAnimationRpc : IRpcMessage<LawnMower>
{
    /// <inheritdoc/>
    public void Send(LawnMower lawnMower)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WritePackedInt(lawnMower.DataID);
        NetworkManager.Packet<RpcPacket>.Singleton.Send(RpcType.LawnMowerAnimation, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader)
    {
        if (sender.Team == PlayerTeam.Plants)
        {
            var id = packetReader.ReadPackedInt();
            var lawnMower = Instances.GameplayActivity.Board.m_lawnMowers.DataArrayGet(id);

            try
            {
                lawnMower?.MowZombieOriginal(null);
            }
            catch { }
        }
    }
}