namespace ReplantedOnline.Interfaces;

/// <summary>
/// Defines a contract for objects that expose a <see cref="Type"/> they were caught or captured as.
/// </summary>
internal interface ICatchedType
{
    /// <summary>
    /// Gets the <see cref="Type"/> that this object was caught or captured as.
    /// </summary>
    Type CatchedType { get; }
}