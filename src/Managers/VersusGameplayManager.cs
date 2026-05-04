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

        foreach (var seedPacket in allSeedPackets)
        {
            if (SeedPacketDefinitions.IgnoreInitialCooldownSeedTypes.Contains(seedPacket.mPacketType)) continue;

            seedPacket.Deactivate();
            var time = Instances.IDataService.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            seedPacket.mRefreshTime = time;
            seedPacket.mRefreshing = true;
        }

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

        switch (VersusState.Arena)
        {
            case ArenaTypes.Night:
                zombies.Add(new(ZombieType.Pail, 15, 13)); // 15% -> 2% -> 0%
                zombies.Add(new(ZombieType.Newspaper, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
            case ArenaTypes.Roof:
            case ArenaTypes.RoofNight:
            case ArenaTypes.China:
                // Bungees ether takes a plant or drops another FlagZombieSpecialSpawn zombie type from this list
                zombies.Add(new(ZombieType.Bungee, 75, 35)); // 75% -> 40% -> 5% | 15% chance to spawn normal bungee in FlagZombiePatch.cs
                zombies.Add(new(ZombieType.Pail, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
            default:
                zombies.Add(new(ZombieType.Pail, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
        }

        return zombies;
    }

    /// <summary>
    /// Calculates the amount of zombies flag zombie spawns.
    /// </summary>
    internal static int FlagSpawnAmount()
    {
        float normalized = Mathf.Clamp01(VersusState.VersusTime / VersusMode.k_suddenDeathStartTime);
        float value = Mathf.Lerp(5f, 10f, normalized);
        return Mathf.FloorToInt(value);
    }

    /// <summary>
    /// Gets the refresh time for a seed packet in versus mode, which scales down over time to its base cooldown.
    /// </summary>
    /// <param name="seedType"></param>
    /// <returns></returns>
    internal static int GetSeedPacketRefreshTime(SeedType seedType)
    {
        if (VersusState.VersusPhase == VersusPhase.SuddenDeath)
        {
            return Instances.IDataService.GetPlantDefinition(seedType)?.m_versusSuddenDeathRefreshTime ?? 0;
        }

        if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
        {
            return Instances.IDataService.GetPlantDefinition(seedType)?.m_versusBaseRefreshTime ?? 0;
        }

        float normalized = Mathf.Clamp01(VersusState.VersusTime / ReplantedOnlineMod.Constants.X2_SEEDPACKET_COOLDOWN_TIME_END);
        int baseTime = Instances.IDataService.GetPlantDefinition(seedType)?.m_versusBaseRefreshTime ?? 0;
        float time = Mathf.Lerp(baseTime * 2, baseTime, normalized);
        return Mathf.FloorToInt(time);
    }
}