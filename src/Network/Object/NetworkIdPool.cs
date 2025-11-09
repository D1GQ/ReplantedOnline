namespace ReplantedOnline.Network.Object;

/// <summary>
/// Provides a pool of network IDs for allocation and reuse.
/// Manages a range of unsigned integer IDs from a specified start to end value.
/// </summary>
internal class NetworkIdPool
{
    private readonly Queue<uint> _availableIds = [];

    internal NetworkIdPool(uint start, uint end)
    {
        _start = start;
        _end = end;

        for (uint i = start; i <= end; i++)
        {
            _availableIds.Enqueue(i);
        }
    }

    private uint _start;

    internal uint _end;

    /// <summary>
    /// Retrieves an unused ID from the pool.
    /// </summary>
    /// <returns>An unused unsigned integer ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no IDs are available in the pool.</exception>
    internal uint GetUnusedId()
    {
        if (AvailableCount == 0)
            throw new InvalidOperationException("No available IDs in the pool");

        return _availableIds.Dequeue();
    }

    /// <summary>
    /// Releases an ID back to the pool for reuse.
    /// </summary>
    /// <param name="id">The ID to release back to the pool.</param>
    internal void ReleaseId(uint id)
    {
        if (!_availableIds.Contains(id) && (id >= _start && id <= _end))
        {
            _availableIds.Enqueue(id);
        }
    }

    /// <summary>
    /// Gets the number of available IDs in the pool.
    /// </summary>
    internal int AvailableCount => _availableIds.Count;
}