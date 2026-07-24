using Il2CppReloaded.Input;
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