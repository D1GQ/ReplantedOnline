using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Patches.Gameplay.Versus.Plants;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked plant on the board, handling synchronization of plant state
/// across connected clients including plant type, position, and imitater type.
/// </summary>
internal sealed class PlantNetworked : NetworkObject
{
    internal enum PlantRpcs
    {
        Die,
        SquashTarget,
        SquishPlant,
        Fire,
        SetZombieTarget,
        SetState
    }

    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Zombie Target { get; set; }

    internal static bool DoNotSyncDeath(Plant plant, int doSpecialCountdown)
    {
        if (plant == null) return false;

        switch (plant.mSeedType)
        {
            case SeedType.Potatomine:
                return plant.GetNetworked()?.Target != null;
            case SeedType.Doomshroom:
            case SeedType.Iceshroom:
            case SeedType.Cherrybomb:
            case SeedType.Jalapeno:
                {
                    if (doSpecialCountdown == 0)
                    {
                        plant.GetNetworked().dead = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            default:
                return false;
        }
    }

    /// <summary>
    /// Represents the networked animation controller used to synchronize animation states across multiple clients.
    /// </summary>
    internal AnimationControllerNetworked AnimationControllerNetworked;

    /// <summary>
    /// The underlying plant instance that this networked object represents.
    /// </summary>
    internal Plant _Plant;

    /// <summary>
    /// The type of seed used to plant this plant when spawning.
    /// </summary>
    internal SeedType SeedType;

    /// <summary>
    /// The imitater type if this plant was created by an Imitater seed when spawning.
    /// </summary>
    internal SeedType ImitaterType;

    /// <summary>
    /// The grid X coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridY;

    [HideFromIl2Cpp]
    protected override void OnClone(RuntimePrefab prefab)
    {
        AnimationControllerNetworked = gameObject.AddComponent<AnimationControllerNetworked>();
        AddChild(AnimationControllerNetworked);

        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public override string GetObjectName()
    {
        return $"{Enum.GetName(_Plant.mSeedType)}Plant ({NetworkId})";
    }

    public void OnDestroy()
    {
        if (_Plant != null)
        {
            _Plant.RemoveNetworkedLookup();

            if (!dead && !_Plant.mDead)
            {
                _Plant.DieOriginal();
            }
        }
    }

    private bool dead;
    public void Update()
    {
        if (!IsOnNetwork) return;

        // Remove Potatomine off network if blown up
        if (AmOwner)
        {
            if (_Plant == null)
            {
                if (SeedType == SeedType.Potatomine)
                {
                    DespawnAndDestroy();
                    return;
                }
            }
        }

        if (_Plant == null) return;

        if (_Plant.mDead)
        {
            _Plant.RemoveNetworkedLookup();
            _Plant = null;
            return;
        }

        if (!AmOwner)
        {
            if (!dead)
            {
                if (_Plant.mPlantHealth < 25)
                {
                    _Plant.mPlantHealth = 25;
                }
            }
        }

        switch (SeedType)
        {
            case SeedType.Chomper:
                ChopperUpdate();
                break;
            case SeedType.Magnetshroom:
                MagnetShroomUpdate();
                break;
        }

        NormalUpdate();
    }

    private float _syncCooldown = 2f;
    private void NormalUpdate()
    {
        if (_Plant == null) return;

        if (AmOwner)
        {
            if (!dead && !_Plant.mDead)
            {
                if (_syncCooldown <= 0f && lastSyncPlantHealth != _Plant.mPlantHealth)
                {
                    MarkDirty();
                    _syncCooldown = 1f;
                    lastSyncPlantHealth = _Plant.mPlantHealth;
                }
                _syncCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!dead && !_Plant.mDead)
            {
                if (lastSyncPlantHealth != null)
                {
                    _Plant.mPlantHealth = lastSyncPlantHealth.Value;
                }
            }
        }
    }

    private void ChopperUpdate()
    {
        if (AmOwner)
        {
            string plantStateStr = _Plant.mState.ToString();

            if (State?.ToString() != plantStateStr)
            {
                State = plantStateStr;
                SendSetStateRpc(plantStateStr);
            }
        }
        else
        {
            if (State is string stateStr)
            {
                if (Enum.TryParse(stateStr, out PlantState state))
                {
                    if (_Plant.mState != state)
                    {
                        if (state == PlantState.ChomperBiting)
                        {
                            _Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE, CharacterAnimationTrack.Body, 30f, ReanimLoopType.PlayOnce);
                            State = PlantState.ChomperBitingMissed.ToString();
                        }
                        else if (state == PlantState.ChomperDigesting)
                        {
                            _Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW, CharacterAnimationTrack.Body, 15f, ReanimLoopType.Loop);
                        }
                        else if (state == PlantState.ChomperSwallowing)
                        {
                            _Plant.mState = PlantState.ChomperDigesting;
                            _Plant.mStateCountdown = 0;
                            return;
                        }

                        _Plant.mState = state;
                        _Plant.mStateCountdown = int.MaxValue;
                    }
                    else if (state == PlantState.Ready)
                    {
                        if (!_Plant.mController.IsAnimationPlaying(Animations.CHOMPER_IDLE))
                        {
                            _Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_IDLE, CharacterAnimationTrack.Body, 10.26f, ReanimLoopType.Loop);
                        }
                    }
                }
            }
        }
    }

