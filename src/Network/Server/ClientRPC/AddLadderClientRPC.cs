using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class AddLadderClientRPC : IClientRPC
{
    /// <inheritdoc/>
    public ClientRpcType Rpc => ClientRpcType.AddLadder;

    internal static void Send(int theGridX, int theGridY)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(theGridX);
        packetWriter.WriteInt(theGridY);
        NetworkDispatcher.SendRpc(ClientRpcType.AddLadder, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
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