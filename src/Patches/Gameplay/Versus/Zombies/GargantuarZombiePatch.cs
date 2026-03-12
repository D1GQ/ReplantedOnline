using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities;
using System.Collections;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class GargantuarZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPostfix]
    private static void Zombie_ZombieInitialize_Postfix(Zombie __instance, ZombieType theType)
    {
        if (theType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar)) return;

        if (NetLobby.AmInLobby())
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

        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetNetworked();
                if (netZombie != null)
                {
                    if (__instance.mZombiePhase != ZombiePhase.GargantuarSmashing)
                    {
                        if (netZombie.State is NetStates.GARGANTUAR_SMASH_STATE)
                        {
                            // If the gargantuar is in synced smashing state, move it forward to find a target
                            if (__result == null)
                            {
                                __instance.mPosX--;
                            }
                            else
                            {
                                netZombie.State = NetStates.GARGANTUAR_TARGET_STATE;
                            }
                        }
                        else if (netZombie.State is not NetStates.GARGANTUAR_TARGET_STATE)
                        {
                            // If the gargantuar is not in synced smashing state, move it backward if target is found
                            if (__result != null)
                            {
                                __result = null;
                                __instance.mPosX++;
                            }
                        }
                    }
                    else
                    {
                        // If the gargantuar is in smashing phase, clear target state
                        if (netZombie.State is NetStates.GARGANTUAR_TARGET_STATE)
                        {
                            netZombie.State = null;
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
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (__instance.mHasObject && __instance.mZombiePhase == ZombiePhase.GargantuarThrowing)
                {
                    HandleGargantuarThrow(__instance);
                    HandleImpThrown(__instance);

                    return false;
                }

                var netZombie = __instance.GetNetworked();
                if (netZombie != null)
                {
                    if (__instance.mZombiePhase == ZombiePhase.GargantuarSmashing)
                    {
                        if (netZombie.State is not NetStates.GARGANTUAR_SMASH_STATE)
                        {
                            netZombie.State = NetStates.GARGANTUAR_SMASH_STATE;
                            netZombie.SendSetStateRpc(NetStates.GARGANTUAR_SMASH_STATE);
                        }
                    }
                    else
                    {
                        if (netZombie.State != null)
                        {
                            netZombie.State = null;
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
        Zombie imp = gargantuar.mBoard.AddZombieInRowOriginal(ZombieType.Imp, gargantuar.mRow, gargantuar.mFromWave, false);

        // Link the imp to the Gargantuar for synchronization
        imp.mRelatedZombieID = gargantuar.DataID;

        SetupImp(gargantuar, imp);
        SeedPacketSyncPatch.SpawnZombieOnNetwork(imp, 20, 0, false); // spawn imp on network off screen for plant side to sync the throw
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
            imp.PlayZombieReanim(Animations.IMP_FLYING, ReanimLoopType.Loop, 3, 18f);

            // Force immediate animation update
            imp.UpdateReanim();

            // Play imp sound
            Instances.GameplayActivity?.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_IMP);
        }).WrapToIl2cpp());
    }

    private static void ConfigureImpArc(Zombie gargantuar, Zombie imp)
    {
        float baseArc = gargantuar.mPosX - 360f;

        // Add random variation (0-100)
        float randomArc = UnityEngine.Random.Range(0f, 100f);
        float finalArc = baseArc - randomArc;

        // Convert to vertical velocity (formula: (finalArc / 3.0) * 0.5 * 0.05)
        imp.mVelZ = (finalArc / 3f) * 0.5f * 0.05f;
    }

    // Serialization and deserialization for the imp to sync the throw across the network
    internal static void ImpSerialize(Zombie imp, PacketWriter packetWriter)
    {
        Zombie gargantuar = imp.mBoard.ZombieGet(imp.mRelatedZombieID);

        if (gargantuar != null)
        {
            packetWriter.WriteNetworkObject(gargantuar.GetNetworked());
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    internal static void ImpDeserialize(Zombie imp, PacketReader packetReader)
    {
        Zombie gargantuar = packetReader.ReadNetworkObject<ZombieNetworked>()._Zombie;

        // Link the imp to the Gargantuar for synchronization, UpdateZombieGargantuar will handle the rest of the throw logic
        gargantuar?.mRelatedZombieID = imp.DataID;
    }

    // Waits for the Gargantuar's throw animation to reach the point where the imp is thrown before executing the callback
    private static IEnumerator CoWaitForGargantuarToFinish(Zombie gargantuar, Action callback)
    {
        while (true)
        {
            if (!NetLobby.AmInLobby())
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
