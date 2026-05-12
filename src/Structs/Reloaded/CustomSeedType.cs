using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Structs.Reloaded;

/// <summary>
/// Represents a custom seed type that extends the base SeedType enumeration
/// with additional zombie and reanimation type associations.
/// </summary>
internal readonly struct CustomSeedType
{
    /// <summary>
    /// Readonly list of all CustomSeedTypes.
    /// </summary>
    internal static IReadOnlyList<CustomSeedType> CustomSeedTypes => _lookup.Values.ToList().AsReadOnly();

    private static readonly Dictionary<SeedType, CustomSeedType> _lookup = [];

    // Custom SeedTypes
    /// <summary>
    /// Represents an invalid or uninitialized custom seed type.
    /// </summary>
    internal static CustomSeedType Invalid { get; } = new(int.MinValue);

    /// <summary>
    /// Custom seed type for the Dolphin Rider zombie.
    /// </summary>
    internal static CustomSeedType DolphinRider { get; } = new(10000, ZombieType.DolphinRider, ReanimationType.ZombieDolphinrider);

    /// <summary>
    /// Custom seed type for the Snorkel zombie.
    /// </summary>
    internal static CustomSeedType Snorkel { get; } = new(20000, ZombieType.Snorkel, ReanimationType.Snorkel);

    /// <summary>
    /// Checks if a <see cref="SeedType"/> is a CustomSeedType.
    /// </summary>
    /// <param name="seedType">The <see cref="SeedType"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <see cref="SeedType"/> is custom <see cref="ZombieType.Invalid"/>; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsCustomSeedType(SeedType seedType)
    {
        return _lookup.ContainsKey(seedType);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomSeedType"/> struct.
    /// </summary>
    /// <param name="id">The unique identifier for the custom seed type.</param>
    /// <param name="zombieType">The associated zombie type. Defaults to <see cref="ZombieType.Invalid"/>.</param>
    /// <param name="reanimationType">The associated reanimation type. Defaults to <see cref="ReanimationType.None"/>.</param>
    internal CustomSeedType(int id, ZombieType zombieType = ZombieType.Invalid, ReanimationType reanimationType = ReanimationType.None)
    {
        _id = id;
        _zombieType = zombieType;
        _reanimationType = reanimationType;
        _lookup[(SeedType)id] = this;
    }

    private readonly int _id;
    private readonly ZombieType _zombieType;
    private readonly ReanimationType _reanimationType;

    /// <summary>
    /// Determines whether this custom seed type has a valid associated zombie type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the zombie type is not <see cref="ZombieType.Invalid"/>; otherwise, <c>false</c>.
    /// </returns>
    internal bool HasValidZombieType()
    {
        return _zombieType != ZombieType.Invalid;
    }

    /// <summary>
    /// Determines whether this custom seed type has a valid associated reanimation type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the reanimation type is not <see cref="ReanimationType.None"/>; otherwise, <c>false</c>.
    /// </returns>
    internal bool HasValidReanimationType()
    {
        return _reanimationType != ReanimationType.None;
    }

    /// <summary>
    /// Implicitly converts a <see cref="SeedType"/> to a <see cref="CustomSeedType"/>.
    /// </summary>
    /// <param name="seedType">The seed type to convert.</param>
    /// <returns>
    /// The corresponding <see cref="CustomSeedType"/> if found in the lookup table;
    /// otherwise, returns <see cref="Invalid"/>.
    /// </returns>
    public static implicit operator CustomSeedType(SeedType seedType)
    {
        if (_lookup.TryGetValue(seedType, out var customSeedType))
        {
            return customSeedType;
        }

        return Invalid;
    }

    /// <summary>
    /// Explicitly converts a <see cref="CustomSeedType"/> to a <see cref="SeedType"/>.
    /// </summary>
    /// <param name="customSeedType">The custom seed type to convert.</param>
    /// <returns>The seed type representation of the custom seed type's identifier.</returns>
    public static implicit operator SeedType(CustomSeedType customSeedType)
    {
        return (SeedType)customSeedType._id;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CustomSeedType"/> to a <see cref="ZombieType"/>.
    /// </summary>
    /// <param name="customSeedType">The custom seed type to convert.</param>
    /// <returns>The associated zombie type of the custom seed type.</returns>
    public static implicit operator ZombieType(CustomSeedType customSeedType)
    {
        return customSeedType._zombieType;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CustomSeedType"/> to a <see cref="ReanimationType"/>.
    /// </summary>
    /// <param name="customSeedType">The custom seed type to convert.</param>
    /// <returns>The associated reanimation type of the custom seed type.</returns>
    public static implicit operator ReanimationType(CustomSeedType customSeedType)
    {
        return customSeedType._reanimationType;
    }

    /// <summary>
    /// Determines whether two specified instances of <see cref="CustomSeedType"/> are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><c>true</c> if left and right represent the same custom seed type; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CustomSeedType left, CustomSeedType right)
    {
        return left._id == right._id;
    }

    /// <summary>
    /// Determines whether two specified instances of <see cref="CustomSeedType"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><c>true</c> if left and right do not represent the same custom seed type; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CustomSeedType left, CustomSeedType right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
    public override bool Equals(object obj)
    {
        if (obj is CustomSeedType other)
        {
            return this == other;
        }
        return false;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}