using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Rpc;
using ReplantedOnline.Utilities;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class CursorSyncPatch
{
    // This also works with gamepad
    [HarmonyPatch(typeof(Board), nameof(Board.MouseDownWithTool))]
    [HarmonyPrefix]
    private static bool Board_MouseDownWithTool_Prefix(Board __instance, int x, int y, CursorType theCursorType, int playerIndex)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (playerIndex != ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX) return false;
            if (!VersusState.AmPlantSide) return false;

            if (theCursorType == CursorType.Shovel)
            {
                var gridX = Instances.GameplayActivity.Board.PixelToGridXKeepOnBoard(x, y);
                var gridY = Instances.GameplayActivity.Board.PixelToGridYKeepOnBoard(x, y);

                var plant = __instance.GetTopPlantAt(gridX, gridY, PlantPriority.Any);
                if (plant != null)
                {
                    var plantNetworked = plant.GetNetworked();
                    if (plantNetworked != null)
                    {
                        __instance.mPlantsShoveled++;
                        plantNetworked.SendShoveledRpc();
                        Instances.GameplayActivity.PlaySample(Sound.SOUND_PLANT2);
                    }
                }

                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController._onCursorConfirmed))]
    [HarmonyPrefix]
    private static bool GamepadCursorController_OnCursorConfirmed_Prefix(GamepadCursorController __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);

            // Check if the player is currently holding a plant in their cursor
            if (seedType != SeedType.None)
            {
                // Get the cursor position and convert it to grid coordinates
                var gridX = __instance.m_cursor.m_gridX;
                var gridY = __instance.m_cursor.m_gridY;

                // Check if planting at this position is valid
                if (__instance.Board.CanPlantAt(gridX, gridY, seedType) == PlantingReason.Ok)
                {
                    // Find the seed packet from the seed bank that matches the seed type
                    var seedPacket = __instance.GetFirstSelectedSeedPack();

                    // Get the cost of the seed and check if player has enough sun
                    var cost = seedPacket.GetCost();
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX) && seedPacket.mActive)
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        seedPacket.mActive = false; // Fix issue with cooldown on GamePad 
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        SeedPacketDefinitions.PlaceSeed(seedType, seedPacket.mImitaterType, gridX, gridY, true);
                        Rpc<SyncSeedPacketRpc>.Instance.Send(seedType);
                    }
                    else
                    {
                        Instances.GameplayActivity.PlaySample(Sound.SOUND_BUZZER);
                    }

                    // This has to be done to prevent Buzzer sound always playing after planting with GamePad
                    // not sure why the original method doesn't work properly but this is the only way I found to fix it
                    // Most likely the method calling _onCursorConfirmed
                    throw new SilentPatchException();
                }

                Instances.GameplayActivity.PlaySample(Sound.SOUND_BUZZER);

                // This has to be done to prevent Buzzer sound always playing after planting with GamePad
                // not sure why the original method doesn't work properly but this is the only way I found to fix it
                // Most likely the method calling _onCursorConfirmed
                throw new SilentPatchException();
            }
        }

        // Return true to execute original method (no plant in cursor, normal behavior)
        return true;
    }

    // Rework planting seeds to support RPCs
    // This actually took hours to find out what's doing what :(
    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.OnMouseDownBG))]
    [HarmonyPrefix]
    private static bool GameplayActivity_OnMouseDownBG_Prefix(GameplayActivity __instance, int mouseButton, int playerIndex)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);

            // Check if the player is currently holding a plant in their cursor
            if (seedType != SeedType.None)
            {
                // Get the mouse position and convert it to grid coordinates
                var pos = Instances.GameplayActivity.GetMousePosition();
                var gridX = Instances.GameplayActivity.Board.PixelToGridXKeepOnBoard(pos.x, pos.y);
                var gridY = Instances.GameplayActivity.Board.PixelToGridYKeepOnBoard(pos.x, pos.y);

                // Check if planting at this position is valid
                if (__instance.Board.CanPlantAt(gridX, gridY, seedType) == PlantingReason.Ok)
                {
                    // Find the seed packet from the seed bank that matches the seed type
                    var seedPacket = __instance.Board.SeedBanks.LocalItem().SeedPackets.FirstOrDefault(packet => packet.mPacketType == seedType);

                    // Get the cost of the seed and check if player has enough sun
                    var cost = seedPacket.GetCost();
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX))
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        __instance.Board.ClearCursor();
                        SeedPacketDefinitions.PlaceSeed(seedType, seedPacket.mImitaterType, gridX, gridY, true);
                        Rpc<SyncSeedPacketRpc>.Instance.Send(seedType);
                    }
                    else
                    {
                        Instances.GameplayActivity.PlaySample(Sound.SOUND_BUZZER);
                    }

                    return false;
                }

                // If planting is not valid, play buzzer sound
                Instances.GameplayActivity.PlaySample(Sound.SOUND_BUZZER);

                return false;
            }
        }

        // Return true to execute original method (no plant in cursor, normal behavior)
        return true;
    }
}