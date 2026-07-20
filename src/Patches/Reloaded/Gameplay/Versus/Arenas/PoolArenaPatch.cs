using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using ReplantedOnline.Modules.Reloaded.Versus.Arenas;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class PoolArenaPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CheckForPool))]
    [HarmonyPrefix]
    private static bool Zombie_CheckForPool_Prefix(Zombie __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (__instance.mZombieType is ZombieType.DolphinRider or ZombieType.Snorkel) return true;

            // All logic is remade in ZombieInPoolNetworkComponent.cs
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.LeftFogColumn))]
    [HarmonyPostfix]
    private static void Board_LeftFogColumn_Postfix(ref int __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            __result = PoolNightArena.NextFogPos + 1;
        }
    }


    private static readonly bool[] TargetZombieRows = new bool[6];
    [HarmonyPatch(typeof(Board), nameof(Board.UpdateFog))]
    [HarmonyPostfix]
    private static void Board_UpdateFog_Postfix(Board __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Clear fog around plants
            foreach (var plant in __instance.GetPlants())
            {
                switch (plant.mSeedType)
                {
                    case SeedType.Plantern:
                        FogUtils.ClearFogAroundPlant(__instance, plant, 3);
                        break;
                    case SeedType.Torchwood:
                        FogUtils.ClearFogAroundPlant(__instance, plant, 2);
                        break;
                    case SeedType.Lilypad:
                        break;
                    default:
                        FogUtils.ClearFogAroundPlant(__instance, plant, 1);
                        break;
                }
            }

            // Clear fog on lanes that have no target zombie
            for (int i = 0; i < TargetZombieRows.Length; i++)
            {
                TargetZombieRows[i] = false;
            }

            foreach (var zombie in __instance.GetZombies())
            {
                if (zombie.mZombieType == ZombieType.Target)
                    TargetZombieRows[zombie.mRow] = true;
            }

            for (int i = 0; i < TargetZombieRows.Length; i++)
            {
                var targetZombieRow = TargetZombieRows[i];
                if (!targetZombieRow)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        FogUtils.SetFogAt(__instance, column, i + 1, 0);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.ClearFogAroundPlant))]
    [HarmonyPrefix]
    private static bool Board_ClearFogAroundPlant_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CanTargetPlant))]
    [HarmonyPostfix]
    private static void Zombie_CanTargetPlant_Postfix(Zombie __instance, Plant thePlant, ZombieAttackType theAttackType, ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Fix zombies in pool not finding target
            if (__instance.mBoard.mPlantRow[__instance.mRow] == PlantRowType.Pool)
            {
                var plantOnLawn = __instance.mBoard.GetPlantsOnLawn(PvZRUtils.ReloadedObjectXToGridX(thePlant.X), PvZRUtils.ReloadedObjectYToGridY(thePlant.Y));

                if (plantOnLawn.PumpkinPlant != null)
                {
                    if (plantOnLawn.PumpkinPlant == thePlant)
                    {
                        __result = true;
                    }

                    return;
                }

                if (plantOnLawn.NormalPlant != null)
                {
                    if (plantOnLawn.NormalPlant == thePlant)
                    {
                        __result = true;
                    }

                    return;
                }

                if (plantOnLawn.UnderPlant == thePlant)
                {
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.AddTodParticle), [typeof(float), typeof(float), typeof(int), typeof(ParticleEffect)])]
    [HarmonyPrefix]
    private static void GameplayActivity_AddTodParticle_Prefix(GameplayActivity __instance, float theY, ref ParticleEffect theEffect, ref TodParticleSystem __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!__instance.Board.StageHasPool()) return;

            if (PvZRUtils.ReloadedObjectYToGridY(theY) is 2 or 3)
            {
                theEffect = theEffect switch
                {
                    ParticleEffect.ZombieHead => ParticleEffect.ZombieHeadPool,
                    ParticleEffect.MoweredZombieArm => ParticleEffect.NumParticles,
                    ParticleEffect.ZombieArm => ParticleEffect.NumParticles,
                    ParticleEffect.ZombieArmRetro => ParticleEffect.NumParticles,
                    ParticleEffect.DustFoot => ParticleEffect.NumParticles,
                    _ => theEffect
                };
            }
        }
    }
}
