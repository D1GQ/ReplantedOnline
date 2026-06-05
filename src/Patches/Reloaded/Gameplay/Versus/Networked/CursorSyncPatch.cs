using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Input;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Rpc;
using ReplantedOnline.Utilities.Modded;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class CursorSyncPatch
{
    // This also works with gamepad
    private static readonly ExecuteInterval downInterval = new();
    [HarmonyPatch(typeof(Board), nameof(Board.MouseDownWithTool))]
    [HarmonyPrefix]
    private static bool Board_MouseDownWithTool_Prefix(Board __instance, int x, int y, CursorType theCursorType, int playerIndex)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!downInterval.Execute()) return false;

            if (playerIndex != ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX) return false;
            if (!VersusState.AmPlantSide) return false;

            if (theCursorType == CursorType.Shovel)
            {
                Plant plant;
                if (Instances.GameplayActivity.InputService.CurrentControlType == ControlType.MKB)
                {
                    var pos = Instances.GameplayActivity.GetMousePosition();
                    plant = __instance.ToolHitTest((int)pos.x, (int)pos.y, ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                }
                else
                {
                    var gridX = Instances.GameplayActivity.Board.PixelToGridXKeepOnBoard(x, y);
                    var gridY = Instances.GameplayActivity.Board.PixelToGridYKeepOnBoard(x, y);
                    plant = __instance.GetTopPlantAt(gridX, gridY, PlantPriority.Any);
                }

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
    private static void GamepadCursorController_OnCursorConfirmed_Prefix(GamepadCursorController __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);

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
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX) && seedPacket.mActive)
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                        seedPacket.mActive = false; // Fix issue with cooldown on GamePad 
                        seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedType);
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                        SeedPacketDefinitions.PlaceSeed(seedType, gridX, gridY, true);
                        Rpc<SyncSeedPacketRpc>.Instance.Send(seedType);

                        // Prevent buzzer sound!
                        throw new SilentPatchException();
                    }
                }

                __instance.Board.mApp.PlaySample(Sound.SOUND_BUZZER);
                // Prevent duplicate buzzer sound!
                throw new SilentPatchException();
            }
        }
    }

    // Rework planting seeds to support RPCs
    // This actually took hours to find out what's doing what :(
    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.OnMouseDownBG))]
    [HarmonyPrefix]
    private static bool GameplayActivity_OnMouseDownBG_Prefix(GameplayActivity __instance, int playerIndex)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (playerIndex != ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX) return false;

            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);

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
                    if (seedPacket == null) return false;

                    // Get the cost of the seed and check if player has enough sun
                    var cost = seedPacket.GetCost();
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX))
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                        seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedType);
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                        __instance.Board.ClearCursor();
                        SeedPacketDefinitions.PlaceSeed(seedType, gridX, gridY, true);
                        Rpc<SyncSeedPacketRpc>.Instance.Send(seedType);

                        return false;
                    }
                }

                __instance.Board.mApp.PlaySample(Sound.SOUND_BUZZER);
                // Prevent duplicate buzzer sound!
                throw new SilentPatchException();
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.MouseDownWithPlant))]
    [HarmonyPrefix]
    private static bool Board_MouseDownWithPlant_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }
}