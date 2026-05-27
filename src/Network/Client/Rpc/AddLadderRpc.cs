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

[RegisterRpc(RpcType.AddLadder)]
internal sealed class AddLadderRpc : IRpcDispatcher<int, int>
{
    /// <inheritdoc/>
    public void Send(int theGridX, int theGridY)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(theGridX);
        packetWriter.WriteInt(theGridY);
        NetworkDispatcher.SendRpc(RpcType.AddLadder, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader)
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