using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Patches.Gameplay.UI;
using ReplantedOnline.Patches.Gameplay.Versus;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode in lonny
/// </summary>
internal class VersusGameplayManager
{
    internal static void OnStart()
    {
        VersusHudPatch.SetHuds();

        List<SeedPacket> allSeedPackets =
        [
            .. Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets,
            .. Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets,
        ];

        IArena.GetCurrentArena()?.InitializeSeedPacketCooldowns([.. allSeedPackets]);

        // Disable inputs for starting countdown 
        Instances.GameplayActivity.InputService
            .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
            .Player.DeactivateInput();

        Instances.GameplayActivity.StartCoroutine(CoroutineUtils.WaitForCondition(() => VersusState.IsInGameplay, () =>
        {
            Instances.GameplayActivity.InputService
                .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
                .Player.ActivateInput();
        }));

        ReplantedLobby.LobbyData.ReadyForNetworkObjects = true;
    }

    internal static void EndGame(Vector3 focusPos, PlayerTeam winningTeam)
    {
        IVersusGamemode.GetCurrentGamemode()?.OnGameplayEnd(Instances.GameplayActivity.VersusMode, winningTeam);

        if (winningTeam is PlayerTeam.Plants)
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.PlantsWin;
        }
        else
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ZombiesWin;
        }

        GameObject trueFocus = new("FocusObject");
        trueFocus.transform.position = focusPos;

        Instances.GameplayActivity.VersusMode.SetFocusOriginal(trueFocus, Vector3.zero);
        Instances.GameplayActivity.m_audioService.StopAllMusic();
        Instances.GameplayActivity.Board.Pause(true);
        VersusEndGameManager.EndGame(winningTeam);
    }

    internal static int GetSkyRate()
    {
        return ReplantedOnlineMod.Constants.Production.SKY_RATE;
    }

    internal static int GetInitSkyRate()
    {
        return ReplantedOnlineMod.Constants.Production.INITIAL_SKY_RATE;
    }

    internal static int GetInitPlantOrGraveRate()
    {
        return Common.RandRangeInt(ReplantedOnlineMod.Constants.Production.INITIAL_PLANT_OR_GRAVE_RATE_RANGE.MIN, ReplantedOnlineMod.Constants.Production.INITIAL_PLANT_OR_GRAVE_RATE_RANGE.MAX);
    }

    internal static int GetPlantRate()
    {
        return ReplantedOnlineMod.Constants.Production.PLANT_RATE;
    }

    internal static int GetGraveRate()
    {
        return ReplantedOnlineMod.Constants.Production.GRAVE_RATE;
    }

    /// <summary>
    /// Gets the list of special zombie spawn rules used during flag zombies.
    /// </summary>
    /// <returns>
    /// A list of <see cref="FlagZombieSpecialSpawn"/> defining possible special zombie spawns.
    /// </returns>
    internal static List<FlagZombieSpecialSpawn> GetFlagZombieSpawns()
    {
        List<FlagZombieSpecialSpawn> zombies = [];

        zombies.Add(new(ZombieType.Pail, 25, 9));
        zombies.Add(new(ZombieType.TrafficCone, 75, 20));

        return zombies;
    }

    /// <summary>
    /// Calculates the amount of zombies flag zombie spawns.
    /// </summary>
    internal static int FlagSpawnAmount()
    {
        return 10;
    }
}