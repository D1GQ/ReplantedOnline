namespace ReplantedOnline.Helper;

/// <summary>
/// Provides extension methods for Il2Cpp types to improve interoperability with C# and simplify common operations.
/// </summary>
internal static class Il2CppExtensions
{
    /// <summary>
    /// Enumerates an Il2Cpp IReadOnlyList, providing a safe way to iterate with a maximum attempt limit.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The Il2Cpp IReadOnlyList to enumerate.</param>
    /// <param name="maxAttempts">The maximum number of enumeration attempts before stopping.</param>
    /// <returns>An enumerable sequence of elements from the list.</returns>
    internal static IEnumerable<T> EnumerateIl2CppReadonlyList<T>(this Il2CppSystem.Collections.Generic.IReadOnlyList<T> list, int maxAttempts = 10000)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            bool success;
            T item;
            try
            {
                item = list[i];
                success = true;
            }
            catch (Exception)
            {
                yield break;
            }

            if (success)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Converts a managed List to an Il2Cpp List.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="managedList">The managed List to convert.</param>
    /// <returns>An Il2Cpp List containing the same elements.</returns>
    internal static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this List<T> managedList)
    {
        var il2cppList = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var item in managedList)
            il2cppList.Add(item);
        return il2cppList;
    }

    /// <summary>
    /// Converts an Il2Cpp List to a managed List.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="il2cppList">The Il2Cpp List to convert.</param>
    /// <returns>A managed List containing the same elements.</returns>
    internal static List<T> ToManagedList<T>(this Il2CppSystem.Collections.Generic.List<T> il2cppList)
    {
        var managedList = new List<T>();
        for (int i = 0; i < il2cppList.Count; i++)
            managedList.Add(il2cppList[i]);
        return managedList;
    }

    /// <summary>
    /// Converts a managed array to an Il2Cpp array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="managedArray">The managed array to convert.</param>
    /// <returns>An Il2Cpp array containing the same elements, or null if input is null.</returns>
    internal static T[] ToIl2CppArray<T>(this T[] managedArray)
    {
        if (managedArray == null) return null;

        var il2cppArray = new T[managedArray.Length];
        for (int i = 0; i < managedArray.Length; i++)
            il2cppArray[i] = managedArray[i];
        return il2cppArray;
    }

    /// <summary>
    /// Converts an Il2Cpp array to a managed array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="il2cppArray">The Il2Cpp array to convert.</param>
    /// <returns>A managed array containing the same elements, or null if input is null.</returns>
    internal static T[] ToManagedArray<T>(this T[] il2cppArray)
    {
        if (il2cppArray == null) return null;

        var managedArray = new T[il2cppArray.Length];
        for (int i = 0; i < il2cppArray.Length; i++)
            managedArray[i] = il2cppArray[i];
        return managedArray;
    }

    /// <summary>
    /// Converts a managed List to an Il2Cpp array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="managedList">The managed List to convert.</param>
    /// <returns>An Il2Cpp array containing the same elements, or null if input is null.</returns>
    internal static T[] ToIl2CppArray<T>(this List<T> managedList)
    {
        if (managedList == null) return null;

        var il2cppArray = new T[managedList.Count];
        for (int i = 0; i < managedList.Count; i++)
            il2cppArray[i] = managedList[i];
        return il2cppArray;
    }

    /// <summary>
    /// Converts an Il2Cpp List to an Il2Cpp array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="il2cppList">The Il2Cpp List to convert.</param>
    /// <returns>An Il2Cpp array containing the same elements, or null if input is null.</returns>
    internal static T[] ToIl2CppArray<T>(this Il2CppSystem.Collections.Generic.List<T> il2cppList)
    {
        if (il2cppList == null) return null;

        var il2cppArray = new T[il2cppList.Count];
        for (int i = 0; i < il2cppList.Count; i++)
            il2cppArray[i] = il2cppList[i];
        return il2cppArray;
    }

    /// <summary>
    /// Converts any IEnumerable to an Il2Cpp array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="enumerable">The IEnumerable to convert.</param>
    /// <returns>An Il2Cpp array containing the same elements, or null if input is null.</returns>
    internal static T[] ToIl2CppArray<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null) return null;

        if (enumerable is List<T> list)
            return list.ToIl2CppArray();
        if (enumerable is T[] array)
            return array.ToIl2CppArray();

        var tempList = new List<T>(enumerable);
        return tempList.ToIl2CppArray();
    }
}