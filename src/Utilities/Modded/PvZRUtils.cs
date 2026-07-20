using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppReloaded.Utils;
using Il2CppSource.Controllers;
using Il2CppSpine.Unity;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using UnityEngine;
using UnityEngine.Rendering;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides extension methods for game-specific types to simplify common operations
/// in multiplayer scenarios.
/// </summary>
internal static class PvZRUtils
{
    /// <summary>
    /// Converts a X position field to X position on board.
    /// </summary>
    /// <param name="posX">The world X coordinate to convert.</param>
    /// <returns>The corresponding board X position after applying the transformation.</returns>
    internal static float GetGridOffsetXPosFromBoardXPos(float posX)
    {
        return posX * 2.92f + Instances.GameplayActivity?.m_boardOffset?.localPosition.x ?? 975f;
    }


    /// <summary>
    /// Converts a ReloadedObject X position to a grid index based on the game's grid spacing.
    /// </summary>
    /// <param name="X">The ReloadedObject X coordinate to convert.</param>
    /// <returns>
    /// The grid index.
    /// </returns>
    internal static int ReloadedObjectXToGridX(float X)
    {
        return (int)((X - 40) / 80f);
    }

    /// <summary>
    /// Converts a ReloadedObject Y position to a grid index based on the game's grid spacing.
    /// </summary>
    /// <param name="Y">The ReloadedObject Y coordinate to convert.</param>
    /// <returns>
    /// The grid index.
    /// </returns>
    internal static int ReloadedObjectYToGridY(float Y)
    {
        return (int)(Y / 80f) - 1;
    }

    /// <summary>
    /// Converts a ReloadedObject X position to a normalized grid position.
    /// </summary>
    /// <param name="X">The ReloadedObject X coordinate to convert.</param>
    /// <returns>
    /// The normalized grid position as a float.
    /// </returns>
    internal static float ReloadedObjectXToGridXNormalized(float X)
    {
        return (X - 40) / 80f;
    }

    /// <summary>
    /// Converts a ReloadedObject Y position to a normalized grid position.
    /// </summary>
    /// <param name="Y">The ReloadedObject Y coordinate to convert.</param>
    /// <returns>
    /// The normalized grid position as a float.
    /// </returns>
    internal static float ReloadedObjectYToGridYNormalized(float Y)
    {
        return (Y / 80f) - 1f;
    }

    /// <summary>
    /// Gets the local player's item from a multiplayer collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the multiplayer collection.</typeparam>
    /// <param name="multiplayerType">The multiplayer collection instance.</param>
    /// <returns>The item associated with the local player.</returns>
    internal static T LocalItem<T>(this MultiplayerType<T> multiplayerType)
    {
        return multiplayerType[ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX];
    }

    /// <summary>
    /// Gets the opponent player's item from a multiplayer collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the multiplayer collection.</typeparam>
    /// <param name="multiplayerType">The multiplayer collection instance.</param>
    /// <returns>The item associated with the opponent player.</returns>
    internal static T OpponentItem<T>(this MultiplayerType<T> multiplayerType)
    {
        return multiplayerType[ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX];
    }

    /// <summary>
    /// Converts the items in a DataArray to an array.
    /// </summary>
    internal static T[] GetItems<T>(this DataArray<T> data) where T : class, new()
    {
        var enumerator = data.m_itemLookup.Keys.GetEnumerator();
        var array = new T[data.m_itemLookup.Count];
        var count = 0;
        while (enumerator.MoveNext())
        {
            array[count] = enumerator.Current;
            count++;
        }
        enumerator.Dispose();
        return array;
    }

    /// <summary>
    /// Retrieves all pooled items from the DataArray as a new array.
    /// This creates a copy of the pooled items collection, not a reference to the internal array.
    /// </summary>
    /// <typeparam name="T">The type of items in the DataArray, must be a class with a parameterless constructor.</typeparam>
    /// <param name="data">The DataArray instance containing the pooled items.</param>
    /// <returns>A new array containing all items from the pooled items collection.</returns>
    internal static T[] GetPooled<T>(this DataArray<T> data) where T : class, new()
    {
        return data.m_pooledItems.ToArray();
    }

    /// <summary>
    /// Retrieves all zombies from the board as an array.
    /// Uses the optimized Items() extension method for efficient array conversion.
    /// </summary>
    /// <param name="board">The game board instance containing zombie data.</param>
    /// <returns>An array containing all zombies present on the board.</returns>
    internal static Zombie[] GetZombies(this Board board)
    {
        return board.m_zombies.GetItems();
    }

    /// <summary>
    /// Retrieves all plants from the board as an array.
    /// Uses the optimized Items() extension method for efficient array conversion.
    /// </summary>
    /// <param name="board">The game board instance containing plant data.</param>
    /// <returns>An array containing all plants present on the board.</returns>
    internal static Plant[] GetPlants(this Board board)
    {
        return board.m_plants.GetItems();
    }

