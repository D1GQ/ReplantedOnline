using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus;
using System.Collections;
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
    internal Vector2 boardGridPos;
    // Type of coin (e.g., silver, gold, etc.)
    internal CoinType theCoinType;
    // Motion behavior of the coin (e.g., falling, bouncing, etc.)
    internal CoinMotion theCoinMotion;

    public void Update()
    {
        if (AmOwner && !Despawning)
        {
            if ((HasSpawned && coin == null) || (coin.mDead || coin.WasCollected))
            {
                Despawning = true;
                MelonCoroutines.Start(CoDespawn());
            }
        }
    }

    private bool Despawning;
    private IEnumerator CoDespawn()
    {
        // wait for desync
        yield return new WaitForSeconds(3f);
        Despawn();
        Destroy(gameObject);
    }

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
                coin.CollectOriginal(1, false);
                break;
        }
    }

    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Only send full state during initial spawn
            packetWriter.WriteInt((int)boardGridPos.x);
            packetWriter.WriteInt((int)boardGridPos.y);
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
            var posX = packetReader.ReadInt();
            var posY = packetReader.ReadInt();
            boardGridPos = new(posX, posY);
            theCoinType = (CoinType)packetReader.ReadByte();
            theCoinMotion = (CoinMotion)packetReader.ReadByte();

            // Recreate the actual coin object in the game world using the original method
            coin = Instances.GameplayActivity.Board.AddCoinOriginal(boardGridPos.x, boardGridPos.y, theCoinType, theCoinMotion);

            // Register this network controller with the newly created coin
            NetworkedCoinControllers[coin] = this;
        }
    }
}