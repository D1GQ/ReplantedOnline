namespace ReplantedOnline.Utilities;

/// <summary>
/// Provides extension methods for shuffling collections.
/// </summary>
internal static class ShuffleListExtension
{
    private readonly static Random _random = new();

    /// <summary>
    /// Shuffles all elements in a collection randomly using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of the collection elements.</typeparam>
    /// <param name="collection">The collection to be shuffled.</param>
    /// <returns>The shuffled collection.</returns>
    internal static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }
}