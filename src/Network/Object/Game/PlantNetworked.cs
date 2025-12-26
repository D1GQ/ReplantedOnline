using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using UnityEngine;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked plant entity in the game world, handling synchronization of plant state
/// across connected clients including plant type, position, and imitater type.
/// </summary>
internal sealed class PlantNetworked : NetworkClass
{
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
    }

    private bool dead;
    public void Update()
    {
        if (!IsOnNetwork) return;
        if (_Plant == null) return;

        if (AmOwner)
        {
            if (!dead && _Plant.mDead)
            {
                DespawnAndDestroy();
            }
        }
        else
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

    private void MagnetShroomUpdate()
    {
        if (!AmOwner)
        {
            if (_State is Zombie)
            {
                _Plant.MagnetShroomAttactItem(null); // MagnetshroomPlantPatch will get the target
            }
        }
    }

    public void OnDestroy()
    {
        _Plant.RemoveNetworkedLookup();

        if (!dead)
        {
            _Plant.DieOriginal();
        }
    }

    internal void SendDieRpc()
    {
        if (!dead)
        {
            dead = true;
            this.SendRpc(0);
            DespawnAndDestroy();
        }
    }

    private void HandleDieRpc()
    {
        dead = true;
        _Plant.DieOriginal();
    }

    internal void SendSquashRpc(Zombie target)
    {
        if (_State is not PlantState.DoingSpecial)
        {
            _State = PlantState.DoingSpecial;
            var writer = PacketWriter.Get();
            writer.WriteNetworkClass(target.GetNetworked<ZombieNetworked>());
            this.SendRpc(1, writer);
            writer.Recycle();
        }
    }

    private void HandleSquashRpc(Zombie target)
    {
        if (_State is not PlantState.DoingSpecial)
        {
            _State = PlantState.DoingSpecial;
            _Plant.mTargetZombieID = target.DataID;
            _Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
            _Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
            _Plant.mState = PlantState.SquashLook;
        }
    }

    internal void SendSetZombieTargetRpc(Zombie target)
    {
        if (_State != target)
        {
            _State = target;
            var writer = PacketWriter.Get();
            writer.WriteNetworkClass(target.GetNetworked<ZombieNetworked>());
            this.SendRpc(2, writer);
            writer.Recycle();
        }
    }

    private void HandleSetZombieTargetRpc(Zombie target)
    {
        _State = target;
    }

    private void SendSetStateRpc(string state)
    {
        var writer = PacketWriter.Get();
        writer.WriteString(state);
        this.SendRpc(3, writer);
        writer.Recycle();
    }

    private void HandleSetUpdateStateRpc(string state)
    {
        _State = state;
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.SteamId != OwnerId) return;

        switch (rpcId)
        {
            case 0:
                {
                    HandleDieRpc();
                }
                break;
            case 1:
                {
                    var target = (ZombieNetworked)packetReader.ReadNetworkClass();
                    HandleSquashRpc(target._Zombie);
                }
                break;
            case 2:
                {
                    var target = (ZombieNetworked)packetReader.ReadNetworkClass();
                    HandleSetZombieTargetRpc(target._Zombie);
                }
                break;
            case 3:
                {
                    var state = packetReader.ReadString();
                    HandleSetUpdateStateRpc(state);
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