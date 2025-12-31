using ReplantedOnline.Network.ClientRPC;
using System.Reflection;

namespace ReplantedOnline.Attributes;

/// <summary>
/// Base attribute class for automatically discovering and registering instances of specific types through reflection.
/// Provides a centralized registration system for mod components that need global access.
/// </summary>
internal abstract class InstanceAttribute : Attribute
{
    /// <summary>
    /// Scans the entire assembly and registers all instances of classes marked with InstanceAttribute subclasses.
    /// This method should be called during mod initialization to set up the automatic instance registration system.
    /// </summary>
    internal static void RegisterAll()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var types = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(InstanceAttribute)) && !t.IsAbstract && t.IsSealed)
            .ToArray();

        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is InstanceAttribute attribute)
            {
                attribute.RegisterInstances();
            }
        }
    }

    /// <summary>
    /// When implemented in a derived class, registers instances of a specific type discovered through reflection.
    /// </summary>
    protected abstract void RegisterInstances();
}

/// <summary>
/// Generic attribute for automatically registering static instances of a specified base type.
/// Provides a type-safe way to collect and access all implementations of a particular interface or base class.
/// </summary>
/// <typeparam name="T">The base type or interface that attributed classes must implement.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
internal abstract class StaticInstanceAttribute<T> : InstanceAttribute where T : class
{
    private static readonly List<T> _instances = [];

    /// <summary>
    /// Gets a read-only collection of all registered instances of type T.
    /// </summary>
    internal static IReadOnlyList<T> Instances => _instances.AsReadOnly();

    /// <summary>
    /// Retrieves a specific instance by its concrete type.
    /// </summary>
    /// <typeparam name="J">The concrete type of the instance to retrieve.</typeparam>
    /// <returns>The instance of type J if found, otherwise null.</returns>
    /// <example>
    /// <code>
    /// var specificHandler = StaticInstanceAttribute&lt;RPCHandler&gt;.GetClassInstance&lt;MySpecificHandler&gt;();
    /// </code>
    /// </example>
    internal static J GetClassInstance<J>() where J : class => _instances.FirstOrDefault(instance => instance.GetType() == typeof(J)) as J;

    /// <summary>
    /// Scans the assembly for classes marked with this attribute type and registers instances of them.
    /// Creates instances using the parameterless constructor and adds them to the static instances collection.
    /// </summary>
    protected override void RegisterInstances()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var attributedTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(GetType(), false).Any());

        foreach (var type in attributedTypes)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (constructor != null && constructor.Invoke(null) is T instance)
                {
                    _instances.Add(instance);
                }
            }
        }
    }
}

/// <inheritdoc/>
internal sealed class RegisterRPCHandler : StaticInstanceAttribute<BaseClientRPCHandler>
{
}