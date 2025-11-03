using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

internal class ZombieNetworked : NetworkClass
{
    internal static Dictionary<Zombie, ZombieNetworked> NetworkedZombies = [];

    internal Zombie _Zombie;
    internal ZombieType ZombieType;
    internal ZombieID ZombieID;
    internal ZombieID RefZombieID;
    internal int GridX;
    internal int GridY;

    public void OnDestroy()
    {
        if (_Zombie != null)
        {
            NetworkedZombies.Remove(_Zombie);
        }
    }

    private static float syncCooldown;
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

    internal void SendSetFollowerZombieIdRpc(int index, ZombieID zombieID)
    {
        var writer = PacketWriter.Get();
        writer.WriteInt(index);
        writer.WriteInt((int)zombieID);
        this.SendRpc(0, writer, false);
    }

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

    private object larpToken;
    private void LarpPos(float posX)
    {
        if (_Zombie == null) return;

        var dis = _Zombie.mPosX - posX;

        if (Mathf.Abs(dis) > 100)
        {
            if (larpToken != null)
            {
                MelonCoroutines.Stop(larpToken);
            }

            larpToken = MelonCoroutines.Start(CoLarpPos(posX));
        }
    }

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
