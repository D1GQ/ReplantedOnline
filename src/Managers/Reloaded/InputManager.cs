using ReplantedOnline.Modules.Modded.Instance;

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
}