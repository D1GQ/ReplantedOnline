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
        GenericArguments = null;
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
        GenericArguments = null;
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
        GenericArguments = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DetourHookAttribute"/> class for a generic method.
    /// </summary>
    /// <param name="type">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    /// <param name="parameterTypes">The parameter types of the specific overload to target.</param>
    /// <param name="genericArguments">The generic type arguments to instantiate the generic method.</param>
    internal DetourHookAttribute(Type type, string methodName, Type[] parameterTypes, Type[] genericArguments)
    {
        TargetType = type;
        MethodName = methodName;
        GenericArguments = genericArguments;
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
    /// Gets the generic type arguments for the target method if it's generic.
    /// </summary>
    internal readonly Type[] GenericArguments;

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
        MethodInfo target = GetTargetMethod();

        // Validate return type compatibility for generic methods
        ValidateReturnType(target, hookMethod);

        _hook = new MonoMod.RuntimeDetour.Hook(target, hookMethod);
    }

    /// <summary>
    /// Gets the target method with proper handling for generic methods.
    /// </summary>
    /// <returns>The target method info.</returns>
    private MethodInfo GetTargetMethod()
    {
        var methods = TargetType.GetMethods(BindingFlag)
            .Where(m => m.Name == MethodName)
            .ToList();

        if (methods.Count == 0)
            throw new Exception($"Failed to find target method: {TargetType.FullName}.{MethodName}");

        if (ParameterTypes != null && ParameterTypes.Length > 0)
        {
            methods = [.. methods.Where(m =>
            {
                var parameters = m.GetParameters();
                if (parameters.Length != ParameterTypes.Length)
                    return false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    // Check if parameter types match
                    if (parameters[i].ParameterType != ParameterTypes[i])
                        return false;
                }
                return true;
            })];
        }

        if (methods.Count == 0)
        {
            string paramInfo = ParameterTypes != null ?
                $" with parameter types: {string.Join(", ", ParameterTypes.Select(t => t.Name))}" : "";
            throw new Exception($"Failed to find target method: {TargetType.FullName}.{MethodName}{paramInfo}");
        }

        if (GenericArguments != null && GenericArguments.Length > 0)
        {
            var genericMethod = methods.FirstOrDefault(m => m.IsGenericMethodDefinition);

            return genericMethod == null
                ? throw new Exception($"Method {TargetType.FullName}.{MethodName} is not generic")
                : genericMethod.MakeGenericMethod(GenericArguments);
        }
        else if (methods.Count > 1)
        {
            throw new Exception($"Multiple overloads found for {TargetType.FullName}.{MethodName}. Please specify parameter types or generic arguments.");
        }

        return methods.First();
    }

    /// <summary>
    /// Validates that the hook method's return type is compatible with the target method.
    /// </summary>
    /// <param name="target">The target method.</param>
    /// <param name="hook">The hook method.</param>
    /// <exception cref="InvalidOperationException">Thrown when return types don't match.</exception>
    private static void ValidateReturnType(MethodInfo target, MethodInfo hook)
    {
        if (target.ReturnType != hook.ReturnType)
        {
            throw new InvalidOperationException(
                $"Return type mismatch for hook of {target.DeclaringType?.Name}.{target.Name}(): " +
                $"Expected {target.ReturnType}, got {hook.ReturnType}"
            );
        }
    }
}