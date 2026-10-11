namespace ReplantedOnline.Interfaces;

/// <summary>
/// Defines a contract for objects that expose a <see cref="System.Type"/> they were caught or captured as.
/// </summary>
internal interface ICatchedType
{
    /// <summary>
    /// Gets the <see cref="System.Type"/> associated with this instance.
    /// </summary>
    Type Type { get; }
}