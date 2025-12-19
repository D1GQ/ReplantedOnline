namespace ReplantedOnline.Network.Object;

/// <summary>
/// Manages synchronization state for network objects using bit flags.
/// Tracks which properties have changed and need to be synchronized across the network.
/// </summary>
internal sealed class SyncedBits
{
    /// <summary>
    /// Gets a value indicating whether any synchronization bits are currently dirty.
    /// Returns true if any properties have changed since the last network sync.
    /// </summary>
    internal bool IsDirty => SyncedDirtyBits > 0U;

    /// <summary>
    /// Gets or sets the current state of dirty bits as a bitmask.
    /// Each bit represents whether a specific property has been modified.
    /// </summary>
    internal uint SyncedDirtyBits { get; set; }

    /// <summary>
    /// Checks if a specific dirty bit is set at the given index.
    /// Used to determine if a particular property needs synchronization.
    /// </summary>
    /// <param name="idx">The zero-based index of the bit to check.</param>
    /// <returns>True if the bit at the specified index is set, indicating the property is dirty.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (SyncedDirtyBits & 1U << idx) > 0U;
    }
}