    /// <summary>
    /// Checks if the zombie type is either a target zombie or a gravestone.
    /// </summary>
    /// <param name="zombieType">The zombie type to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the zombie type is <see cref="ZombieType.Target"/> or <see cref="ZombieType.Gravestone"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsGravestoneOrTarget(this ZombieType zombieType)
    {
        return zombieType is ZombieType.Target or ZombieType.Gravestone;
    }


    /// <summary>
    /// Gets the team associated with the seed bank.
    /// </summary>
    /// <param name="seedBank">The seed bank reference.</param>
    /// <returns>
    /// <see cref="PlayerTeam.Plants"/> if the seed bank belongs to the board's main seed bank;
    /// otherwise, <see cref="PlayerTeam.Zombies"/>.
    /// </returns>
    internal static PlayerTeam GetSeedBankTeam(this SeedBank seedBank)
    {
        if (ReloadedClientData.LocalClient!.Team == PlayerTeam.Zombies)
        {
            return seedBank.PlayerIndex == ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX ? PlayerTeam.Plants : PlayerTeam.Zombies;
        }
        else
        {
            return seedBank.PlayerIndex == ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX ? PlayerTeam.Plants : PlayerTeam.Zombies;
        }
    }

    /// <summary>
    /// Gets the seed bank information for the plant side.
    /// </summary>
    /// <returns>
    /// The <see cref="SeedChooserScreen.SeedBankInfo"/> instance containing the plants seed bank data.
    /// </returns>
    internal static SeedChooserScreen.SeedBankInfo GetPlantSeedBankInfo()
    {
        if (Instances.GameplayActivity?.SeedChooserScreen == null)
            return null!;

        foreach (var bankInfo in Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items)
        {
            if (bankInfo.mSeedBank.GetSeedBankTeam() == PlayerTeam.Plants)
            {
                return bankInfo;
            }
        }

        throw new Exception("Unable to find plant seed bank.");
    }

    /// <summary>
    /// Gets the seed bank information for the zombie side.
    /// </summary>
    /// <returns>
    /// The <see cref="SeedChooserScreen.SeedBankInfo"/> instance containing the zombies seed bank data.
    /// </returns>
    internal static SeedChooserScreen.SeedBankInfo GetZombieSeedBankInfo()
    {
        if (Instances.GameplayActivity?.SeedChooserScreen == null)
            return null!;

        foreach (var bankInfo in Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items)
        {
            if (bankInfo.mSeedBank.GetSeedBankTeam() == PlayerTeam.Zombies)
            {
                return bankInfo;
            }
        }

        throw new Exception("Unable to find zombie seed bank.");
    }

    /// <summary>
    /// Adds a seed from the seed chooser screen to the seed bank and updates its state.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank info to add the seed to.</param>
    /// <param name="seedType">The type of seed to add.</param>
    internal static void AddSeedFromChooser(this SeedChooserScreen.SeedBankInfo seedBankInfo, SeedType seedType)
    {
        seedBankInfo.mSeedsInBank++;
        seedBankInfo.mSeedBank.AddSeed(seedType, true);

        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        foreach (var seedPacket in chosenSeeds)
        {
            if (seedPacket.mSeedType == seedType)
            {
                seedPacket.mSeedState = ChosenSeedState.SeedInBank;
                break;
            }
        }
    }

    /// <summary>
    /// Clears all seeds from the specified seed bank by removing each non empty seed packet.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank information containing the seed bank to clear.</param>
    internal static void ClearAllSeedsInSeedBack(this SeedChooserScreen.SeedBankInfo seedBankInfo)
    {
        List<SeedType> removed = [];
        for (int i = 0; i < seedBankInfo.mSeedsInBank; i++)
        {
            var packet = seedBankInfo.mSeedBank.SeedPackets[i];
            seedBankInfo.mSeedBank.RemoveSeed(i);
            removed.Add(packet.mPacketType);
        }
        seedBankInfo.mSeedsInBank = 0;

        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        foreach (var seedPacket in chosenSeeds)
        {
            if (!removed.Contains(seedPacket.mSeedType)) continue;

            seedPacket.mSeedState = ChosenSeedState.SeedInChooser;
            seedPacket.mSeedIndexInBank = -1;
        }
    }

