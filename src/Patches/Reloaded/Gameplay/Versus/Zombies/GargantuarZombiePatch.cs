using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using System.Collections;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class GargantuarZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPostfix]
    private static void Zombie_ZombieInitialize_Postfix(Zombie __instance, ZombieType theType)
    {
        if (theType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar)) return;

        if (ReloadedLobby.AmInLobby())
        {
            // Stop gargantuar from going into throwing phase
            if (!VersusState.AmPlantSide)
            {
                __instance.mHasObject = false;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ref Plant __result)
    {
        if (__instance.mZombieType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar)) return;

        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                var zombieNetworked = __instance.GetNetworked();
                if (zombieNetworked != null)
                {
                    if (__instance.mZombiePhase != ZombiePhase.GargantuarSmashing)
                    {
                        if (zombieNetworked.State is ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_SMASH_STATE)
                        {
                            // If the gargantuar is in synced smashing state, move it forward to find a target
                            zombieNetworked.LogicComponent.PosSyncingPaused = true;
                            zombieNetworked.LogicComponent.StopLarpPos();
                            if (__result == null)
                            {
                                __instance.mPosX += __instance.GetZombieMoveDirection();
                            }
                            else
                            {
                                zombieNetworked.State = ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_TARGET_STATE;
                            }
                        }
                        else if (zombieNetworked.State is not ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_TARGET_STATE)
                        {
                            // If the gargantuar is not in synced smashing state, move it backward if target is found
                            if (__result != null)
                            {
                                __result = null;
                                __instance.mPosX -= __instance.GetZombieMoveDirection();
                            }
                        }
                    }
                    else
                    {
                        // If the gargantuar is in smashing phase, clear target state
                        if (zombieNetworked.State is ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_TARGET_STATE)
                        {
                            zombieNetworked.LogicComponent.PosSyncingPaused = false;
                            zombieNetworked.State = null;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombieGargantuar))]
    [HarmonyPrefix]
    private static bool Zombie_UpdateZombieGargantuar_Prefix(Zombie __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (__instance.mHasObject && __instance.mZombiePhase == ZombiePhase.GargantuarThrowing)
                {
                    HandleGargantuarThrow(__instance);
                    HandleImpThrown(__instance);

                    return false;
                }

                var zombieNetworked = __instance.GetNetworked();
                if (zombieNetworked != null)
                {
                    if (__instance.mZombiePhase == ZombiePhase.GargantuarSmashing)
                    {
                        if (zombieNetworked.State is not ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_SMASH_STATE)
                        {
                            zombieNetworked.State = ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_SMASH_STATE;
                            zombieNetworked.SendSetStateRpc(ReplantedOnlineMod.Constants.Network.ObjectStates.GARGANTUAR_SMASH_STATE);
                        }
                    }
                    else
                    {
                        if (zombieNetworked.State != null)
                        {
                            zombieNetworked.State = null;
                        }
                    }
                }
            }
            else
            {
                if (__instance.mRelatedZombieID != ZombieID.Null)
                {
                    if (__instance.mHasObject && __instance.mZombiePhase == ZombiePhase.GargantuarThrowing)
                    {
                        // Get imp from deserialization
                        Zombie imp = __instance.mBoard.ZombieGet(__instance.mRelatedZombieID);
                        __instance.mRelatedZombieID = ZombieID.Null; // Clear related ID after getting the imp
                        HandleGargantuarThrow(__instance);
                        SetupImp(__instance, imp);

                        return false;
                    }
                    else
                    {
                        // Allow gargantuar to go into throwing phase
                        __instance.mHasObject = true;
                    }
                }
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DragUnder))]
    [HarmonyPrefix]
    private static bool Zombie_DragUnder_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar)) return true;

        if (ReloadedLobby.AmInLobby())
        {
            // Make Tanglekelp act like every other insta on Gargantuar
            if (__instance.mBodyHealth > 1800)
            {
                __instance.TakeDamage(1800, DamageFlags.HitsShieldAndBody);
                foreach (var plant in __instance.mBoard.GetPlants())
                {
                    if (plant.mSeedType != SeedType.Tanglekelp) continue;

                    if (plant.mTargetZombieID == __instance.DataID)
                    {
                        plant.mTargetZombieID = ZombieID.Null;
                    }
                }
                return false;
            }
        }

        return true;
    }

    private static void HandleGargantuarThrow(Zombie gargantuar)
    {
        gargantuar.mZombiePhase = ZombiePhase.GargantuarThrowing;
        float animSpeed = gargantuar.mBoard.StageHasRoof() ? 24f : 18f;
        gargantuar.PlayZombieReanim(Animations.GARGANTUAR_THROW, ReanimLoopType.Loop, 20, animSpeed);

        // Set hasObject to false
        gargantuar.mHasObject = false;

        // Play throw sound
        Instances.GameplayActivity?.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_THROW);
    }

    private static void HandleImpThrown(Zombie gargantuar)
    {
        // Create the Imp zombie
        Zombie imp = SeedPacketDefinitions.SpawnZombie(ZombieType.Imp, 9, gargantuar.mRow, SpawnType.None, false).Zombie;

        // Link the imp to the Gargantuar for synchronization
        imp.mRelatedZombieID = gargantuar.DataID;

        SetupImp(gargantuar, imp);
        SeedPacketDefinitions.SpawnZombieOnNetwork(imp, 20, 0, SpawnType.None); // spawn imp on network off screen for plant side to sync the throw
    }

    private static void SetupImp(Zombie gargantuar, Zombie imp)
    {
        // Hide the real imp visually until the throw animation reaches the right point
        imp.mController.gameObject.SetActive(false);

        Instances.GameplayActivity?.StartCoroutine(CoWaitForGargantuarToFinish(gargantuar, () =>
        {
            // Hide the imp visually on gargantuar
            gargantuar.mController.AssignRenderGroupToPrefix("Zombie_imp", -1);
            gargantuar.mController.AssignRenderGroupToTrack("Zombie_imp_track", -1);

            imp.mController.gameObject.SetActive(true);

            // Position the imp relative to Gargantuar
            imp.mPosX = gargantuar.mPosX - 133f;
            imp.mPosY = gargantuar.GetPosYBasedOnRow(gargantuar.mRow);
            imp.mRow = gargantuar.mRow;

            // Set render order
            imp.mRenderOrder = Board.MakeRenderOrder(RenderLayer.Zombie, gargantuar.mRow, 4);
            imp.mRenderOrder++; // Slightly higher render order

            // Configure imp state
            imp.mVariant = false;
            imp.mZombiePhase = ZombiePhase.ImpGettingThrown;
            imp.mAltitude = 88f; // Start at height
            imp.mVelX = 3f; // Horizontal speed

            // Copy chilled state from Gargantuar
            imp.mChilledCounter = gargantuar.mChilledCounter;

            // Calculate the arc trajectory
            ConfigureImpArc(gargantuar, imp);

            // Play imp flying animation
            imp.PlayZombieReanim(Animations.IMP_FLYING.Anim, ReanimLoopType.Loop, 3, Animations.IMP_FLYING.AnimRate);

            // Force immediate animation update
            imp.UpdateReanim();

            // Play imp sound
            Instances.GameplayActivity?.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_IMP);
        }));
    }

    private static void ConfigureImpArc(Zombie gargantuar, Zombie imp)
    {
        float randomArc = 0f;
        var impNetworked = imp.GetNetworked();
        if (impNetworked != null)
        {
            randomArc = impNetworked.GetNetworkComponent<ImpNetworkComponent>().ImpRandomArc;
        }

        float baseArc = gargantuar.mPosX - 360f;

        // Add random variation (0-100)
        float finalArc = baseArc - randomArc;

        // Convert to vertical velocity (formula: (finalArc / 3.0) * 0.5 * 0.05)
        imp.mVelZ = finalArc / 3f * 0.5f * 0.05f;
    }

    // Serialization and deserialization for the imp to sync the throw across the network
    internal static void ImpSerialize(ZombieNetworked impNetworked, PacketWriter packetWriter)
    {
        Zombie gargantuar = impNetworked._Zombie.mBoard.ZombieGet(impNetworked._Zombie.mRelatedZombieID);

        if (gargantuar != null)
        {
            packetWriter.WriteNetworkObject(gargantuar.GetNetworked());
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }

        var impComp = impNetworked.GetNetworkComponent<ImpNetworkComponent>();
        impComp.ImpRandomArc = UnityEngine.Random.Range(0f, 100f);
        packetWriter.WriteFloat(impComp.ImpRandomArc);
    }

    internal static void ImpDeserialize(ZombieNetworked impNetworked, PacketReader packetReader)
    {
        Zombie gargantuar = packetReader.ReadNetworkObject<ZombieNetworked>()?._Zombie;

        var impComp = impNetworked.GetNetworkComponent<ImpNetworkComponent>();
        impComp.ImpRandomArc = packetReader.ReadFloat();

        // Link the imp to the Gargantuar for synchronization, UpdateZombieGargantuar will handle the rest of the throw logic
        gargantuar?.mRelatedZombieID = impNetworked._Zombie.DataID;
    }

    // Waits for the Gargantuar's throw animation to reach the point where the imp is thrown before executing the callback
    private static IEnumerator CoWaitForGargantuarToFinish(Zombie gargantuar, Action callback)
    {
        while (true)
        {
            if (!ReloadedLobby.AmInLobby())
            {
                yield break;
            }

            if (gargantuar.mZombiePhase == ZombiePhase.GargantuarThrowing)
            {
                if (gargantuar.mController.ShouldTriggerTimedEvent(0.72f, CharacterAnimationTrack.Body))
                {
                    break;
                }
            }

            yield return null;
        }


        callback();
        yield break;
    }
}
