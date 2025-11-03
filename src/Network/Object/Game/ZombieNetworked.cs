using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked zombie entity in the game world, handling synchronization of zombie state
/// across connected clients including health, position, and follower relationships.
/// </summary>
internal class ZombieNetworked : NetworkClass
{
    /// <summary>
    /// Dictionary mapping zombie instances to their networked counterparts for easy lookup.
    /// </summary>
    internal static Dictionary<Zombie, ZombieNetworked> NetworkedZombies = [];

    /// <summary>
    /// The underlying zombie instance that this networked object represents.
    /// </summary>
    internal Zombie _Zombie;

    /// <summary>
    /// The type of zombie this networked object represents when spawning.
    /// </summary>
    internal ZombieType ZombieType;

    /// <summary>
    /// The unique identifier for this zombie instance when spawning.
    /// </summary>
    internal ZombieID ZombieID;

    /// <summary>
    /// The grid X coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridY;

    /// <summary>
    /// Called when the zombie is destroyed, cleans up the zombie from the networked zombies dictionary.
    /// </summary>
    public void OnDestroy()
    {
        if (_Zombie != null)
        {
            NetworkedZombies.Remove(_Zombie);
        }
    }

    /// <summary>
    /// Cooldown timer for synchronization to prevent excessive network traffic.
    /// </summary>
    private static float syncCooldown;

    /// <summary>
    /// Updates the zombie state and handles periodic synchronization.
    /// </summary>
    public void Update()
    {
        if (Time.time - syncCooldown >= 2f)
        {
            MarkDirty();
            syncCooldown = Time.time;
        }
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        switch (rpcId)
        {
            case 0:
                HandleSetFollowerZombieIdRpc(packetReader);
                break;
        }
    }

    /// <summary>
    /// Sends an RPC to set a follower zombie ID for this zombie.
    /// </summary>
    /// <param name="index">The index in the follower array to set</param>
    /// <param name="zombieID">The zombie ID to set as follower</param>
    internal void SendSetFollowerZombieIdRpc(int index, ZombieID zombieID)
    {
        var writer = PacketWriter.Get();
        writer.WriteInt(index);
        writer.WriteInt((int)zombieID);
        this.SendRpc(0, writer, false);
    }

    /// <summary>
    /// Handles the RPC for setting a follower zombie ID.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the follower data</param>
    [HideFromIl2Cpp]
    private void HandleSetFollowerZombieIdRpc(PacketReader packetReader)
    {
        var index = packetReader.ReadInt();
        var zombieId = (ZombieID)packetReader.ReadInt();
        _Zombie?.mFollowerZombieID[index] = zombieId;
    }

    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);
            packetWriter.WriteInt((int)ZombieID);
            packetWriter.WriteByte((byte)ZombieType);
            return;
        }

        packetWriter.WriteInt(_Zombie.mBodyHealth);
        packetWriter.WriteInt(_Zombie.mFlyingHealth);
        packetWriter.WriteInt(_Zombie.mHelmHealth);
        packetWriter.WriteInt(_Zombie.mShieldHealth);
        packetWriter.WriteFloat(_Zombie.mPosX);

        ClearDirtyBits();
    }

    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();
            ZombieID = (ZombieID)packetReader.ReadInt();
            ZombieType = (ZombieType)packetReader.ReadByte();

            _Zombie = SeedPacketSyncPatch.SpawnZombie(ZombieType, GridX, GridY, false);
            _Zombie.DataID = ZombieID;

            NetworkedZombies[_Zombie] = this;
            return;
        }

        _Zombie?.mBodyHealth = packetReader.ReadInt();
        _Zombie?.mFlyingHealth = packetReader.ReadInt();
        _Zombie?.mHelmHealth = packetReader.ReadInt();
        _Zombie?.mShieldHealth = packetReader.ReadInt();
        var PosX = packetReader.ReadFloat();
        LarpPos(PosX);

        ClearDirtyBits();
    }

    /// <summary>
    /// Token used to track and manage position interpolation coroutines.
    /// </summary>
    private object larpToken;

    /// <summary>
    /// Smoothly interpolates the zombie's position to the target position when distance threshold is exceeded.
    /// </summary>
    /// <param name="posX">The target X position to interpolate to</param>
    private void LarpPos(float posX)
    {
        if (_Zombie == null) return;

        var dis = _Zombie.mPosX - posX;

        if (Mathf.Abs(dis) > 35)
        {
            if (larpToken != null)
            {
                MelonCoroutines.Stop(larpToken);
            }

            larpToken = MelonCoroutines.Start(CoLarpPos(posX));
        }
    }

    /// <summary>
    /// Coroutine that smoothly interpolates the zombie's position over time.
    /// </summary>
    /// <param name="posX">The target X position to reach</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    [HideFromIl2Cpp]
    private IEnumerator CoLarpPos(float posX)
    {
        if (this == null || _Zombie == null)
        {
            yield break;
        }

        float startX = _Zombie.mPosX;
        float targetX = posX;
        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (this == null || _Zombie == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            _Zombie.mPosX = Mathf.Lerp(startX, targetX, t);

            yield return null;
        }

        _Zombie?.mPosX = targetX;

        larpToken = null;
    }
}