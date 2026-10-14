using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gargantuar)]
internal sealed class GargantuarNetworkComponent : ZombieNetworkComponent
{
    private enum GargantuarMsgs : byte
    {
        ImpJumpOut
    }

    internal override void OnInit(Zombie zombie)
    {
        var groundType = zombie.mBoard.mPlantRow[zombie.mRow];
        if (groundType == PlantRowType.Pool)
        {
            zombie.mHasObject = false;
            zombie.mController.AssignRenderGroupToPrefix("Zombie_imp", -1);
            zombie.mController.AssignRenderGroupToTrack("Zombie_imp_track", -1);
        }
    }

    private int GetPoleSkinIndex()
    {
        if (Net.Zombie == null)
            return 0;

        string codeName = Net.Zombie.mController.GetImageOverride(Animations.GARGANTUAR_POLE_OBJECT.Slot);
        var skinData = Net.Zombie.mController.m_skinController.m_skinSets.First();

        foreach (var skin in skinData.Skins)
        {
            if (skin.SlotTrackName != Animations.GARGANTUAR_POLE_OBJECT.Slot)
                continue;

            for (int i = 0; i < skin.Entries.Count; i++)
            {
                if (skin.Entries[i].CodeName == codeName)
                {
                    return i;
                }
            }

            break;
        }

        return 0;
    }

    internal void SendImpJumpOutMessage()
    {
        if (Net.Zombie == null)
            return;

        var imp = SeedPacketDefinitions.SpawnZombie(ZombieType.Imp, 20, Net.Zombie.mRow, SpawnType.None, true);
        int waitForTime = Mathf.FloorToInt(VersusState.VersusTimeSynced + 1f);
        Net.StartCoroutine(CoImpJumpOut(imp.Zombie, waitForTime));
        Net.StartCoroutine(CoTimeoutImpJumpOut(imp.ZombieNetworked!));
        SendMessage(GargantuarMsgs.ImpJumpOut, imp.ZombieNetworked!.NetworkId.Id, waitForTime);
    }

    [ObjectMessageHandler(GargantuarMsgs.ImpJumpOut)]
    private void HandleImpJumpOutMessage(uint networkId, int waitForTime)
    {
        if (Net.Zombie == null)
            return;

        NetworkIdentifier impNetworkIdentifier = NetworkIdentifier.Get(networkId);
        Net.StartCoroutine(CoWaitForImp(impNetworkIdentifier, waitForTime));
    }

    private IEnumerator CoWaitForImp(NetworkIdentifier impNetworkIdentifier, int waitForTime)
    {
        Zombie? imp = null;
        while (imp == null)
        {
            if (ReloadedLobby.LobbyData == null)
            {
                yield break;
            }

            // Try to get spawned imp
            if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(impNetworkIdentifier, out var networkObject))
            {
                if (networkObject is ZombieNetworked zombieNetworked)
                {
                    imp = zombieNetworked.Zombie;
                }

                break;
            }

            yield return null;
        }

        yield return CoImpJumpOut(imp, waitForTime);
    }

    private IEnumerator CoImpJumpOut(Zombie? imp, int waitForTime)
    {
        Zombie? gargantuar = Net.Zombie;
        if (imp == null || gargantuar == null)
            yield break;

        imp.mController.gameObject.SetActive(false);

        imp.mRow = gargantuar.mRow;
        imp.mRenderOrder = gargantuar.RenderOrder + 100;
        imp.mVariant = false;

        // Set IZombie hp
        imp.mBodyMaxHealth = 70;
        imp.mBodyHealth = 70;

        while (VersusState.VersusTimeSynced < waitForTime)
        {
            yield return null;
        }

        while (gargantuar.mZombiePhase != ZombiePhase.ZombieNormal)
        {
            yield return null;
        }

        imp.mController.gameObject.SetActive(true);

        // Setup visuals
        gargantuar.mController.AssignRenderGroupToPrefix("Zombie_imp", -1);
        gargantuar.mController.AssignRenderGroupToTrack("Zombie_imp_track", -1);

        // Set states
        imp.mZombiePhase = ZombiePhase.ImpGettingThrown;
        imp.mChilledCounter = gargantuar.mChilledCounter;

        // Set movement
        imp.mVelZ = 0.5f;
        imp.mAltitude = gargantuar.mAltitude + 140f;
        imp.mPosX = gargantuar.mPosX + 15f;

        // Play imp flying animation with sound
        imp.PlayZombieReanim(Animations.IMP_FLYING.Anim, ReanimLoopType.Loop, 3, Animations.IMP_FLYING.AnimRate);
        imp.UpdateReanim();
        Instances.GameplayActivity?.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_IMP);

        while (imp.mZombiePhase != ZombiePhase.ZombieNormal)
        {
            yield return null;
        }

        while (imp != null && !imp.IsDeadOrDying())
        {
            // Set IZombie speed
            if (imp.mVelX != 0.9f)
            {
                imp.mVelX = 0.9f;
                imp.UpdateAnimSpeed();
            }

            yield return null;
        }
    }

    private IEnumerator CoTimeoutImpJumpOut(ZombieNetworked impNetworked)
    {
        if (!impNetworked.AmOwner)
            yield break;

        var gargantuarNetworked = Net;
        while (gargantuarNetworked.IsOnNetwork && !gargantuarNetworked.Dying)
        {
            if (impNetworked.Zombie?.mController == null ||
                impNetworked.Zombie.mController.gameObject.active)
            {
                yield break;
            }

            yield return null;
        }

        impNetworked.DespawnAndDestroy();
    }

    private void SetPoleSkinByIndex(int index)
    {
        if (Net.Zombie == null)
            return;

        var skinData = Net.Zombie.mController.m_skinController.m_skinSets.First();

        foreach (var skin in skinData.Skins)
        {
            if (skin.SlotTrackName != Animations.GARGANTUAR_POLE_OBJECT.Slot)
                continue;

            if (index >= skin.Entries.Count)
                return;

            var entry = skin.Entries[index];
            if (entry != null)
            {
                Net.Zombie.mController.SetImageOverride(Animations.GARGANTUAR_POLE_OBJECT.Slot, entry.CodeName);
            }
            break;
        }
    }

    public sealed override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Sync pole type used
            var poleSkinIndex = GetPoleSkinIndex();
            packetWriter.WritePackedInt(poleSkinIndex);
        }

        base.Serialize(packetWriter, init);
    }

    public sealed override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Sync pole type used
            var poleSkinIndex = packetReader.ReadPackedInt();
            SetPoleSkinByIndex(poleSkinIndex);
        }

        base.Deserialize(packetReader, init);
    }
}
