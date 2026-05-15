using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Data.Asset;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Structs.Reloaded;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Modules.Reloaded;

/// <summary>
/// Represents a custom plant definition that can be registered.
/// </summary>
internal static class CustomPlantDefinition
{
    /// <summary>
    /// Creates a zombie seed packet definition for a custom seed type.
    /// </summary>
    /// <param name="customSeedType">The custom seed type that must have a valid zombie type configured.</param>
    /// <param name="translationName">The base name used for localization and asset identification.</param>
    /// <param name="sprite">The sprite image to use for the plant's visual representation.</param>
    /// <returns>
    /// A new <see cref="PlantDefinition"/> instance configured as a zombie seed packet,
    /// or <c>null</c> if the provided <paramref name="customSeedType"/> does not have a valid zombie type.
    /// </returns>
    internal static PlantDefinition CreateZombieSeedPacketDefinition(CustomSeedType customSeedType, string translationName, Sprite sprite)
    {
        if (!customSeedType.HasValidZombieType())
        {
            return null;
        }

        var customPlantDefinition = ScriptableObject.CreateInstance<PlantDefinition>();
        customPlantDefinition.name = $"CustomPlantDefinition-{translationName}";

        customPlantDefinition.m_seedType = customSeedType;
        customPlantDefinition.m_animationType = customSeedType;
        customPlantDefinition.m_plantName = translationName + "_ZOMBIE";
        customPlantDefinition.m_plantToolTip = translationName + "_ZOMBIE";
        customPlantDefinition.m_defaultSkin = "Normal";

        CustomAssetReference<AssetReferenceSprite> imageRef = new($"CustomPlantDefinition:{translationName}", sprite);
        CustomAssetReference.Register(imageRef);
        customPlantDefinition.m_plantImage = imageRef.AssetRef;
        customPlantDefinition.m_versusImage = imageRef.AssetRef;
        customPlantDefinition.m_previewSprite = Instances.IDataService.GetZombieDefinition(customSeedType).PreviewSprite;
        customPlantDefinition.m_previewSpriteOffset = Instances.IDataService.GetZombieDefinition(customSeedType).PreviewSpriteOffset;

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