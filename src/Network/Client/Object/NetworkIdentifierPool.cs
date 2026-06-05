using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Client.Object;

/// <summary>
/// Provides a pool of network identifiers for allocation and reuse.
/// </summary>
internal sealed class NetworkIdentifierPool : IDisposable
{
    private readonly Queue<uint> _availableIds = [];
    private readonly HashSet<uint> _allocatedIds = [];

    internal NetworkIdentifierPool()
    {
        for (uint i = 0; i <= 100000 * ReplantedOnlineMod.Constants.Network.MAX_NETWORK_CHILDREN; i += ReplantedOnlineMod.Constants.Network.MAX_NETWORK_CHILDREN)
        {
            _availableIds.Enqueue(i);
        }
    }

    /// <summary>
    /// Allocates a network identifier from the pool.
    /// </summary>
    /// <returns>A new <see cref="NetworkIdentifier"/> that is tracked as allocated.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no identifiers are available in the pool.
    /// </exception>
    internal NetworkIdentifier Allocate()
    {
        if (_availableIds.Count == 0)
        {
            throw new InvalidOperationException("No available network identifiers in the pool.");
        }

        uint id = _availableIds.Dequeue();
        _allocatedIds.Add(id);
        return NetworkIdentifier.Create(id);
    }

    /// <summary>
    /// Releases a network identifier back into the pool.
    /// </summary>
    /// <param name="id">The network identifier to free.</param>
    internal void Free(NetworkIdentifier id)
    {
        if (ReloadedClientData.LocalClient!.GetClientIndex() != id.ClientIndex)
        {
            return;
        }

        if (_allocatedIds.Remove(id.LocalId))
        {
            _availableIds.Enqueue(id.LocalId);
        }
    }

    /// <summary>
    /// Gets the number of available (unallocated) identifiers remaining in the pool.
    /// </summary>
    internal int AvailableCount => _availableIds.Count;

    /// <summary>
    /// Releases all resources used by the <see cref="NetworkIdentifierPool"/>.
    /// </summary>
    public void Dispose()
    {
        _availableIds.Clear();
        _allocatedIds.Clear();
    }
}