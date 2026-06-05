using ReplantedOnline.Network.Client.Object.Component;

namespace ReplantedOnline.Attributes.Register;

/// <summary>
/// Registers factory types that can create instances of <typeparamref name="T"/> based on an identifier key.
/// Unlike lookup attributes which store singleton instances, this attribute stores type references
/// and creates new instances on demand via the <see cref="TryCreateInstance"/> method.
/// </summary>
/// <typeparam name="T">The base type or interface that the created instances must implement.</typeparam>
/// <typeparam name="Id">The type used as a key for factory lookup.</typeparam>
/// <param name="identifier">The specific identifier value to associate with this factory registration.</param>
[AttributeUsage(AttributeTargets.Class)]
internal abstract class AutoRegisterFactoryAttribute<T, Id>(Id identifier) : AutoRegisterAttribute where T : class where Id : notnull
{
    private readonly Id _identifier = identifier;

    private static readonly Dictionary<Id, Type> TypeLookup = [];

    /// <summary>
    /// Gets the unique identifier associated with this factory attribute instance.
    /// </summary>
    /// <returns>
    /// The identifier value that serves as the key 
    /// for factory lookup when creating instances of <typeparamref name="T"/>.
    /// </returns>
    internal override object GetIdentifier()
    {
        return _identifier;
    }

    /// <summary>
    /// Tries to create a new instance of the type registered for the specified identifier.
    /// </summary>
    /// <param name="identifier">The identifier value identifying which type to instantiate.</param>
    /// <param name="fallback">An optional fallback type to instantiate if no type is registered for the identifier. Must be assignable to <typeparamref name="T"/>.</param>
    /// <returns>
    /// A new instance of type <typeparamref name="T"/> if successful; 
    /// otherwise, null if no type is registered for the identifier, the registered type is not assignable to <typeparamref name="T"/>, 
    /// the fallback type is null or not assignable, or instantiation fails.
    /// </returns>
    internal static T? TryCreateInstance(Id identifier, Type? fallback = null)
    {
        if (TypeLookup.TryGetValue(identifier, out var type))
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type) as T;
            }
        }

        if (fallback != null)
        {
            if (typeof(T).IsAssignableFrom(fallback))
            {
                return Activator.CreateInstance(fallback) as T;
            }
        }

        return null;
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
                        Id identifierValue = (Id)autoRegisterAttribute.GetIdentifier()!;
                        TypeLookup[identifierValue] = type;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Registers NetworkComponent classe types for factory.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class RegisterNetworkComponent(object @enum) : AutoRegisterFactoryAttribute<NetworkComponent, Enum>((Enum)@enum) { }