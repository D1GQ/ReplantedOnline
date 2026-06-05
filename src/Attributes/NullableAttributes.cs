#pragma warning disable IDE0130

namespace System.Runtime.CompilerServices;

/// <summary>
/// Represents an external initialization marker for init-only properties and records.
/// </summary>
internal static class IsExternalInit { }

/// <summary>
/// Specifies nullability annotations for reference types.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
internal sealed class NullableAttribute : Attribute
{
    /// <summary>
    /// Gets the array of nullability flags for the annotated element.
    /// </summary>
    public readonly byte[] NullableFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableAttribute"/> class with a single nullability flag.
    /// </summary>
    /// <param name="flag">The nullability flag value.</param>
    public NullableAttribute(byte flag) { NullableFlags = [flag]; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableAttribute"/> class with multiple nullability flags.
    /// </summary>
    /// <param name="flags">An array of nullability flag values.</param>
    public NullableAttribute(byte[] flags) { NullableFlags = flags; }
}

/// <summary>
/// Specifies the nullability context for reference types within a scope.
/// </summary>
/// <param name="flag">The nullability context flag value.</param>
[AttributeUsage(AttributeTargets.All)]
internal sealed class NullableContextAttribute(byte flag) : Attribute
{
    /// <summary>
    /// Gets the nullability context flag value.
    /// </summary>
    public readonly byte Flag = flag;
}