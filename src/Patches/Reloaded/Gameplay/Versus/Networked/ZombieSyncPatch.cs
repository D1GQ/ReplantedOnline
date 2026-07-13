using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class ZombieSyncPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PlayDeathAnim))]
    [HarmonyPrefix]
    private static bool Zombie_PlayDeathAnim_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                if (VersusState.AmPlantSide)
                {
                    zombieNetworked.SendDeathRpc(theDamageFlags);
                }
                else
                {
                    if (!zombieNetworked.Dying)
                    {
                        return false;
                    }
                }
            }
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
        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                if (VersusState.AmPlantSide)
                {
                    zombieNetworked.SendDieLootRpc(true);
                }
                else
                {
                    if (!zombieNetworked.Dying)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DieWithLoot))]
    internal static void DieWithLootOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DieNoLoot))]
    [HarmonyPrefix]
    private static bool Zombie_DieNoLoot_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                if (VersusState.AmPlantSide)
                {
                    zombieNetworked.SendDieLootRpc(false);
                }
                else
                {
                    if (!zombieNetworked.Dying)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DieNoLoot))]
    internal static void DieNoLootOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DragUnder))]
    [HarmonyPrefix]
    private static bool Zombie_DragUnder_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                zombieNetworked.SendDragUnderRpc();
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DragUnder))]
    internal static void DragUnderOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.MowDown))]
    [HarmonyPrefix]
    private static bool Zombie_MowDown_Prefix(Zombie __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (__instance.mZombieType.IsGravestoneOrTarget()) return false;

            if (!__instance.mDead)
            {
                var zombieNetworked = __instance.GetNetworked();
                if (zombieNetworked != null)
                {
                    if (VersusState.AmPlantSide)
                    {
                        zombieNetworked.SendMowDownRpc();
                    }
                    else
                    {
                        if (!zombieNetworked.Dying)
                        {
                            return false;
                        }
                    }
                }
            }
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
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            if (__instance.mHasHead)
            {
                var zombieNetworked = __instance.GetNetworked();
                if (zombieNetworked != null)
                {
                    zombieNetworked.SendTakeDamageRpc(theDamage, theDamageFlags);
                }
            }
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
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                // Execute the original HitIceTrap logic locally
                __instance.HitIceTrapOriginal();
                zombieNetworked.SendSetFrozenRpc(true);
            }

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
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                // Execute the original RemoveIceTrap logic locally
                __instance.RemoveIceTrapOriginal();
                zombieNetworked.SendSetFrozenRpc(false);
            }

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
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;
            if (__instance.mZombieType.IsGravestoneOrTarget()) return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                zombieNetworked.SendApplyBurnRpc();
            }
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
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                zombieNetworked.SendMindControlledRpc();
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.StartMindControlled))]
    internal static void StartMindControlledOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateYuckyFace))]
    [HarmonyPrefix]
    private static void Zombie_UpdateYuckyFace_Prefix(Zombie __instance, ref (int Row, int RenderOrder) __state)
    {
        __state = (__instance.mRow, __instance.RenderOrder);
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateYuckyFace))]
    [HarmonyPostfix]
    private static void Zombie_UpdateYuckyFace_Postfix(Zombie __instance, (int Row, int RenderOrder) __state)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (__instance.mRow != __state.Row)
                {
                    var zombieNetworked = __instance.GetNetworked();
                    if (zombieNetworked != null)
                    {
                        zombieNetworked.SendMoveToRowRpc(__instance.mRow);
                    }
                }
            }
            else
            {
                __instance.mRow = __state.Row;
                __instance.RenderOrder = __state.RenderOrder;
            }
        }
    }
}