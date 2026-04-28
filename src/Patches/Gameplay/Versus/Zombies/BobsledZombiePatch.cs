using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Utilities;
using System.Collections;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class BobsledZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.Update))]
    [HarmonyPrefix]
    private static bool Zombie_Update_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.Bobsled) return true;

        if (ReplantedLobby.AmInLobby())
        {
            // Do not update and hide bobsled team until everything is spawned and ready
            if (__instance.mZombiePhase is ZombiePhase.BobsledBoarding)
            {
                if (__instance.mRelatedZombieID == ZombieID.Null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        ZombieID passenger = __instance.mFollowerZombieID[i];

                        // Passengers are not ready
                        if (passenger == ZombieID.Null)
                        {
                            __instance.mController.gameObject.SetActive(false);
                            return false;
                        }
                    }
                }
                else
                {
                    var leader = __instance.mBoard.ZombieGet(__instance.mRelatedZombieID);

                    // Leader is not ready
                    if (leader == null) return false;
                    for (int i = 0; i < 3; i++)
                    {
                        ZombieID passenger = leader.mFollowerZombieID[i];

                        if (passenger == ZombieID.Null)
                        {
                            __instance.mController.gameObject.SetActive(false);
                            return false;
                        }
                    }
                }
            }

            if (__instance.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                if (__instance.mHelmType == HelmType.Bobsled)
                {
                    // For some reason passengers helmet type doesn't get set after crash
                    // and BobsledCrash needs to be called to fix React size even though it throws a Exception, I have no idea...
                    try
                    {
                        __instance.BobsledCrash();
                    }
                    catch { }
                    __instance.mHelmType = HelmType.None;
                }
            }

            __instance.mController?.gameObject.SetActive(true);
        }

        return true;
    }

    private static void SetupPassenger(Zombie passenger, Zombie leader)
    {
        // Setup relations 
        passenger.mRelatedZombieID = leader.DataID;
        ZombieID[] followerZombieID = [.. leader.mFollowerZombieID];
        for (int i = 0; i < followerZombieID.Length; i++)
        {
            ZombieID follower = followerZombieID[i];
            if (follower == ZombieID.Null)
            {
                followerZombieID[i] = passenger.DataID;
                break;
            }
        }
        leader.mFollowerZombieID = followerZombieID;

        // Offset passenger position 
        passenger.mPosX += 50 * passenger.GetBobsledPosition();
    }

    internal static void BobsledSerialize(Zombie bobsled, PacketWriter packetWriter)
    {
        var leader = bobsled.mBoard.ZombieGet(bobsled.mRelatedZombieID);
        if (leader == null)
        {
            // Setup Bobsled leader
            leader = bobsled;
            Instances.GameplayActivity.StartCoroutine(CoSpawnPassengers(leader));

            // The leader does not have related zombie
            packetWriter.WriteNetworkObject(null);
        }
        else
        {
            // Setup Bobsled passenger
            var netLeader = leader.GetNetworked();
            packetWriter.WriteNetworkObject(netLeader);

            SetupPassenger(bobsled, leader);
        }
    }

    private static IEnumerator CoSpawnPassengers(Zombie leader)
    {
        while (!leader.HasNetworked())
        {
            yield return null;
        }

        yield return null;

        Zombie[] passengers = new Zombie[3];
        for (int i = 0; i < passengers.Length; i++)
        {
            var passenger = passengers[i] = SeedPacketDefinitions.SpawnZombie(ZombieType.Bobsled, 9, leader.mRow, false).Zombie;
            passenger.mRelatedZombieID = leader.DataID;
            SeedPacketDefinitions.SpawnZombieOnNetwork(passenger, 9, leader.mRow);
        }
    }

    internal static void BobsledDeserialize(Zombie bobsled, PacketReader packetReader)
    {
        // Setup Bobsled passenger
        var netLeader = packetReader.ReadNetworkObject<ZombieNetworked>();
        if (netLeader != null)
        {
            SetupPassenger(bobsled, netLeader._Zombie);
        }
    }
}