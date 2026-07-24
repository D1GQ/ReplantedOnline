using Il2CppReloaded.Input;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using UnityEngine.InputSystem;

namespace ReplantedOnline.Managers.Reloaded;

/// <summary>
/// Manages input-related operations.
/// </summary>
internal static class InputManager
{
    /// <summary>
    /// Set player input mappings based on their assigned side.
    /// </summary>
    internal static void SetPlayerInput(PlayerTeam team)
    {
        ResetPlayerInput();

        var versusData = Instances.VersusDataModel;
        var gameplayActivity = Instances.GameplayActivity;
        if (versusData != null && gameplayActivity != null)
        {
            if (team == PlayerTeam.Plants)
            {
                Instances.VersusDataModel.m_player1Model.m_isPlantsModel.Value = true;
                versusData.UpdatePlantsPlayer("input1", "input1", ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                gameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX;
                gameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX;
            }
            else if (team == PlayerTeam.Zombies)
            {
                Instances.VersusDataModel.m_player1Model.m_isZombiesModel.Value = true;
                versusData.UpdateZombiesPlayer("input1", "input1", ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                gameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX;
                gameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX;
            }
        }
    }

    /// <summary>
    /// reset player input mappings to default values.
    /// </summary>
    internal static void ResetPlayerInput()
    {
        Instances.VersusDataModel?.m_player1Model?.m_isZombiesModel?.Value = false;
        Instances.VersusDataModel?.m_player1Model?.m_isPlantsModel?.Value = false;
        Instances.GameplayActivity?.VersusMode?.ZombiePlayerIndex = ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX;
        Instances.GameplayActivity?.VersusMode?.PlantPlayerIndex = ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX;
        Instances.VersusDataModel?.UpdateZombiesPlayer("default", "input1", ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX);
        Instances.VersusDataModel?.UpdatePlantsPlayer("default", "input1", ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX);
    }

    /// <summary>
    /// Sets whether the input system should listen for new unpaired devices.
    /// </summary>
    /// <param name="listening">True to enable listening for new devices; false to disable.</param>
    internal static void SetListeningForNewDevice(bool listening)
    {
        var playerInput = Instances.GameplayActivity.InputService.GetPlayer(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
        playerInput.SetListeningForUnpairedDevices(listening);
    }

    /// <summary>
    /// Activates or deactivates input for the local player.
    /// </summary>
    /// <param name="active">True to activate input; false to deactivate input.</param>
    internal static void SetDeviceActive(bool active)
    {
        var playerInput = Instances.GameplayActivity.InputService.GetPlayer(ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
        if (active)
        {
            playerInput.Player.ActivateInput();
        }
        else
        {
            playerInput.Player.DeactivateInput();
        }
    }

    /// <summary>
    /// Update input listening.
    /// </summary>
    internal static void Update()
    {
        if (VersusState.AmPlantSide)
        {
            if (VersusState.IsInGameplay)
            {
                // Add shovel keybinds for keyboard and mouse
                if (Instances.GameplayActivity.InputService.CurrentControlType == ControlType.MKB)
                {
                    if (Keyboard.current.backquoteKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    {
                        if (!Instances.GameplayDataProvider.m_boardToolsModel.m_shovelModel.IsSelected)
                        {
                            Instances.GameplayDataProvider.m_boardToolsModel.SetSelected(
                                Instances.GameplayDataProvider.m_boardToolsModel.m_shovelModel,
                                ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);

                            // Add double audio like base game
                            Instances.GameplayActivity.m_audioService.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_SHOVEL);
                        }
                        else
                        {
                            Instances.GameplayDataProvider.m_boardToolsModel.ClearSelected(
                                ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX);
                        }
                    }
                }
            }
        }
    }
}