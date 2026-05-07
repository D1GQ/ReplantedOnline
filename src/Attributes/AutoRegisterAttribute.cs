using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Interfaces.Versus;
using System.Reflection;
using System.Runtime.Serialization;

namespace ReplantedOnline.Attributes;

/// <summary>
/// Base attribute class for automatically discovering and registering instances of specific types through reflection.
/// </summary>
internal abstract class AutoRegisterAttribute : Attribute
{
    /// <summary>
    /// Gets the enum value associated with this attribute, if applicable. Override in derived classes that support enum-based lookup.
    /// </summary>
    /// <returns>The associated enum value, or null if this attribute doesn't use enum-based lookup.</returns>
    internal virtual Enum GetLookupEnum() => null;

    /// <summary>
    /// Scans the entire assembly and registers all instances of classes marked with <see cref="AutoRegisterAttribute"/> subclasses.
    /// </summary>
    internal static void RegisterAll()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var types = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AutoRegisterAttribute)) && !t.IsAbstract && t.IsSealed)
            .ToArray();

        foreach (var type in types)
        {
            var attribute = (AutoRegisterAttribute)FormatterServices.GetUninitializedObject(type);
            attribute.RegisterInstances();
        }
    }

    /// <summary>
    /// When implemented in a derived class, registers instances of a specific type discovered through reflection.
    /// </summary>
    protected abstract void RegisterInstances();
}

/// <summary>
/// Generic attribute for automatically registering static instances of a specified base type or interface.
/// </summary>
/// <typeparam name="T">The base type or interface that attributed classes must implement.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
internal abstract class AutoRegisterAttribute<T> : AutoRegisterAttribute where T : class
{
    /// <summary>
    /// The collection of all discovered and registered instances of type <typeparamref name="T"/>.
    /// </summary>
    protected static readonly List<T> _instances = [];

    /// <summary>
    /// Gets a read-only collection of all registered instances of type <typeparamref name="T"/>.
    /// </summary>
    internal static IReadOnlyList<T> Instances => _instances.AsReadOnly();

    /// <summary>
    /// Retrieves a specific instance by its concrete type.
    /// </summary>
    /// <typeparam name="J">The concrete type of the instance to retrieve. Must inherit from or implement <typeparamref name="T"/>.</typeparam>
    /// <returns>The instance of type <typeparamref name="J"/> if found, otherwise null.</returns>
    internal static J GetInstance<J>() where J : T => (J)_instances.FirstOrDefault(instance => instance.GetType() == typeof(J));

    /// <inheritdoc/>
    protected override void RegisterInstances()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var attributedTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(GetType(), false).Any())
            .Where(t => !t.IsAbstract && !t.IsInterface); // Only instantiable types

        foreach (var type in attributedTypes)
        {
            // Check if the type implements the interface or inherits from the base class
            if (typeof(T).IsAssignableFrom(type))
            {
                // Try to get any parameterless constructor (public, private, or internal)
                var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

                if (constructor != null)
                {
                    if (constructor.Invoke(null) is T instance)
                    {
                        _instances.Add(instance);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Provides a base attribute class that extends <see cref="AutoRegisterAttribute{T}"/> with enum-based lookup capabilities.
/// </summary>
/// <typeparam name="T">The base type or interface that attributed classes must implement.</typeparam>
/// <typeparam name="EnumType">The enum type used as a key for instance lookup.</typeparam>
/// <param name="enumType">The specific enum value to associate with this attribute.</param>
[AttributeUsage(AttributeTargets.Class)]
internal abstract class AutoRegisterLookupAttribute<T, EnumType>(EnumType enumType) : AutoRegisterAttribute<T> where T : class where EnumType : Enum
{
    /// <summary>
    /// The enum value associated with instances registered by this attribute.
    /// </summary>
    private readonly EnumType _enumType = enumType;

    /// <summary>
    /// Static dictionary that maps enum values to their corresponding instances for fast lookup.
    /// </summary>
    private static readonly Dictionary<EnumType, T> _lookup = [];

    /// <summary>
    /// Gets the enum value associated with this attribute instance.
    /// </summary>
    /// <returns>The <see cref="Enum"/> value stored in this attribute.</returns>
    internal override Enum GetLookupEnum()
    {
        return _enumType;
    }

    /// <summary>
    /// Retrieves an instance from the lookup dictionary by its associated enum value.
    /// </summary>
    /// <param name="enum">The enum value to look up.</param>
    /// <returns>The instance associated with the specified enum value, or null if not found.</returns>
    internal static T GetInstanceFromLookup(EnumType @enum)
    {
        if (_lookup.TryGetValue(@enum, out var instanceLookup))
        {
            return instanceLookup;
        }

        return null;
    }

    /// <inheritdoc/>
    protected override void RegisterInstances()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var attributeType = GetType();

        var attributedTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        foreach (var type in attributedTypes)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                var attribute = type.GetCustomAttribute(attributeType, false);

                if (attribute is AutoRegisterAttribute autoRegisterAttribute)
                {
                    var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

                    EnumType enumValue = (EnumType)autoRegisterAttribute.GetLookupEnum();
                    if (constructor != null && constructor.Invoke(null) is T instance)
                    {
                        _instances.Add(instance);
                        _lookup[enumValue] = instance;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Registers classes that implement IClientRPC.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterRpc(RpcType rpcType) : AutoRegisterLookupAttribute<IRpc, RpcType>(rpcType) { }

/// <summary>
/// Registers classes that implement IPacketHandler.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterPacketHandler(PacketHandlerType packetHandlerType) : AutoRegisterLookupAttribute<IPacketHandler, PacketHandlerType>(packetHandlerType) { }

/// <summary>
/// Registers classes that implement IFastPacketResolver.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterFastPacketResolver : AutoRegisterAttribute<IFastPacketResolver> { }

/// <summary>
/// Registers classes that implement IArena.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterArena : AutoRegisterAttribute<IArena> { }

/// <summary>
/// Registers classes that implement IVersusGamemode.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterVersusGameMode : AutoRegisterAttribute<IVersusGamemode> { }

/// <summary>
/// Registers classes that implement ICharacterConfig.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterCharacterConfig : AutoRegisterAttribute<ICharacterConfig> { }