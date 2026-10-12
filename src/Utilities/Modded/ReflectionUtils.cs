using System.Reflection;
using System.Reflection.Emit;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides utilities for working with Reflection,
/// </summary>
internal static class ReflectionUtils
{
    /// <summary>
    /// Dispatch delegate. Returns the boxed return value, or null for void methods.
    /// </summary>
    internal delegate object MethodDispatcher(object target, object[] args);

    /// <summary>
    /// Builds an IL-emitted dispatcher for the given instance method.
    /// </summary>
    internal static MethodDispatcher CreateMethodDispatcher(MethodInfo methodInfo)
    {
        // We need a concrete declaring type to emit a call against.
        var declaringType = methodInfo.DeclaringType
            ?? throw new InvalidOperationException(
                $"Handler '{methodInfo.Name}' has no declaring type.");

        // Generic method definitions can't be invoked directly
        if (methodInfo.IsGenericMethodDefinition)
            throw new InvalidOperationException(
                $"Handler '{methodInfo.Name}' is a generic method definition, which is not supported.");

        // by-ref / out parameters can't be represented through object[] marshaling
        var parameters = methodInfo.GetParameters();
        foreach (var p in parameters)
        {
            if (p.ParameterType.IsByRef || p.IsOut)
                throw new InvalidOperationException(
                    $"Handler '{methodInfo.Name}' has a by-ref or out parameter '{p.Name}', which is not supported.");
        }

        // Build a DynamicMethod with the same shape as MethodDispatcher
        var dm = new DynamicMethod(
            name: $"__dispatch_{declaringType.Name}_{methodInfo.Name}_{methodInfo.MetadataToken}",
            returnType: typeof(object),                 // <-- was typeof(void)
            parameterTypes: [typeof(object), typeof(object[])],
            m: declaringType.Module,
            skipVisibility: true);

        var il = dm.GetILGenerator();

        // Push the target object onto the stack...
        il.Emit(OpCodes.Ldarg_0);
        // then turn it into the actual declaring type
        il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

        // Load and convert each argument (arg 1 is the object[])
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // push args[]
            il.Emit(OpCodes.Ldarg_1);
            // push index i
            il.Emit(OpCodes.Ldc_I4, i);
            // args[i]
            il.Emit(OpCodes.Ldelem_Ref);

            if (paramType.IsValueType) // unbox to value type
                il.Emit(OpCodes.Unbox_Any, paramType);
            else if (paramType != typeof(object)) // cast reference types
                il.Emit(OpCodes.Castclass, paramType);

            // If paramType is already object, no conversion needed.
        }

        // Callvirt for virtual/interface dispatch
        il.Emit(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);

        // The DynamicMethod's declared return type is object, so anything the
        // underlying method returns must be converted to object (or to null).
        var returnType = methodInfo.ReturnType;
        if (returnType == typeof(void))
        {
            // void methods leave nothing on the stack; push null so we still
            // return an object as the delegate signature requires.
            il.Emit(OpCodes.Ldnull);
        }
        else if (returnType.IsValueType)
        {
            // Value types on the stack must be boxed to become an object reference.
            il.Emit(OpCodes.Box, returnType);
        }

        il.Emit(OpCodes.Ret);

        return (MethodDispatcher)dm.CreateDelegate(typeof(MethodDispatcher));
    }
}