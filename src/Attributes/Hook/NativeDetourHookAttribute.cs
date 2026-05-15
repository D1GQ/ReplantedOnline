using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader.NativeUtils;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReplantedOnline.Attributes.Hook;

/// <summary>
/// Abstract base attribute for creating native detour hooks that intercept IL2CPP method calls.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal class NativeDetourHook : Attribute
{
    /// <summary>
    /// Installs all native detour hooks defined in the current assembly.
    /// </summary>
    internal static void InstallAll()
    {
        var assembly = ModInfo.Assembly;

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<NativeDetourHook>() == null) continue;

                var attributes = method.GetCustomAttributes();
                foreach (var attribute in attributes)
                {
                    if (attribute is NativeDetourHook hook)
                    {
                        hook.Install(method);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Installs the native detour hook for the specified method.
    /// </summary>
    /// <param name="hookMethod">The method that will replace or intercept the target method.</param>
    protected virtual unsafe void Install(MethodInfo hookMethod) { }
}

/// <summary>
/// Attribute for creating a native detour hook with a specific delegate type.
/// </summary>
/// <typeparam name="TDelegate">The delegate type that matches the signature of the target method.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class NativeDetourHook<TDelegate> : NativeDetourHook where TDelegate : Delegate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NativeDetourHook{TDelegate}"/> class with the specified target method.
    /// </summary>
    /// <param name="targetType">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    internal NativeDetourHook(Type targetType, string methodName)
    {
        TargetType = targetType;
        MethodName = methodName;
        ParameterTypes = null;
        GenericArguments = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeDetourHook{TDelegate}"/> class with parameter type specification.
    /// </summary>
    /// <param name="targetType">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    /// <param name="parameterTypes">The parameter types of the specific overload to target.</param>
    internal NativeDetourHook(Type targetType, string methodName, Type[] parameterTypes)
    {
        TargetType = targetType;
        MethodName = methodName;
        ParameterTypes = parameterTypes;
        GenericArguments = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeDetourHook{TDelegate}"/> class for a generic method.
    /// </summary>
    /// <param name="targetType">The type that contains the target method to be hooked.</param>
    /// <param name="methodName">The name of the target method to be hooked.</param>
    /// <param name="parameterTypes">The parameter types of the specific overload to target.</param>
    /// <param name="genericArguments">The generic type arguments to instantiate the generic method.</param>
    internal NativeDetourHook(Type targetType, string methodName, Type[] parameterTypes, Type[] genericArguments)
    {
        TargetType = targetType;
        MethodName = methodName;
        ParameterTypes = parameterTypes;
        GenericArguments = genericArguments;
    }

    /// <summary>
    /// Binding flags used to locate the target method, including public, non-public, instance, and static methods.
    /// </summary>
    private static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    /// <summary>
    /// Caches delegates created from hook methods to prevent garbage collection.
    /// </summary>
    private static readonly List<Delegate> DelegateCache = [];

    /// <summary>
    /// Gets the original method trampoline that can be called to invoke the original implementation.
    /// </summary>
    internal static TDelegate Orig { get; private set; }

    /// <summary>
    /// Gets the type that contains the target method to be hooked.
    /// </summary>
    internal readonly Type TargetType;

    /// <summary>
    /// Gets the name of the target method to be hooked.
    /// </summary>
    internal readonly string MethodName;

    /// <summary>
    /// Gets the parameter types of the specific overload to target. If null, the first matching overload is used.
    /// </summary>
    internal readonly Type[] ParameterTypes;

    /// <summary>
    /// Gets the generic type arguments for the target method if it's generic.
    /// </summary>
    internal readonly Type[] GenericArguments;

    /// <summary>
    /// Installs the native detour hook by creating an IL2CPP method detour.
    /// </summary>
    /// <param name="hookMethod">The managed method that will intercept the target native method.</param>
    /// <exception cref="Exception">Thrown when the target method cannot be found or when generic arguments are missing for a generic method.</exception>
    protected override unsafe void Install(MethodInfo hookMethod)
    {
        var targetMethod = GetTargetMethod();

        // Handle generic method instantiation
        if (targetMethod.IsGenericMethodDefinition)
        {
            if (GenericArguments == null || GenericArguments.Length == 0)
                throw new Exception($"Generic arguments are required for generic method: {MethodName}");

            targetMethod = targetMethod.MakeGenericMethod(GenericArguments);
        }

        // Get the native method pointer from IL2CPP
        var methodInfoPtr = (Il2CppMethodInfo*)(IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(targetMethod)
                .GetValue(null)!;

        var wrappedMethod = UnityVersionHandler.Wrap(methodInfoPtr);
        IntPtr targetPtr = wrappedMethod.MethodPointer;

        // Create delegate from the hook method and cache it
        var detourDelegate = Delegate.CreateDelegate(typeof(TDelegate), hookMethod);
        DelegateCache.Add(detourDelegate);
        var detourPtr = Marshal.GetFunctionPointerForDelegate(detourDelegate);

        // Create and attach the native hook
        var nativeHookType = typeof(NativeHook<>).MakeGenericType(typeof(TDelegate));
        object nativeHook = Activator.CreateInstance(nativeHookType, targetPtr, detourPtr)!;

        var attachMethod = nativeHookType.GetMethod("Attach", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new Exception("Failed to find NativeHook.Attach() method");

        attachMethod.Invoke(nativeHook, null);

        // Store the original trampoline for external use
        var trampolineProperty = nativeHookType.GetProperty(nameof(NativeHook<>.Trampoline))
            ?? throw new Exception("Failed to find NativeHook.Trampoline property");

        var trampoline = trampolineProperty.GetValue(nativeHook);
        Orig = (TDelegate)trampoline!;
    }

    /// <summary>
    /// Retrieves the target method information based on the configured parameters.
    /// </summary>
    /// <returns>The <see cref="MethodInfo"/> for the target method.</returns>
    /// <exception cref="Exception">Thrown when the target method cannot be found.</exception>
    private MethodInfo GetTargetMethod()
    {
        var methods = TargetType.GetMethods(BindingFlags)
            .Where(m => m.Name == MethodName)
            .ToList();

        if (ParameterTypes != null && ParameterTypes.Length > 0)
        {
            methods = [.. methods.Where(m =>
            {
                var parameters = m.GetParameters();
                if (parameters.Length != ParameterTypes.Length)
                    return false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != ParameterTypes[i])
                        return false;
                }

                return true;
            })];
        }

        if (methods.Count == 0)
        {
            var paramInfo = ParameterTypes != null
                ? $" with parameter types: {string.Join(", ", ParameterTypes.Select(t => t.Name))}"
                : string.Empty;

            throw new Exception($"Failed to find native target method: {TargetType.FullName}.{MethodName}{paramInfo}");
        }

        return methods.First();
    }
}