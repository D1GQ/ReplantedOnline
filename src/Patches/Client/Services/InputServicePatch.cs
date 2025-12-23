using HarmonyLib;
using Il2CppReloaded.Input;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Client;

[HarmonyPatch]
internal class InputServicePatch
{
    [HarmonyPatch(typeof(InputService), nameof(InputService.BeginListeningForGuestInputDevice))]
    [HarmonyPrefix]
    private static bool InputService_BeginListeningForGuestInputDevice_Prefix()
    {
        if (NetLobby.AmInLobby())
        {
            // Prevent second local player from being detected
            return false;
        }

        return true;
    }
}
