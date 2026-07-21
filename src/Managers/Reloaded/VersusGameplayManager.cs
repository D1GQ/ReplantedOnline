using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Input;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Patches.Reloaded.Gameplay.UI;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReplantedOnline.Managers.Reloaded;

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
        InputManager.SetDeviceActive(false);

        Instances.GameplayActivity.StartCoroutine(CoroutineUtils.WaitForCondition(() => VersusState.IsInGameplay, () =>
        {
            InputManager.SetDeviceActive(true);
        }));

        ReloadedLobby.LobbyData?.ReadyForNetworkObjects = true;
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
        Instances.GameplayActivity.m_messageWidgetController.Hide();
        Instances.GameplayActivity.Board.FreezeEffectsForCutscene(true);
        Instances.GameplayActivity.m_audioService.StopAllMusic();
        VersusEndGameManager.EndGame(winningTeam);
    }

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

    internal static int GetSkyRate()
    {
        return ReplantedOnlineMod.Constants.Reloaded.Production.SKY_RATE;
    }

    internal static int GetInitSkyRate()
    {
        return ReplantedOnlineMod.Constants.Reloaded.Production.INITIAL_SKY_RATE;
    }

    internal static int GetInitPlantOrGraveRate()
    {
        return Common.RandRangeInt(ReplantedOnlineMod.Constants.Reloaded.Production.INITIAL_PLANT_OR_GRAVE_RATE_RANGE.MIN, ReplantedOnlineMod.Constants.Reloaded.Production.INITIAL_PLANT_OR_GRAVE_RATE_RANGE.MAX);
    }

    internal static int GetPlantRate()
    {
        return ReplantedOnlineMod.Constants.Reloaded.Production.PLANT_RATE;
    }

    internal static int GetGraveRate()
    {
        return ReplantedOnlineMod.Constants.Reloaded.Production.GRAVE_RATE;
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
        var definition = Instances.IDataService.GetPlantDefinition(seedType);
        if (definition != null)
        {
            int refreshTime;
            if (VersusState.VersusPhase != VersusPhase.SuddenDeath)
            {
                refreshTime = definition.m_versusBaseRefreshTime;
            }
            else
            {
                refreshTime = definition.m_versusSuddenDeathRefreshTime;
            }

            CloudyDayArenaPatch.ApplyRefreshTimeReduction(ref refreshTime);

            if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
            {
                return refreshTime;
            }

            float normalized = Mathf.Clamp01(VersusState.VersusTime / ReplantedOnlineMod.Constants.Reloaded.X2_SEEDPACKET_COOLDOWN_TIME_END);
            float time = Mathf.Lerp(refreshTime * 2, refreshTime, normalized);
            return Mathf.FloorToInt(time);
        }

        return 0;
    }
}