using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Data.Asset;
using ReplantedOnline.Utilities.Il2cpp;
using System.Runtime.InteropServices;
using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Patches.Hooks;

[DetourHook]
[NativeDetourHook]
internal static class AssetReferenceHook
{
    [DetourHook(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
    private static bool AssetReference_RuntimeKeyIsValid_Hook(Func<AssetReference, bool> orig, AssetReference instance)
    {
        if (CustomAssetReference.IsValid(instance))
        {
            return true;
        }

        return orig(instance);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate IntPtr LoadAssetAsyncDelegate(IntPtr @this, IntPtr keyPtr, Il2CppMethodInfo* methodInfo);

    [NativeDetourHook<LoadAssetAsyncDelegate>(typeof(Addressables), nameof(Addressables.LoadAssetAsync), [typeof(Il2CppSystem.Object)], [typeof(UnityEngine.Object)])]
    private static unsafe IntPtr Addressables_LoadAssetAsync_Hook(IntPtr @this, IntPtr keyPtr, Il2CppMethodInfo* methodInfo)
    {
        var keyClassPtr = IL2CPP.il2cpp_object_get_class(keyPtr);

        string assetGuid = null;

        if (keyClassPtr.IsSubclassOf(Il2CppClassPointerStore<AssetReference>.NativeClassPtr))
        {
            assetGuid = new AssetReference(keyPtr).AssetGUID;
        }
        else if (keyClassPtr == Il2CppClassPointerStore<string>.NativeClassPtr)
        {
            assetGuid = IL2CPP.Il2CppStringToManaged(keyPtr);
        }

        if (assetGuid == null)
        {
            return NativeDetourHook<LoadAssetAsyncDelegate>.Orig(@this, keyPtr, methodInfo);
        }

        var customReference = CustomAssetReference.GetByGuid(assetGuid);

        if (customReference != null)
        {
            var operation = customReference.LoadAssetAsync();
            if (operation.IsValid())
            {
                return IL2CPP.il2cpp_object_unbox(operation.Pointer);
            }
        }

        return NativeDetourHook<LoadAssetAsyncDelegate>.Orig(@this, keyPtr, methodInfo);
    }
}
