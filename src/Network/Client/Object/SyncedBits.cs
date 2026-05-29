namespace ReplantedOnline.Network.Client.Object;

/// <summary>
/// Manages state tracking using bit flags.
/// </summary>
internal sealed class SyncedBits
{
    /// <summary>
    /// Gets a value indicating whether any bits are currently marked as dirty.
    /// </summary>
    internal bool IsDirty => SyncedDirtyBits > 0U;

    /// <summary>
    /// Gets or sets the current state of dirty bits as a bitmask.
    /// </summary>
    internal uint SyncedDirtyBits { get; set; }

    /// <summary>
    /// Checks if a specific dirty bit is set at the given index.
    /// </summary>
    /// <param name="idx">The zero-based index of the bit to check.</param>
    /// <returns>True if the bit at the specified index is set, indicating the item is dirty.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (SyncedDirtyBits & 1U << idx) > 0U;
    }
}