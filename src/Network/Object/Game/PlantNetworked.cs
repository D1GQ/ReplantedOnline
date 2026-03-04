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
/// Represents a networked plant entity in the game world, handling synchronization of plant state
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
    internal Zombie _Target { get; set; }

    internal static bool DoNotSyncDeath(Plant plant)
    {
        return plant.mSeedType == SeedType.Potatomine && plant.mState == PlantState.PotatoArmed;
    }

    private bool IsSuicide()
    {
        return SeedType is SeedType.Doomshroom or SeedType.Iceshroom or SeedType.Cherrybomb or SeedType.Jalapeno;
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

        if (IsSuicide())
        {
            SuicideUpdate();
            return;
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
    }

    private void SuicideUpdate()
    {
        if (!AmOwner)
        {
            _Plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    private void ChopperUpdate()
    {
        if (AmOwner)
        {
            string plantStateStr = _Plant.mState.ToString();

            if (_State?.ToString() != plantStateStr)
            {
                _State = plantStateStr;
                SendSetStateRpc(plantStateStr);
            }
        }
        else
        {
            if (_State is string stateStr)
            {
                if (Enum.TryParse(stateStr, out PlantState state))
                {
                    if (_Plant.mState != state)
                    {
                        _Plant.mState = state;
                        _Plant.mStateCountdown = int.MaxValue;

                        if (state == PlantState.ChomperBiting)
                        {
                            _Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE, CharacterAnimationTrack.Body, 30f, ReanimLoopType.PlayOnce);
                            _State = PlantState.ChomperBitingMissed.ToString();
                        }
                        else if (state == PlantState.ChomperDigesting)
                        {
                            _Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW, CharacterAnimationTrack.Body, 15f, ReanimLoopType.Loop);
                        }
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
            if (_Target is Zombie)
            {
                _Plant.MagnetShroomAttactItem(null); // MagnetshroomPlantPatch.cs will get the target
            }
        }
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

    internal void SendDieRpc()
    {
        if (!dead)
        {
            dead = true;
            SendNetworkClassRpc((byte)PlantRpcs.Die);
            DespawnAndDestroy();
        }
    }

    private void HandleDieRpc()
    {
        dead = true;
        _Plant.DieOriginal();
    }

    internal void SendSquashTargetRpc(Zombie target)
    {
        if (SeedType != SeedType.Squash) return;

        if (_State is not PlantState.DoingSpecial)
        {
            _State = PlantState.DoingSpecial;
            var writer = PacketWriter.Get();
            writer.WriteNetworkObject(target.GetNetworked<ZombieNetworked>());
            SendNetworkClassRpc((byte)PlantRpcs.SquashTarget, writer);
            writer.Recycle();
        }
    }

    private void HandleSquashTargetRpc(Zombie target)
    {
        if (SeedType != SeedType.Squash) return;

        if (_State is not PlantState.DoingSpecial)
        {
            _State = PlantState.DoingSpecial;
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
        writer.WriteNetworkObject(theTargetZombie.GetNetworked<ZombieNetworked>());
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
        if (_Target != target)
        {
            _Target = target;
            var writer = PacketWriter.Get();
            writer.WriteNetworkObject(target.GetNetworked<ZombieNetworked>());
            SendNetworkClassRpc((byte)PlantRpcs.SetZombieTarget, writer);
            writer.Recycle();
        }
    }

    private void HandleSetZombieTargetRpc(Zombie target)
    {
        _Target = target;
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
        _State = state;
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
                    HandleDieRpc();
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
                    HandleSetZombieTargetRpc(target._Zombie);
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

            _Plant = Utils.SpawnPlant(SeedType, ImitaterType, GridX, GridY, false);
            _Plant.AddNetworkedLookup(this);
            AnimationControllerNetworked.Init(_Plant.mController.AnimationController);

            gameObject.name = $"{Enum.GetName(_Plant.mSeedType)}_Plant ({NetworkId})";
        }
    }
}