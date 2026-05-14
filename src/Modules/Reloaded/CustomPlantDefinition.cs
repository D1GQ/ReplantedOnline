using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using MelonLoader;
using ReplantedOnline.Data.Asset;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Il2Cpp;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Modules.Reloaded;

/// <summary>
/// Represents a custom plant definition that can be registered.
/// </summary>
[RegisterTypeInIl2Cpp]
internal class CustomPlantDefinition : PlantDefinition
{
    private bool _isZombie;

    /// <summary>
    /// Creates a zombie seed packet definition for a custom seed type.
    /// </summary>
    /// <param name="customSeedType">The custom seed type that must have a valid zombie type configured.</param>
    /// <param name="translationName">The base name used for localization and asset identification.</param>
    /// <param name="sprite">The sprite image to use for the plant's visual representation.</param>
    /// <returns>
    /// A new <see cref="CustomPlantDefinition"/> instance configured as a zombie seed packet,
    /// or <c>null</c> if the provided <paramref name="customSeedType"/> does not have a valid zombie type.
    /// </returns>
    internal static CustomPlantDefinition CreateZombieSeedPacketDefinition(CustomSeedType customSeedType, string translationName, Sprite sprite)
    {
        if (!customSeedType.HasValidZombieType())
        {
            return null;
        }

        var customPlantDefinition = CreateInstance<CustomPlantDefinition>();
        customPlantDefinition.name = $"CustomPlantDefinition-{translationName}";
        customPlantDefinition._isZombie = true;

        customPlantDefinition.m_seedType = customSeedType;
        customPlantDefinition.m_animationType = customSeedType;
        Il2CppExtensions.AssignIl2CppStringToField(customPlantDefinition, nameof(customPlantDefinition.m_plantName), translationName + "_ZOMBIE");
        Il2CppExtensions.AssignIl2CppStringToField(customPlantDefinition, nameof(customPlantDefinition.m_plantToolTip), translationName + "_ZOMBIE");
        Il2CppExtensions.AssignIl2CppStringToField(customPlantDefinition, nameof(customPlantDefinition.m_defaultSkin), "Normal");

        CustomAssetReference<AssetReferenceSprite> imageRef = new($"CustomPlantDefinition:{translationName}", sprite);
        CustomAssetReference.Register(imageRef);
        customPlantDefinition.m_plantImage = imageRef.AssetRef;
        customPlantDefinition.m_versusImage = imageRef.AssetRef;

        customPlantDefinition.m_previewSpriteScale = 1f;

        AssetReferenceSprite emptyImageRef = new("");
        AssetReferenceGameObject emptyGoRef = new("");
        customPlantDefinition.m_prefab = emptyGoRef;
        customPlantDefinition.m_preview = emptyGoRef;
        customPlantDefinition.m_easterEggGameObject = emptyGoRef;
        customPlantDefinition.m_preorderGameObject = emptyGoRef;
        customPlantDefinition.m_chinaGameObject = emptyGoRef;
        customPlantDefinition.m_chinaPlantImage = emptyImageRef;
        customPlantDefinition.m_chinaPreviewSprite = emptyImageRef;
        customPlantDefinition.m_decemberGameObject = emptyGoRef;

        var dataLookup = Instances.IDataService.Cast<DataService>().m_plantDataLoader.Cast<DataLookupLoader<SeedType, PlantDefinition>>();
        dataLookup.m_loadedData.Add(customPlantDefinition);
        dataLookup.m_lookup.Add(customSeedType, customPlantDefinition);
        return customPlantDefinition;
    }
}