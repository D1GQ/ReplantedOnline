using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class ZombieSyncPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PlayDeathAnim))]
    [HarmonyPrefix]
    private static bool Zombie_PlayDeathAnim_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var netZombie = __instance.GetZombieNetworked();
            netZombie.SendDeathRpc(theDamageFlags);

            netZombie.CheckDeath(() =>
            {
                __instance.PlayDeathAnimOriginal(theDamageFlags);
            });

            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PlayDeathAnim))]
    internal static void PlayDeathAnimOriginal(this Zombie __instance, DamageFlags theDamageFlags)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DieWithLoot))]
    [HarmonyPrefix]
    private static bool Zombie_DieWithLoot_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            __instance.GetZombieNetworked().SendDieWithLootRpc();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DieWithLoot))]
    internal static void DieWithLootOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.MowDown))]
    [HarmonyPrefix]
    private static bool Zombie_MowDown_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            __instance.GetZombieNetworked().SendMowDownRpc();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.MowDown))]
    internal static void MowDownOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
    [HarmonyPrefix]
    private static bool Zombie_TakeDamage_Prefix(Zombie __instance, int theDamage, DamageFlags theDamageFlags)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            __instance.GetZombieNetworked().SendTakeDamageRpc(theDamage, theDamageFlags);

            return true;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
    internal static void TakeDamageOriginal(this Zombie __instance, int theDamage, DamageFlags theDamageFlags)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.HitIceTrap))]
    [HarmonyPrefix]
    private static bool Zombie_HitIceTrap_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Execute the original HitIceTrap logic locally
            __instance.HitIceTrapOriginal();

            __instance.GetZombieNetworked().SendSetFrozenRpc(true);

            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.HitIceTrap))]
    internal static void HitIceTrapOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.RemoveIceTrap))]
    [HarmonyPrefix]
    private static bool Zombie_RemoveIceTrap_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Execute the original RemoveIceTrap logic locally
            __instance.RemoveIceTrapOriginal();

            __instance.GetZombieNetworked().SendSetFrozenRpc(false);

            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.RemoveIceTrap))]
    internal static void RemoveIceTrapOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ApplyBurn))]
    [HarmonyPrefix]
    private static bool Zombie_ApplyBurn_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            __instance.GetZombieNetworked().SendApplyBurnRpc();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ApplyBurn))]
    internal static void ApplyBurnOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.StartMindControlled))]
    [HarmonyPrefix]
    private static bool Zombie_StartMindControlled_Prefix(Zombie __instance)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetZombieNetworked();
                netZombie.SendMindControlledRpc();

                return true;
            }

            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.StartMindControlled))]
    internal static void StartMindControlledOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}