    private void MagnetShroomUpdate()
    {
        if (!AmOwner)
        {
            if (Target != null)
            {
                _Plant.MagnetShroomAttactItemOriginal(Target);
                Target = null;
            }
        }
    }

    internal void SendDieRpc()
    {
        if (!dead)
        {
            dead = true;
            var writer = PacketWriter.Get();
            writer.WriteInt(_Plant.mDoSpecialCountdown);
            SendNetworkClassRpc((byte)PlantRpcs.Die, writer);
            DespawnAndDestroy();
        }
    }

    private void HandleDieRpc(int doSpecialCountdown)
    {
        if (DoNotSyncDeath(_Plant, doSpecialCountdown)) return;

        dead = true;
        _Plant?.DieOriginal();
    }

    internal void SendSquashTargetRpc(Zombie target)
    {
        if (SeedType != SeedType.Squash) return;

        if (State is not PlantState.DoingSpecial)
        {
            State = PlantState.DoingSpecial;
            var writer = PacketWriter.Get();
            writer.WriteNetworkObject(target.GetNetworked());
            SendNetworkClassRpc((byte)PlantRpcs.SquashTarget, writer);
            writer.Recycle();
        }
    }

    private void HandleSquashTargetRpc(Zombie target)
    {
        if (SeedType != SeedType.Squash) return;

        if (State is not PlantState.DoingSpecial)
        {
            State = PlantState.DoingSpecial;
            _Plant.mTargetZombieID = target.DataID;
            _Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
            _Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
            _Plant.mState = PlantState.SquashLook;
        }
    }

    internal void SendSquashPlantRpc()
    {
        SendNetworkClassRpc((byte)PlantRpcs.SquishPlant);
    }

    private void HandleSquashPlantRpc()
    {
        _Plant.SquishOriginal();
    }

    internal void SendFireRpc(Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        var writer = PacketWriter.Get();
        writer.WriteNetworkObject(theTargetZombie.GetNetworked());
        writer.WriteInt(theRow);
        writer.WriteInt((int)thePlantWeapon);
        SendNetworkClassRpc((byte)PlantRpcs.Fire, writer);
        writer.Recycle();
    }

    private void HandleFireRpc(Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        _Plant.FireOriginal(theTargetZombie, theRow, thePlantWeapon);
    }

    internal void SendSetZombieTargetRpc(Zombie target)
    {
        var writer = PacketWriter.Get();
        writer.WriteNetworkObject(target?.GetNetworked());
        SendNetworkClassRpc((byte)PlantRpcs.SetZombieTarget, writer);
        writer.Recycle();
    }

    private void HandleSetZombieTargetRpc(Zombie target)
    {
        Target = target;
    }

    internal void SendSetStateRpc(string state)
    {
        var writer = PacketWriter.Get();
        writer.WriteString(state);
        SendNetworkClassRpc((byte)PlantRpcs.SetState, writer);
        writer.Recycle();
    }

    private void HandleSetStateRpc(string state)
    {
        State = state;
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(NetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.ClientId != OwnerId) return;

        var rpc = (PlantRpcs)rpcId;
        switch (rpc)
        {
            case PlantRpcs.Die:
                {
                    var doSpecialCountdown = packetReader.ReadInt();
                    HandleDieRpc(doSpecialCountdown);
                }
                break;
            case PlantRpcs.SquashTarget:
                {
                    var target = packetReader.ReadNetworkObject<ZombieNetworked>();
                    HandleSquashTargetRpc(target._Zombie);
                }
                break;
            case PlantRpcs.Fire:
                {
                    var target = packetReader.ReadNetworkObject<ZombieNetworked>();
                    var row = packetReader.ReadInt();
                    var plantWeapon = (PlantWeapon)packetReader.ReadInt();
                    HandleFireRpc(target._Zombie, row, plantWeapon);
                }
                break;
            case PlantRpcs.SetZombieTarget:
                {
                    var target = packetReader.ReadNetworkObject<ZombieNetworked>();
                    HandleSetZombieTargetRpc(target?._Zombie);
                }
                break;
            case PlantRpcs.SquishPlant:
                {
                    HandleSquashPlantRpc();
                }
                break;
            case PlantRpcs.SetState:
                {
                    var state = packetReader.ReadString();
                    HandleSetStateRpc(state);
                }
                break;
        }
    }

    /// <summary>
    /// Serializes the plant state for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write data to</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);
            packetWriter.WriteInt((int)SeedType);
            packetWriter.WriteInt((int)ImitaterType);
        }

        packetWriter.WriteInt(_Plant.mPlantHealth);

        ClearDirtyBits();
    }

    /// <summary>
    /// Deserializes the plant state from network data and spawns the plant instance.
    /// </summary>
    /// <param name="packetReader">The packet reader to read data from</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();
            SeedType = (SeedType)packetReader.ReadInt();
            ImitaterType = (SeedType)packetReader.ReadInt();

            _Plant = SeedPacketDefinitions.SpawnPlant(SeedType, ImitaterType, GridX, GridY, false);
            _Plant.AddNetworkedLookup(this);
            AnimationControllerNetworked.Init(_Plant.mController.AnimationController);
        }

        lastSyncPlantHealth = Math.Max(packetReader.ReadInt(), 5);
    }

    internal int? lastSyncPlantHealth;
}