using MonoMod.RuntimeDetour;
using ReplantedOnline.Utilities.MelonLoader;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ReplantedOnline.Attributes.Hook;

/// <summary>
/// Non-generic base for all auto-hook attributes. It exists so that <see cref="InstallAll"/> can find and
/// install every concrete hook attribute without knowing the generic arguments of
/// <see cref="MethodAutoHookAttribute{T, InstanceT}"/>.
/// </summary>
internal abstract class MethodAutoHookAttribute : Attribute
{
    /// <summary>
    /// Binding flags that match every method declared directly on a type, regardless of visibility or staticness.
    /// </summary>
    protected readonly BindingFlags BindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Finds every concrete hook attribute type in the mod assembly and installs the hooks for each.
    /// </summary>
    internal static void InstallAll()
    {
        var types = ReplantedOnlineMod.ModInfo.Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MethodAutoHookAttribute)) && !t.IsAbstract && t.IsSealed).ToArray();

        foreach (var type in types)
        {
            var tempAttribute = (MethodAutoHookAttribute)FormatterServices.GetUninitializedObject(type);
            tempAttribute.Install();
        }
    }

    /// <summary>
    /// When overridden in a derived attribute, discovers all methods decorated with that attribute and installs their hooks.
    /// Called once per concrete attribute type by <see cref="InstallAll"/>.
    /// </summary>
    protected abstract void Install();
}

/// <summary>
/// An abstract attribute that provides method hooking functionality using MonoMod.RuntimeDetour.
/// Derived attributes can be applied to methods to intercept calls to those methods.
/// </summary>
/// <typeparam name="T">The concrete attribute type.</typeparam>
/// <typeparam name="InstanceT">The type of the instance passed to hooks. Must be a reference type.</typeparam>
internal abstract class MethodAutoHookAttribute<T, InstanceT> : MethodAutoHookAttribute where T : MethodAutoHookAttribute<T, InstanceT> where InstanceT : class
{
    /// <summary>
    /// Delegate that represents a call to the original (unhooked) method.
    /// </summary>
    /// <param name="instance">The instance the method was called on, or <see langword="null"/> for static methods.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>The return value of the original method, or <see langword="null"/> for void methods.</returns>
    internal delegate object? OriginalDelegate(InstanceT? instance, object[] args);

    /// <summary>
    /// Cached reflection handle for <see cref="Dispatch"/>, which every generated stub calls.
    /// </summary>
    private static readonly MethodInfo DispatchMethod =
        typeof(MethodAutoHookAttribute<T, InstanceT>).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// All installed hooks. The index of a hook in this list is the id baked into its stub,
    /// which lets <see cref="Dispatch"/> find its state with a plain array lookup.
    /// </summary>
    protected static readonly List<HookInfo> Hooks = [];

