using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Utils;
using ReplantedOnline.Modules;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides extension methods for game-specific types to simplify common operations
/// in multiplayer scenarios.
/// </summary>
internal static class PvZRHelper
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
}