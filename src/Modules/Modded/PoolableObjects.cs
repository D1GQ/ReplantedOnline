using System.Collections.Concurrent;

namespace ReplantedOnline.Modules.Modded;

/// <summary>
/// Provides a generic object pool for reusing instances of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must have a parameterless constructor.</typeparam>
/// <param name="maxPoolSize">The maximum number of objects to keep in the pool. Default is 100.</param>
internal class PoolableObjects<T>(int maxPoolSize = 100) where T : new()
{
    private readonly ConcurrentQueue<T> _pool = new();
    private int _amountInUse;
    private readonly int _maxPoolSize = maxPoolSize;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the current number of objects that have been retrieved but not yet released.
    /// </summary>
    internal int AmountInUse
    {
        get
        {
            lock (_lock)
            {
                return _amountInUse;
            }
        }
    }

    /// <summary>
    /// Retrieves an object from the pool. If the pool is empty, a new instance is created.
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/> from the pool or a new instance.</returns>
    internal T Get()
    {
        lock (_lock)
        {
            _amountInUse++;
        }

        if (_pool.TryDequeue(out var item))
        {
            return item;
        }

        return new T();
    }

    /// <summary>
    /// Returns an object to the pool for reuse.
    /// </summary>
    /// <param name="item">The object to return to the pool.</param>
    internal void Release(T item)
    {
        if (item == null) return;

        lock (_lock)
        {
            _amountInUse--;

            if (_pool.Count < _maxPoolSize)
            {
                _pool.Enqueue(item);
            }
        }
    }

    /// <summary>
    /// Clears all objects from the pool and resets the count of objects in use to zero.
    /// </summary>
    internal void Clear()
    {
        lock (_lock)
        {
            _amountInUse = 0;
            _pool.Clear();
        }
    }
}