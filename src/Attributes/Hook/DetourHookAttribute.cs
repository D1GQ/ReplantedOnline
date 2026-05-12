using System.Reflection;

namespace ReplantedOnline.Attributes.Hook;

/// <summary>
/// Represents an attribute that marks a class or method as a detour hook for another method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class DetourHookAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetourHookAttribute"/> class with empty target information.
    /// </summary>
    internal DetourHookAttribute()
    {
        TargetType = null;
        MethodName = string.Empty;
        ParameterTypes = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DetourHookAttribute"/> class with the specified target method.
    /// </summary>
    /// <param name="type">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    internal DetourHookAttribute(Type type, string methodName)
    {
        TargetType = type;
        MethodName = methodName;
        ParameterTypes = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DetourHookAttribute"/> class with the specified target method and parameter types.
    /// </summary>
    /// <param name="type">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    /// <param name="parameterTypes">The parameter types of the specific overload to target.</param>
    internal DetourHookAttribute(Type type, string methodName, Type[] parameterTypes)
    {
        TargetType = type;
        MethodName = methodName;
        ParameterTypes = parameterTypes;
    }

    /// <summary>
    /// Binding flags used to locate the target method, including public, non-public, instance, and static methods.
    /// </summary>
    private static readonly BindingFlags BindingFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// Gets the type that contains the target method to be hooked.
    /// </summary>
    internal readonly Type TargetType;

    /// <summary>
    /// Gets the name of the target method to be hooked.
    /// </summary>
    internal readonly string MethodName;

    /// <summary>
    /// Gets the parameter types of the specific overload to target. If null, any overload with matching name will be used.
    /// </summary>
    internal readonly Type[] ParameterTypes;

    /// <summary>
    /// The hook instance used to intercept the target method.
    /// </summary>
    private MonoMod.RuntimeDetour.Hook _hook;

    /// <summary>
    /// Installs all method hooks defined in the current assembly.
    /// </summary>
    /// <exception cref="Exception">Thrown when a target method cannot be found for a hook.</exception>
    internal static void InstallAll()
    {
        var assembly = ModInfo.Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<DetourHookAttribute>() == null) continue;

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var attribute = method.GetCustomAttribute<DetourHookAttribute>();
                attribute?.Install(method);
            }
        }
    }

    /// <summary>
    /// Installs a hook for the specified target method.
    /// </summary>
    /// <param name="hookMethod">The method that will replace or intercept the target method.</param>
    /// <exception cref="Exception">Thrown when the target method cannot be found in the specified type.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="TargetType"/> or <see cref="MethodName"/> are null or empty.</exception>
    private void Install(MethodInfo hookMethod)
    {
        MethodInfo target;

        if (ParameterTypes != null && ParameterTypes.Length > 0)
        {
            // Target a specific overload by parameter types
            target = TargetType.GetMethod(MethodName, BindingFlag, null, ParameterTypes, null)
                ?? throw new Exception($"Failed to find target method: {TargetType.FullName}.{MethodName} with specified parameter types: {string.Join(", ", ParameterTypes.Select(t => t.Name))}");
        }
        else
        {
            // Fall back to original behavior - find any method with matching name
            target = TargetType.GetMethod(MethodName, BindingFlag)
                ?? throw new Exception($"Failed to find target method: {TargetType.FullName}.{MethodName}");
        }

        _hook = new MonoMod.RuntimeDetour.Hook(target, hookMethod);
    }
}