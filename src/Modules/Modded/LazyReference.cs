namespace ReplantedOnline.Modules.Modded;

/// <summary>
/// Provides a dynamic weak reference to a source object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the referenced object, which must be a reference type.</typeparam>
internal sealed class LazyReference<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyReference{T}"/> class with no initial delegate.
    /// </summary>
    internal LazyReference() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyReference{T}"/> class with the specified getter delegate.
    /// </summary>
    /// <param name="getter">A function that returns the target object of type <typeparamref name="T"/> when invoked.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="getter"/> is <c>null</c>.</exception>
    internal LazyReference(Func<T?> getter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        _getTarget = getter;
    }

    /// <summary>
    /// Gets the current target value by invoking the internal getter delegate.
    /// </summary>
    /// <returns>
    /// The referenced object of type <typeparamref name="T"/>, or <c>null</c>.
    /// </returns>
    internal T? Value => _getTarget?.Invoke();

    private Func<T?>? _getTarget;

    /// <summary>
    /// Sets or updates the delegate used to retrieve the target object dynamically.
    /// </summary>
    /// <param name="getter">A function that returns the target object of type <typeparamref name="T"/> when invoked.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="getter"/> is <c>null</c>.</exception>
    internal void SetTarget(Func<T?> getter)
    {
        ArgumentNullException.ThrowIfNull(getter);

        if (_getTarget != null && Value != null)
        {
            return;
        }

        _getTarget = getter;
    }
}