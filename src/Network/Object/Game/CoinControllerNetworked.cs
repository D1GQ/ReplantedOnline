using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked coin controller that manages synchronization of coin entities
/// across connected clients, including coin collection, despawning, and state management.
/// </summary>
internal class CoinControllerNetworked : NetworkClass
{
    /// <summary>
    /// Global dictionary tracking all active coins and their associated network controllers.
    /// Used to find network controllers when coins need to send RPCs or be synchronized.
    /// </summary>
    internal static Dictionary<Coin, CoinControllerNetworked> NetworkedCoinControllers = [];

    /// <summary>
    /// The underlying coin instance that this networked controller manages.
    /// </summary>
    internal Coin _Coin;

    /// <summary>
    /// Original spawn position of the coin on the board grid when spawning.
    /// </summary>
    internal Vector2 BoardGridPos;

    /// <summary>
    /// Type of coin when spawning.
    /// </summary>
    internal CoinType TheCoinType;

    /// <summary>
    /// Motion behavior of the coin when spawning.
    /// </summary>
    internal CoinMotion TheCoinMotion;

    public void Start()
    {
        // End game
        if (_Coin?.mType is CoinType.VersusTrophyPlant or CoinType.VersusTrophyZombie)
        {
            NetLobby.ResetLobby();
        }
    }

    /// <summary>
    /// Updates the coin state each frame, handling automatic despawning of collected or dead coins.
    /// </summary>
    public void Update()
    {
        if (AmOwner && !IsDespawning)
        {
            if ((HasSpawned && _Coin == null) || (_Coin.mDead || _Coin.WasCollected))
            {
                DespawnAndDestroyWithDelay(3f);
            }
        }
    }

    /// <summary>
    /// Called when the coin controller is destroyed, cleans up the coin from the networked controllers dictionary.
    /// </summary>
    public void OnDestroy()
    {
        if (_Coin != null)
        {
            NetworkedCoinControllers.Remove(_Coin);
        }
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        switch (rpcId)
        {
            case 0:
                HandleCollectRpc();
                break;
            case 1:
                HandleDieRpc();
                break;
        }
    }

    internal void SendCollectRpc()
    {
        this.SendRpc(0, false);
    }

    private void HandleCollectRpc()
    {
        _Coin.CollectOriginal(1, false);
    }

    internal void SendDieRpc()
    {
        this.SendRpc(1, false);
    }

    [HideFromIl2Cpp]
    private void HandleDieRpc()
    {
        _Coin.DieOriginal();
    }

    /// <summary>
    /// Serializes the coin state for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write data to</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Only send full state during initial spawn
            packetWriter.WriteVector2(BoardGridPos);
            packetWriter.WriteByte((byte)TheCoinType);
            packetWriter.WriteByte((byte)TheCoinMotion);
        }
    }

    /// <summary>
    /// Deserializes the coin state from network data and recreates the coin in the game world.
    /// </summary>
    /// <param name="packetReader">The packet reader to read data from</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Only process full state during initial spawn
            BoardGridPos = packetReader.ReadVector2();
            TheCoinType = (CoinType)packetReader.ReadByte();
            TheCoinMotion = (CoinMotion)packetReader.ReadByte();

            // Recreate the actual coin object in the game world using the original method
            _Coin = Instances.GameplayActivity.Board.AddCoinOriginal(BoardGridPos.x, BoardGridPos.y, TheCoinType, TheCoinMotion);

            // Register this network controller with the newly created coin
            NetworkedCoinControllers[_Coin] = this;
        }
    }
}