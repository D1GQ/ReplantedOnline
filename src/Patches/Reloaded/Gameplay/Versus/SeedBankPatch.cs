using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class SeedBankPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.GetNumSeedsInBank))]
    [HarmonyPostfix]
    private static void Board_GetNumSeedsInBank_Postfix(ref int __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Set seed back size
            __result = IArenaSetupSeedbank.GetSeedPacketCount();
        }
    }

    [HarmonyPatch(typeof(SeedBank), nameof(SeedBank.GetPacketCount))]
    [HarmonyPrefix]
    private static bool SeedBank_GetPacketCount_Prefix(SeedBank __instance, ref int __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Work around hard coded required seeds in seedbank to start game
            int inBank = 0;
            foreach (var seedPacket in __instance.SeedPackets)
            {
                if (seedPacket.mPacketType != SeedType.None)
                {
                    inBank++;
                }
            }

            __result = inBank - (IArenaSetupSeedbank.GetSeedPacketCount() - VersusMode.k_numPackets);
        }

        return false;
    }
}
