using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

internal class CoinControllerNetworked : NetworkClass
{
    /// Global dictionary tracking all active coins and their associated network controllers.
    /// Used to find network controllers when coins need to send RPCs or be synchronized.
    internal static Dictionary<Coin, CoinControllerNetworked> NetworkedCoinControllers = [];

    // Local coin instance this controller manages
    internal Coin coin;
    // Original spawn position of the coin on the board
    internal Vector2 boardPos;
    // Type of coin (e.g., silver, gold, etc.)
    internal CoinType theCoinType;
    // Motion behavior of the coin (e.g., falling, bouncing, etc.)
    internal CoinMotion theCoinMotion;

    public void OnDestroy()
    {
        if (coin != null)
        {
            NetworkedCoinControllers.Remove(coin);
        }
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        switch (rpcId)
        {
            case 0:
                // RPC ID 0: Coin collection notification
                // When receiving collection from another player, collect as player 1 (opposite player)
                coin.OriginalCoinCollect(1, false);
                break;
        }
    }

    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Only send full state during initial spawn
            packetWriter.WriteVector2(boardPos);
            packetWriter.WriteByte((byte)theCoinType);
            packetWriter.WriteByte((byte)theCoinMotion);
        }
    }

    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Only process full state during initial spawn
            boardPos = packetReader.ReadVector2();
            theCoinType = (CoinType)packetReader.ReadByte();
            theCoinMotion = (CoinMotion)packetReader.ReadByte();

            // Recreate the actual coin object in the game world using the original method
            coin = Instances.GameplayActivity.Board.BoardAddCoinOriginal(boardPos.x, boardPos.y, theCoinType, theCoinMotion);

            // Register this network controller with the newly created coin
            NetworkedCoinControllers[coin] = this;
        }
    }
}