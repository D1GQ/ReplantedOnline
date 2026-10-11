namespace ReplantedOnline.Structs.Network;

/// <summary>
/// A lightweight wrapper around an <see cref="int"/> that behaves like a normal integer
/// </summary>
internal readonly struct PackedInt
{
    private readonly int _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackedInt"/> struct.
    /// </summary>
    /// <param name="value">The underlying integer value.</param>
    public PackedInt(int value) => _value = value;

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="PackedInt"/>.
    /// </summary>
    /// <param name="value">The integer value to wrap.</param>
    public static implicit operator PackedInt(int value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="PackedInt"/> to its underlying <see cref="int"/> value.
    /// </summary>
    /// <param name="packed">The <see cref="PackedInt"/> to unwrap.</param>
    public static implicit operator int(PackedInt packed) => packed._value;

    /// <summary>Adds two <see cref="PackedInt"/> values.</summary>
    public static PackedInt operator +(PackedInt a, PackedInt b) => new(a._value + b._value);

    /// <summary>Subtracts one <see cref="PackedInt"/> value from another.</summary>
    public static PackedInt operator -(PackedInt a, PackedInt b) => new(a._value - b._value);

    /// <summary>Multiplies two <see cref="PackedInt"/> values.</summary>
    public static PackedInt operator *(PackedInt a, PackedInt b) => new(a._value * b._value);

    /// <summary>Divides one <see cref="PackedInt"/> value by another.</summary>
    public static PackedInt operator /(PackedInt a, PackedInt b) => new(a._value / b._value);

    /// <summary>Computes the remainder of dividing one <see cref="PackedInt"/> value by another.</summary>
    public static PackedInt operator %(PackedInt a, PackedInt b) => new(a._value % b._value);

    /// <summary>Increments a <see cref="PackedInt"/> value by one.</summary>
    public static PackedInt operator ++(PackedInt a) => new(a._value + 1);

    /// <summary>Decrements a <see cref="PackedInt"/> value by one.</summary>
    public static PackedInt operator --(PackedInt a) => new(a._value - 1);

    /// <summary>Negates a <see cref="PackedInt"/> value.</summary>
    public static PackedInt operator -(PackedInt a) => new(-a._value);

    /// <summary>Computes the bitwise AND of two <see cref="PackedInt"/> values.</summary>
    public static PackedInt operator &(PackedInt a, PackedInt b) => new(a._value & b._value);

    /// <summary>Computes the bitwise OR of two <see cref="PackedInt"/> values.</summary>
    public static PackedInt operator |(PackedInt a, PackedInt b) => new(a._value | b._value);

    /// <summary>Computes the bitwise exclusive OR (XOR) of two <see cref="PackedInt"/> values.</summary>
    public static PackedInt operator ^(PackedInt a, PackedInt b) => new(a._value ^ b._value);

    /// <summary>Computes the bitwise complement of a <see cref="PackedInt"/> value.</summary>
    public static PackedInt operator ~(PackedInt a) => new(~a._value);

    /// <summary>Shifts a <see cref="PackedInt"/> value left by the specified number of bits.</summary>
    /// <param name="a">The value to shift.</param>
    /// <param name="shift">The number of bits to shift by.</param>
    public static PackedInt operator <<(PackedInt a, int shift) => new(a._value << shift);

    /// <summary>Shifts a <see cref="PackedInt"/> value right by the specified number of bits.</summary>
    /// <param name="a">The value to shift.</param>
    /// <param name="shift">The number of bits to shift by.</param>
    public static PackedInt operator >>(PackedInt a, int shift) => new(a._value >> shift);

    /// <summary>Determines whether two <see cref="PackedInt"/> values are equal.</summary>
    public static bool operator ==(PackedInt a, PackedInt b) => a._value == b._value;

    /// <summary>Determines whether two <see cref="PackedInt"/> values are not equal.</summary>
    public static bool operator !=(PackedInt a, PackedInt b) => a._value != b._value;

    /// <summary>Determines whether the left <see cref="PackedInt"/> is less than the right.</summary>
    public static bool operator <(PackedInt a, PackedInt b) => a._value < b._value;

    /// <summary>Determines whether the left <see cref="PackedInt"/> is greater than the right.</summary>
    public static bool operator >(PackedInt a, PackedInt b) => a._value > b._value;

    /// <summary>Determines whether the left <see cref="PackedInt"/> is less than or equal to the right.</summary>
    public static bool operator <=(PackedInt a, PackedInt b) => a._value <= b._value;

    /// <summary>Determines whether the left <see cref="PackedInt"/> is greater than or equal to the right.</summary>
    public static bool operator >=(PackedInt a, PackedInt b) => a._value >= b._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PackedInt other && _value == other._value;

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Returns the string representation of the underlying integer value.
    /// </summary>
    /// <returns>The underlying value formatted as a string.</returns>
    public override string ToString() => _value.ToString();
}