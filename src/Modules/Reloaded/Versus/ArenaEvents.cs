using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded.Versus;

/// <summary>
/// Handles arena event functionality for Versus mode.
/// </summary>
internal static class ArenaEvents
{
    /// <summary>
    /// Represents the different types of arena events that can be triggered in Versus mode.
    /// </summary>
    internal enum EventTitle
    {
        /// <summary>
        /// Event that pushes zombies backward across the board.
        /// </summary>
        PushBack,
    }

    /// <summary>
    /// Displays an event title message on screen and plays the final wave sound effect.
    /// </summary>
    /// <param name="eventTitle">The title text to display on screen.</param>
    internal static void DisplayEventTitle(EventTitle eventTitle)
    {
        string title = eventTitle switch
        {
            EventTitle.PushBack => "Push Back!",
            _ => string.Empty
        };

        Instances.GameplayActivity.Board.DisplayAdvice(title, MessageStyle.BigMiddleFast, AdviceType.None);
        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_FINALWAVE);
    }

    /// <summary>
    /// List of zombie types that are immune to push back events.
    /// </summary>
    internal static readonly ZombieType[] PushBackEventIgnoreZombie =
    [
        ZombieType.Gravestone, ZombieType.Target, ZombieType.Bungee,
        ZombieType.Gargantuar, ZombieType.RedeyeGargantuar, ZombieType.Zamboni,
        ZombieType.Bobsled, ZombieType.Catapult, ZombieType.Digger
    ];

    /// <summary>
    /// List of zombie phases that cause immunity to push back events.
    /// </summary>
    internal static readonly ZombiePhase[] PushBackEventIgnoreZombiePhase =
    [
        ZombiePhase.RisingFromGrave, ZombiePhase.BalloonPopping, ZombiePhase.BalloonFlying,
        ZombiePhase.BalloonPopping, ZombiePhase.PolevaulterInVault, ZombiePhase.DolphinIntoPool,
        ZombiePhase.DolphinRiding, ZombiePhase.DolphinInJump, ZombiePhase.SnorkelWalkingInPool
    ];

    /// <summary>
    /// Triggers a synced push back event that pushes eligible zombies backward across the board.
    /// </summary>
    internal static void PushBackEvent()
    {
        if (!ReloadedLobby.AmLobbyHost()) return;

        DisplayEventTitle(EventTitle.PushBack);
        NetworkManager.Rpc<DisplayEventTitleRpc>.Singleton.Send(EventTitle.PushBack);

        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (PushBackEventIgnoreZombie.Contains(zombie.mZombieType)) continue;
            if (PushBackEventIgnoreZombiePhase.Contains(zombie.mZombiePhase)) continue;
            if (zombie.IsDeadOrDying()) continue;
            if (zombie.mVelX <= 0f) continue;
            if (zombie.mPosX > 490f) continue;
            if (VersusState.ArenaSynced is ArenaType.Pool or ArenaType.PoolNight && Instances.GameplayActivity.Board.mPlantRow[zombie.mRow] != PlantRowType.Pool) continue;

            PushBackZombie(zombie);
            NetworkManager.Rpc<PushBackZombieRpc>.Singleton.Send(zombie);
        }

        for (int gridY = 0; gridY < Instances.GameplayActivity.Board.GetNumRows(); gridY++)
        {
            for (int gridX = 0; gridX < 5; gridX++)
            {
                var plant = Instances.GameplayActivity.Board.GetTopPlantAt(gridX, gridY, PlantPriority.Any);
                if (plant == null)
                {
                    if (Instances.GameplayActivity.Board.GetGridItemAt(GridItemType.Crater, gridX, gridY) != null)
                        continue;

                    if (VersusState.ArenaSynced is ArenaType.Roof or ArenaType.RoofNight or ArenaType.China)
                    {
                        SeedPacketDefinitions.SpawnPlant(SeedType.Flowerpot, gridX, gridY, true);
                    }
                    else if (VersusState.ArenaSynced is ArenaType.Pool or ArenaType.PoolNight)
                    {
                        if (Instances.GameplayActivity.Board.mPlantRow[gridY] != PlantRowType.Pool)
                        {
                            continue;
                        }

                        SeedPacketDefinitions.SpawnPlant(SeedType.Lilypad, gridX, gridY, true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies the push back effect to a single zombie by starting the push back coroutine.
    /// </summary>
    /// <param name="zombie">The zombie to push back.</param>
    internal static void PushBackZombie(Zombie zombie)
    {
        zombie.mController.StartCoroutine(CoPushBackZombie(zombie));
    }

    /// <summary>
    /// Coroutine that smoothly pushes a zombie backward with an arcing trajectory.
    /// </summary>
    /// <param name="zombie">The zombie to push back.</param>
    /// <returns>An IEnumerator for coroutine execution.</returns>
    private static IEnumerator CoPushBackZombie(Zombie zombie)
    {
        var zombieNetworked = zombie.GetNetworked();
        if (zombieNetworked != null)
        {
            zombieNetworked.Event = EventState.PushBack;

            if (zombieNetworked.State is ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE)
            {
                zombieNetworked.State = null;
            }
        }

        float originalY = zombie.mPosY;
        float archY = 75f;
        float startX = zombie.mPosX;
        float endX = zombie.mPosX + 150;
        float duration = 0.5f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            zombie.mPosX = Mathf.Lerp(startX, endX, t);
            float archFactor = 4f * t * (1f - t);
            zombie.mPosY = originalY - archY * archFactor;

            yield return null;
        }

        zombie.mPosX = endX;
        zombie.mPosY = originalY;
        if (zombieNetworked != null)
        {
            zombieNetworked.LogicComponent.SyncedPosX = zombie.mPosX;
            zombieNetworked.Event = EventState.None;
        }
    }
}