    /// <summary>
    /// Scans the mod assembly for all methods decorated with the concrete attribute <typeparamref name="T"/>,
    /// then installs a hook for each one that passes <see cref="ShouldInstallHook"/>.
    /// </summary>
    protected override void Install()
    {
        var entries = new List<HookEntry>();
        var types = ReplantedOnlineMod.ModInfo.Assembly.GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<T>();
                if (attr is not null)
                    entries.Add(new HookEntry(type, method, attr));
            }
        }

        foreach (var entry in entries)
        {
            if (!ShouldInstallHook(entry))
                continue;

            InstallHook(entry);
        }
    }

    /// <summary>
    /// Called after a hook has been successfully installed for the method described by <paramref name="hookEntry"/>.
    /// Override in a derived attribute to perform additional setup once the hook is active.
    /// </summary>
    /// <param name="hookEntry">The entry describing the method that was just hooked.</param>
    protected virtual void OnHooked(HookEntry hookEntry) { }

    /// <summary>
    /// Decides whether a discovered method should actually be hooked.
    /// </summary>
    /// <param name="hookEntry">The entry describing the method that is about to be hooked.</param>
    /// <returns><see langword="true"/> to install the hook; <see langword="false"/> to skip it. The default is <see langword="true"/>.</returns>
    protected virtual bool ShouldInstallHook(HookEntry hookEntry)
    {
        return true;
    }

    /// <summary>
    /// Applies a hook to the method described by the given <see cref="HookEntry"/>.
    /// Unsupported methods are logged and skipped; methods that are already hooked are ignored.
    /// </summary>
    /// <param name="entry">The entry describing the method to hook.</param>
    internal static void InstallHook(HookEntry entry)
    {
        var target = entry.MethodInfo;

        if (GetUnsupportedReason(target) is { } reason)
        {
            ReplantedOnlineMod.Logger.Warning(typeof(T),
                $"{typeof(T).Name} Failed to hook {target.DeclaringType!.Name}.{target.Name}: {reason}");
            return;
        }

        if (Hooks.Exists(h => h.Entry.MethodInfo == target))
            return;

        var stub = BuildStub(target, Hooks.Count);
        var detour = new Detour(target, stub.Method);

        try
        {
            var trampoline = (DynamicMethod)detour.GenerateTrampoline();
            Hooks.Add(new HookInfo(entry, stub, detour, BuildOriginalAdapter(trampoline, target)));
            entry.Attribute.OnHooked(entry);
        }
        catch
        {
            detour.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Retrieves the delegate that invokes the original method for the given entry.
    /// </summary>
    /// <param name="entry">The entry whose original delegate is requested.</param>
    /// <returns>The original delegate, or <see langword="null"/> if the method is not hooked.</returns>
    internal static OriginalDelegate? GetOriginal(HookEntry entry) => Hooks.Find(h => h.Entry.MethodInfo == entry.MethodInfo)?.Original;

    /// <summary>
    /// Checks whether a method can be hooked by this system.
    /// </summary>
    /// <param name="method">The method to validate.</param>
    /// <returns>A message describing why the method is unsupported, or <see langword="null"/> if it can be hooked.</returns>
    private static string? GetUnsupportedReason(MethodInfo method)
    {
        if (method.IsGenericMethod)
            return "Method cannot be generic!";

        foreach (var p in method.GetParameters())
        {
            if (p.ParameterType.IsPointer)
                return $"Parameter '{p.Name}' is a pointer, which is unsupported!";

            if (p.ParameterType.IsByRef)
                return $"Parameter '{p.Name}' uses the unsupported '{(p.IsOut ? "out" : p.IsIn ? "in" : "ref")}' modifier!";
        }

        return null;
    }

    /// <summary>
    /// Emits IL that converts the <see cref="object"/> on top of the evaluation stack into <paramref name="type"/>.
    /// Value types are unboxed; reference types are cast.
    /// </summary>
    /// <param name="il">The IL generator to emit into.</param>
    /// <param name="type">The type to convert to.</param>
    private static void EmitCast(ILGenerator il, Type type) => il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);

    /// <summary>
    /// Builds a dynamic stub method that packages arguments into an <see cref="object"/> array
    /// and forwards the call to <see cref="Dispatch"/>.
    /// </summary>
    /// <param name="target">The method being hooked.</param>
    /// <param name="id">The index of this hook in <see cref="Hooks"/>, passed to <see cref="Dispatch"/> on every call.</param>
    /// <returns>A delegate matching the signature of the target method.</returns>
    private static Delegate BuildStub(MethodInfo target, int id)
    {
        // The stub's signature mirrors the target. Instance methods take 'this' as an explicit first parameter.
        Type[] paramTypes = [.. target.GetParameters().Select(p => p.ParameterType)];
        Type[] signature = target.IsStatic ? paramTypes : [target.DeclaringType!, .. paramTypes];

        // Instance methods shift every real parameter up by one because arg 0 is 'this'.
        var offset = signature.Length - paramTypes.Length;

        var method = new DynamicMethod($"Hook_{target.DeclaringType?.Name}_{target.Name}", target.ReturnType, signature);
        var il = method.GetILGenerator();

        // Local variable 0: object[] args
        var args = il.DeclareLocal(typeof(object[]));

        // args = new object[paramTypes.Length];
        il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Stloc, args);

        // args[i] = (object)argument_i;
        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldloc, args);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, (short)(i + offset));

            // Value types must be boxed to be stored as object. Reference types are already objects.
            if (paramTypes[i].IsValueType)
                il.Emit(OpCodes.Box, paramTypes[i]);

            il.Emit(OpCodes.Stelem_Ref);
        }

        // Push the three arguments for Dispatch(int id, InstanceT? instance, object[] args).

        // Argument 1: the hook id, so Dispatch can find this hook's state.
        il.Emit(OpCodes.Ldc_I4, id);

        // Argument 2: the instance, or null for static methods.
        if (target.IsStatic)
            il.Emit(OpCodes.Ldnull);
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, typeof(InstanceT));
        }

        // Argument 3: the boxed argument array.
        il.Emit(OpCodes.Ldloc, args);

        // object result = Dispatch(id, instance, args);
        il.Emit(OpCodes.Call, DispatchMethod);

        // Convert Dispatch's object result into whatever the hooked method is supposed to return.
        if (target.ReturnType == typeof(void))
            il.Emit(OpCodes.Pop);
        else
            EmitCast(il, target.ReturnType);

        il.Emit(OpCodes.Ret);

        // Expression.GetDelegateType picks Func<>/Action<> when possible and generates a custom delegate type otherwise.
        return method.CreateDelegate(Expression.GetDelegateType([.. signature, target.ReturnType]));
    }

    /// <summary>
    /// Dispatches a hooked method call to the appropriate attribute's <see cref="Hook"/> implementation.
    /// Called by every generated stub.
    /// </summary>
    /// <param name="id">The index of the hook in <see cref="Hooks"/>.</param>
    /// <param name="instance">The instance the method was called on, or <see langword="null"/> for static methods.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>The value returned by the hook implementation.</returns>
    internal static object? Dispatch(int id, InstanceT? instance, object[] args)
    {
        var info = Hooks[id];
        return info.Entry.Attribute.Hook(info.Entry, instance, args, info.Original);
    }

    /// <summary>
    /// Builds a delegate that adapts the MonoMod trampoline into an <see cref="OriginalDelegate"/> that accepts an instance and an object array.
    /// </summary>
    /// <param name="trampoline">The dynamic method trampoline generated by MonoMod.</param>
    /// <param name="target">The original method being hooked.</param>
    /// <returns>An <see cref="OriginalDelegate"/> that invokes the original method.</returns>
    private static OriginalDelegate BuildOriginalAdapter(DynamicMethod trampoline, MethodInfo target)
    {
        // Signature matches OriginalDelegate: arg 0 = instance, arg 1 = object[] args.
        var method = new DynamicMethod(
            $"Original_{target.DeclaringType?.Name}_{target.Name}",
            typeof(object), [typeof(InstanceT), typeof(object[])]);
        var il = method.GetILGenerator();

        // The trampoline has the same signature as the target, so for instance methods
        // 'this' goes first, cast from InstanceT to the method's real declaring type.
        if (!target.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0);                 // stack: [instance]
            il.Emit(OpCodes.Castclass, target.DeclaringType!); // stack: [(TDeclaring)instance]
        }

        // Unpack each argument from the object[]: (Pi)args[i]
        var parameters = target.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_1);                 // stack: [..., args]
            il.Emit(OpCodes.Ldc_I4, i);               // stack: [..., args, i]
            il.Emit(OpCodes.Ldelem_Ref);              // stack: [..., args[i]]  (as object)
            EmitCast(il, parameters[i].ParameterType); // stack: [..., (Pi)args[i]]  (unbox or castclass)
        }

        // Call the original method body through the trampoline.
        // Stack now holds [this?, p0, p1, ...] and is consumed by the call.
        il.Emit(OpCodes.Call, trampoline);            // stack: [result] (or [] if void)

        // Convert the result back to object, since OriginalDelegate returns object?.
        if (target.ReturnType == typeof(void))
            il.Emit(OpCodes.Ldnull);                  // void: return null; stack: [null]
        else if (target.ReturnType.IsValueType)
            il.Emit(OpCodes.Box, target.ReturnType);  // value type: box it; stack: [(object)result]
        // Reference types are already objects, so nothing to emit.

        il.Emit(OpCodes.Ret);                         // return the object on top of the stack
        return method.CreateDelegate<OriginalDelegate>();
    }

    /// <summary>
    /// When overridden in a derived attribute, implements the hook logic for the decorated method.
    /// </summary>
    /// <param name="hookEntry">The entry describing the hooked method.</param>
    /// <param name="instance">The instance the method was called on, or <see langword="null"/> for static methods.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <param name="original">A delegate that invokes the original (unhooked) method.</param>
    /// <returns>The value to return from the hooked method.</returns>
    protected abstract object? Hook(HookEntry hookEntry, InstanceT? instance, object[] args, OriginalDelegate original);

    /// <summary>
    /// Holds the runtime state associated with a single hooked method.
    /// </summary>
    /// <param name="Entry">The entry describing the hooked method.</param>
    /// <param name="Stub">The dynamic stub delegate that forwards to <see cref="Dispatch"/>. Kept alive so it is not garbage collected.</param>
    /// <param name="Detour">The MonoMod detour applied to the target method. Kept alive so the hook stays installed.</param>
    /// <param name="Original">The delegate that invokes the original method.</param>
    protected sealed record HookInfo(HookEntry Entry, Delegate Stub, IDetour Detour, OriginalDelegate Original);

    /// <summary>
    /// Describes a method decorated with a concrete <see cref="MethodAutoHookAttribute{T, InstanceT}"/>.
    /// </summary>
    /// <param name="Type">The declaring type of the method.</param>
    /// <param name="MethodInfo">The method to hook.</param>
    /// <param name="Attribute">The concrete attribute instance applied to the method.</param>
    internal sealed record HookEntry(Type Type, MethodInfo MethodInfo, T Attribute)
    {
        /// <summary>
        /// Gets the delegate that invokes the original (unhooked) method, if the method has been hooked.
        /// </summary>
        internal OriginalDelegate? Original => GetOriginal(this);
    }
}