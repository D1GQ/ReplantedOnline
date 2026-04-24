using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc]
internal sealed class StartMowerRpc : IRpcDispatcher<LawnMower>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.StartMower;

    /// <inheritdoc/>
    public void Send(LawnMower lawnMower)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(lawnMower.DataID);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        if (sender.Team == PlayerTeam.Plants)
        {
            var id = packetReader.ReadInt();
            var lawnMower = Instances.GameplayActivity.Board.m_lawnMowers.DataArrayGet(id);

            try
            {
                // Only want to start the mower so give a null ref
                lawnMower?.MowZombieOriginal(null);
            }
            catch { }
        }
    }
}