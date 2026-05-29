namespace ReplantedOnline.Modules.Modded;

/// <summary>
/// Provides a dynamic weak reference to a source object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the referenced object, which must be a reference type.</typeparam>
internal sealed class WeakVar<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeakVar{T}"/> class with no initial delegate.
    /// The target must be set later using <see cref="SetTarget"/> before accessing <see cref="Value"/>.
    /// </summary>
    internal WeakVar() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakVar{T}"/> class with the specified getter delegate.
    /// </summary>
    /// <param name="get">A function that returns the target object of type <typeparamref name="T"/> when invoked.</param>
    internal WeakVar(Func<T> get)
    {
        _getTarget = get;
    }

    /// <summary>
    /// Gets the current target value by invoking the internal getter delegate.
    /// </summary>
    /// <returns>
    /// The referenced object of type <typeparamref name="T"/>, or <c>null</c> if the delegate is <c>null</c>, 
    /// the delegate returns <c>null</c>, or any intermediate property in the delegate's resolution path is <c>null</c>.
    /// </returns>
    internal T Value => _getTarget?.Invoke();

    private Func<T> _getTarget;

    /// <summary>
    /// Sets or updates the delegate used to retrieve the target object dynamically.
    /// </summary>
    /// <param name="get">A function that returns the target object of type <typeparamref name="T"/> when invoked.</param>
    internal void SetTarget(Func<T> get)
    {
        if (_getTarget != null && Value != null)
        {
            return;
        }

        _getTarget = get;
    }
}