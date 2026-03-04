using Il2CppInterop.Runtime.Injection;
using System.Collections;

namespace ReplantedOnline.Modules;

/// <summary>
/// A wrapper that converts a managed IEnumerator to an Il2Cpp IEnumerator for interop compatibility.
/// Handles type conversion and exception logging for coroutines.
/// </summary>
internal sealed class Il2cppEnumeratorWrapper : Il2CppSystem.Object
{
    /// <summary>
    /// Registers this wrapper type with Il2Cpp for runtime injection.
    /// Must be called before any instances are created.
    /// </summary>
    internal static void Register()
    {
        ClassInjector.RegisterTypeInIl2Cpp<Il2cppEnumeratorWrapper>(new()
        {
            LogSuccess = true,
            Interfaces = new Type[] { typeof(Il2CppSystem.Collections.IEnumerator) }
        });
    }

    private readonly IEnumerator enumerator;

    /// <summary>
    /// Il2Cpp constructor required for runtime injection.
    /// </summary>
    /// <param name="ptr">Pointer to the Il2Cpp object.</param>
    public Il2cppEnumeratorWrapper(IntPtr ptr) : base(ptr) { }

    /// <summary>
    /// Managed constructor that wraps a managed IEnumerator for Il2Cpp consumption.
    /// </summary>
    /// <param name="_enumerator">The managed enumerator to wrap.</param>
    /// <exception cref="NullReferenceException">Thrown when the enumerator is null.</exception>
    public Il2cppEnumeratorWrapper(IEnumerator _enumerator) : base(ClassInjector.DerivedConstructorPointer<Il2cppEnumeratorWrapper>())
    {
        ClassInjector.DerivedConstructorBody(this);
        enumerator = _enumerator ?? throw new NullReferenceException("routine is null");
    }

    /// <summary>
    /// Gets the current element in the collection.
    /// Converts managed objects to Il2Cpp objects as needed, with special handling for nested enumerators.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when encountering an unsupported type.</exception>
    public Il2CppSystem.Object Current
    {
        get => enumerator.Current switch
        {
            IEnumerator next => new Il2cppEnumeratorWrapper(next),
            Il2CppSystem.Object il2cppObject => il2cppObject,
            null => null,
            _ => throw new NotSupportedException($"{enumerator.GetType()}: Unsupported type {enumerator.Current.GetType()}"),
        };
    }

    /// <summary>
    /// Advances the enumerator to the next element of the collection.
    /// Logs any exceptions to the appropriate MelonLoader logger.
    /// </summary>
    /// <returns>
    /// true if the enumerator was successfully advanced to the next element; 
    /// false if the enumerator has passed the end of the collection.
    /// </returns>
    public bool MoveNext()
    {
        try
        {
            return enumerator.MoveNext();
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error("Unhandled exception in coroutine. It will not continue executing.", ex);
            return false;
        }
    }

    /// <summary>
    /// Sets the enumerator to its initial position, which is before the first element in the collection.
    /// </summary>
    public void Reset() => enumerator.Reset();
}