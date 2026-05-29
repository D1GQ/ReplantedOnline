using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc(RpcType.StartMower)]
internal sealed class StartMowerRpc : IRpcDispatcher<LawnMower>
{
    /// <inheritdoc/>
    public void Send(LawnMower lawnMower)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WritePackedInt(lawnMower.DataID);
        NetworkDispatcher.SendRpc(RpcType.StartMower, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader)
    {
        if (sender.Team == PlayerTeam.Plants)
        {
            var id = packetReader.ReadPackedInt();
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