    /// <summary>
    /// Initializes a lawn mower instance with the appropriate settings based on the game state and arena type.
    /// </summary>
    /// <param name="lawnMower">The lawn mower instance to initialize.</param>
    /// <param name="row">The row in which the lawn mower is located.</param>
    /// <param name="app">The gameplay activity instance.</param>
    internal static void LawnMowerInitialize(LawnMower lawnMower, int row, GameplayActivity app)
    {
        // Set basic references
        lawnMower.mApp = app;
        lawnMower.mBoard = app.Board;
        lawnMower.mRow = row;

        // Make render order
        lawnMower.mRenderOrder = Board.MakeRenderOrder(RenderLayer.LawnMower, row, 0);
        lawnMower.mShadowRenderOrder = lawnMower.RenderOrder - 100;

        // Get Y position based on row
        float posY = lawnMower.mBoard.GetPosYBasedOnRow(19f, row);
        lawnMower.mY = posY + 23f;
        lawnMower.X = -21;

        // Initialize state flags
        lawnMower.mDead = false;
        lawnMower.mMowerState = LawnMowerState.Ready;
        lawnMower.mVisible = true;
        lawnMower.mMowerHeight = MowerHeight.Land;

        // Initialize other fields
        lawnMower.mChompCounter = 0;
        lawnMower.mRollingInCounter = 0;
        lawnMower.mSquishedCounter = 0;
        lawnMower.mLastPortalX = -1;

        // Determine mower type based on Arena
        if (VersusState.Arena is ArenaTypes.Roof or ArenaTypes.RoofNight)
        {
            lawnMower.mMowerType = LawnMowerType.Roof;
            lawnMower.mMowerState = LawnMowerState.Ready;
        }
        else if (VersusState.Arena is ArenaTypes.Pool or ArenaTypes.PoolNight)
        {
            var groundType = lawnMower.mBoard.mPlantRow[row];
            if (groundType == PlantRowType.Pool)
            {
                lawnMower.mMowerType = LawnMowerType.Pool;
            }
            else
            {
                lawnMower.mMowerType = LawnMowerType.Lawn;
            }
        }
        else
        {
            lawnMower.mMowerType = LawnMowerType.Lawn;
        }

        // Create controller
        lawnMower.mController = app.CreateLawnMowerController(lawnMower);

        if (lawnMower.mController != null)
        {
            // Set initial position
            lawnMower.mController.SetPosition(0f, 19f, 0f);

            // Set scale and animation based on type
            switch (lawnMower.mMowerType)
            {
                case LawnMowerType.Lawn:
                    lawnMower.mController.OverrideScale(0.9f, 0.9f);
                    lawnMower.mController.PlayAnimationOnTrack(Animations.MOWER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.MOWER_IDLE.Fps, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.SuperMower:
                    lawnMower.mController.OverrideScale(0.8f, 0.8f);
                    lawnMower.mController.PlayAnimationOnTrack(Animations.SUPER_MOWER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.SUPER_MOWER_IDLE.Fps, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.Roof:
                    lawnMower.mController.PlayAnimationOnTrack(Animations.ROOF_MOWER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.ROOF_MOWER_IDLE.Fps, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.Pool:
                    lawnMower.mController.PlayAnimationOnTrack(Animations.POOL_MOWER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.POOL_MOWER_IDLE.Fps, ReanimLoopType.Loop);
                    break;
            }
        }

        // Set shadow offsets
        float altitude = lawnMower.mAltitude;
        lawnMower.mShadowOffsetX = -28f;
        lawnMower.mShadowOffsetY = 47f - altitude;

        if (lawnMower.mMowerType == LawnMowerType.SuperMower)
        {
            lawnMower.mShadowOffsetX = -24f;
            lawnMower.mShadowOffsetY = 47f - altitude - 8f;
        }
        else if (lawnMower.mMowerType == LawnMowerType.Roof)
        {
            lawnMower.mShadowScaleY = 1.2f;
            lawnMower.mShadowOffsetX -= 9f;
        }

        // Final position adjustments
        float finalY = -altitude;
        if (lawnMower.mMowerType == LawnMowerType.SuperMower)
        {
            finalY = -altitude - 33f;
        }
        else if (lawnMower.mMowerType == LawnMowerType.Roof)
        {
            finalY = -altitude - 40f;
        }

        // Set final position
        lawnMower.mController?.SetPosition(6f, finalY, 0f);

        // Force skeleton update
        if (lawnMower.mController != null)
        {
            var skeleton = lawnMower.mController.GetComponent<SkeletonAnimation>();
            skeleton?.Update(0f);
        }
    }

    /// <summary>
    /// Create a new bowling line on the current stage.
    /// </summary>
    /// <returns>The SpriteRenderer for the bowling line.</returns>
    internal static SpriteRenderer CreateBowlingLine(Sprite sprite, bool addSortingGroup = true)
    {
        if (Instances.GameplayActivity.BackgroundController.m_bowlingLine != null)
        {
            UnityEngine.Object.Destroy(Instances.GameplayActivity.BackgroundController.m_bowlingLine);
        }

        var bowlingLineGo = new GameObject("P_WalnutBowlingLine");
        bowlingLineGo.transform.SetParent(Instances.GameplayActivity.m_boardOffset);
        bowlingLineGo.transform.SetSiblingIndex(0);
        bowlingLineGo.transform.localPosition = new(0f, -907.26f, -1f);
        bowlingLineGo.transform.localScale = new(97f, 97f, 1f);
        Instances.GameplayActivity.BackgroundController.m_bowlingLine = bowlingLineGo;
        var spriteRenderer = bowlingLineGo.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        if (addSortingGroup)
        {
            var sortingGroup = bowlingLineGo.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = Il2CppReloaded.Constants.SortingLayer.BACKGROUND;
        }
        return spriteRenderer;
    }
}