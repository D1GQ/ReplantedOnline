using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppReloaded.Utils;
using Il2CppSource.Controllers;
using Il2CppSpine.Unity;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;

namespace ReplantedOnline.Utilities;

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
    /// Gets the local player's item from a multiplayer collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the multiplayer collection.</typeparam>
    /// <param name="multiplayerType">The multiplayer collection instance.</param>
    /// <returns>The item associated with the local player.</returns>
    internal static T LocalItem<T>(this MultiplayerType<T> multiplayerType)
    {
        return multiplayerType[ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX];
    }

    /// <summary>
    /// Gets the opponent player's item from a multiplayer collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the multiplayer collection.</typeparam>
    /// <param name="multiplayerType">The multiplayer collection instance.</param>
    /// <returns>The item associated with the opponent player.</returns>
    internal static T OpponentItem<T>(this MultiplayerType<T> multiplayerType)
    {
        return multiplayerType[ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX];
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
    /// Gets the movement direction of the zombie based on its current state.
    /// </summary>
    /// <param name="zombie">The zombie instance to get the move direction for.</param>
    /// <returns>
    /// The movement direction value
    /// </returns>
    internal static float GetZombieMoveDirection(this Zombie zombie)
    {
        float speed = zombie.mVelX + 0.30f;

        if (!zombie.IsWalkingBackwards())
        {
            return -speed;
        }
        else
        {
            return speed;
        }
    }

    /// <summary>
    /// Checks if the zombie type is a target zombie or gravestone.
    /// </summary>
    /// <param name="zombieType">The zombie type.</param>
    /// <returns></returns>
    internal static bool IsGravestoneOrTarget(this ZombieType zombieType)
    {
        return zombieType is ZombieType.Target or ZombieType.Gravestone;
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
    /// Gets the seed bank information for the local player.
    /// </summary>
    /// <returns>
    /// The <see cref="SeedChooserScreen.SeedBankInfo"/> instance containing the local player's seed bank data.
    /// </returns>
    internal static SeedChooserScreen.SeedBankInfo GetLocalSeedBankInfo()
    {
        return Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items[ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX];
    }

    /// <summary>
    /// Gets the seed bank information for the opponent player.
    /// </summary>
    /// <returns>
    /// The <see cref="SeedChooserScreen.SeedBankInfo"/> instance containing the opponent player's seed bank data.
    /// </returns>
    internal static SeedChooserScreen.SeedBankInfo GetOpponentSeedBankInfo()
    {
        return Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items[ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX];
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
        else if (VersusState.Arena is ArenaTypes.Pool)
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
                    lawnMower.mController.PlayAnimationOnTrack("normal", CharacterAnimationTrack.Body, 0f, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.SuperMower:
                    lawnMower.mController.OverrideScale(0.8f, 0.8f);
                    lawnMower.mController.PlayAnimationOnTrack("", CharacterAnimationTrack.Body, 0f, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.Roof:
                    lawnMower.mController.PlayAnimationOnTrack("default", CharacterAnimationTrack.Body, 0f, ReanimLoopType.Loop);
                    break;
                case LawnMowerType.Pool:
                    lawnMower.mController.PlayAnimationOnTrack("land", CharacterAnimationTrack.Body, 0f, ReanimLoopType.Loop);
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
            lawnMower.mShadowOffsetY = (47f - altitude) - 8f;
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
}