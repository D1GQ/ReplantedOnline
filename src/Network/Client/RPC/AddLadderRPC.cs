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
internal sealed class AddLadderRpc : IRpcDispatcher<int, int>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.AddLadder;

    /// <inheritdoc/>
    public void Send(int theGridX, int theGridY)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(theGridX);
        packetWriter.WriteInt(theGridY);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        if (sender.Team is PlayerTeam.Plants)
        {
            int gridX = packetReader.ReadInt();
            int gridY = packetReader.ReadInt();
            if (Instances.GameplayActivity.Board.GetTopPlantAt(gridX, gridY, PlantPriority.Any) != null &&
                Instances.GameplayActivity.Board.GetLadderAt(gridX, gridY) == null)
            {
                Instances.GameplayActivity.Board.AddALadderOriginal(gridX, gridY);
            }
        }
    }
}