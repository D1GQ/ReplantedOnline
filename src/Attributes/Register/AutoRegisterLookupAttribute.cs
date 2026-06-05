using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Interfaces.Versus;
using System.Reflection;

namespace ReplantedOnline.Attributes.Register;

/// <summary>
/// Provides a base attribute class that extends <see cref="AutoRegisterAttribute{T}"/> with identifier-based lookup capabilities.
/// </summary>
/// <typeparam name="T">The base type or interface that attributed classes must implement.</typeparam>
/// <typeparam name="Id">The type used as a key for instance lookup.</typeparam>
/// <param name="identifier">The specific identifier value to associate with this attribute.</param>
[AttributeUsage(AttributeTargets.Class)]
internal abstract class AutoRegisterLookupAttribute<T, Id>(Id identifier) : AutoRegisterAttribute<T> where T : class where Id : notnull
{
    /// <summary>
    /// The identifier value associated with instances registered by this attribute.
    /// </summary>
    private readonly Id _identifier = identifier;

    /// <summary>
    /// Static dictionary that maps identifier values to their corresponding instances for fast lookup.
    /// </summary>
    private static readonly Dictionary<Id, T> InstanceLookup = [];

    /// <summary>
    /// Gets the identifier associated with this attribute instance for lookup purposes.
    /// </summary>
    /// <returns>The value that serves as the unique identifier for this registered instance.</returns>
    internal override object GetIdentifier()
    {
        return _identifier;
    }

    /// <summary>
    /// Retrieves an instance from the lookup dictionary by its associated identifier.
    /// </summary>
    /// <param name="identifier">The identifier value to look up.</param>
    /// <returns>The instance associated with the specified identifier, or null if not found.</returns>
    internal static T? GetInstanceFromLookup(Id identifier)
    {
        if (InstanceLookup.TryGetValue(identifier, out var instanceLookup))
        {
            return instanceLookup;
        }

        return null;
    }

    /// <summary>
    /// Attempts to retrieve an instance from the lookup dictionary by its associated identifier.
    /// </summary>
    /// <param name="identifier">The identifier value to look up.</param>
    /// <param name="instance">When this method returns, contains the instance associated with the specified identifier, or <see langword="null"/> if the identifier is not found.</param>
    /// <returns>
    /// <see langword="true"/> if the lookup dictionary contains an instance with the specified identifier; otherwise, <see langword="false"/>.
    /// </returns>
    internal static bool TryGetInstanceFromLookup(Id identifier, out T instance)
    {
        if (InstanceLookup.TryGetValue(identifier, out var instanceLookup))
        {
            instance = instanceLookup;
            return true;
        }

        instance = null!;
        return false;
    }

    /// <inheritdoc/>
    protected override void RegisterInstances()
    {
        var attributeType = GetType();

        var attributedTypes = ModInfo.Assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface).ToArray();

        foreach (var type in attributedTypes)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                var attributes = type.GetCustomAttributes(attributeType, false);

                foreach (var attribute in attributes)
                {
                    if (attribute is AutoRegisterAttribute autoRegisterAttribute)
                    {
                        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

                        Id identifierValue = (Id)autoRegisterAttribute.GetIdentifier()!;
                        if (constructor != null && constructor.Invoke(null) is T instance)
                        {
                            _instances.Add(instance);
                            InstanceLookup[identifierValue] = instance;
                        }
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
/// Registers classes that implement IPlantConfig.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterPlantConfig(SeedType seedType) : AutoRegisterLookupAttribute<IPlantConfig, SeedType>(seedType) { }

/// <summary>
/// Registers classes that implement IZombieConfig.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterZombieConfig(ZombieType zombieType) : AutoRegisterLookupAttribute<IZombieConfig, ZombieType>(zombieType) { }