using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Data.Asset;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
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
    /// <param name="translationStr">The translation string used for localization and asset identification.</param>
    /// <param name="seedPacketSprite">The sprite image to use for the zombies seedpacket icom.</param>
    /// <param name="description">An optional description used for the seedpacket.</param>
    /// <returns>
    /// A new <see cref="PlantDefinition"/> instance configured as a zombie seed packet,
    /// or <c>null</c> if the provided <paramref name="customSeedType"/> does not have a valid zombie type.
    /// </returns>
    internal static PlantDefinition? CreateZombieSeedPacketDefinition(CustomSeedType customSeedType, string translationStr, Sprite seedPacketSprite, string? description = null)
    {
        if (!customSeedType.HasValidZombieType())
        {
            return null;
        }

        string nameTrim = translationStr.ToUpper().Replace("_", "");

        var customPlantDefinition = ScriptableObject.CreateInstance<PlantDefinition>();
        customPlantDefinition.name = $"CustomPlantDefinition-{nameTrim}";

        customPlantDefinition.m_seedType = customSeedType;
        customPlantDefinition.m_animationType = customSeedType;
        customPlantDefinition.m_plantName = translationStr;
        customPlantDefinition.m_defaultSkin = "Normal";

        CustomAssetReference<AssetReferenceSprite> seedPacketImageAsset = new($"CustomPlantDefinition:{nameTrim}-SeedPacketIcon", seedPacketSprite);
        CustomAssetReference.Register(seedPacketImageAsset);
        customPlantDefinition.m_plantImage = seedPacketImageAsset.AssetRef;
        customPlantDefinition.m_versusImage = seedPacketImageAsset.AssetRef;


        var zombieAlmanacData = Instances.IDataService.ZombieAlmanacData.Cast<Il2CppSystem.Collections.Generic.List<AlmanacEntryData>>();
        foreach (var zombieAlmanac in zombieAlmanacData)
        {
            if (zombieAlmanac.ZombieType != customSeedType) continue;

            customPlantDefinition.m_previewSprite = zombieAlmanac.EntryThumbnail;
            customPlantDefinition.m_previewSpriteScale = 1f;
            customPlantDefinition.m_previewSpriteOffset = new(115f, -184f);

            if (description == null)
            {
                customPlantDefinition.m_plantToolTip =
                    ReplantedOnlineMod.Constants.Reloaded.REDIRECT_ALMANAC_PREFIX + (int)(SeedType)customSeedType;
            }
            else
            {
                customPlantDefinition.m_plantToolTip = "raw:" + description;
            }

            break;
        }

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

    internal static bool TryGetAlmanacDescription(CustomSeedType customSeedType, out string description)
    {
        var almanacDataList = Instances.IDataService.Cast<DataService>().m_almanacDataLoader.Cast<DataCategorizedLoader<AlmanacEntryType, AlmanacEntryData>>().Data;
        foreach (var almanacData in almanacDataList)
        {
            if (!customSeedType.HasValidZombieType())
            {
                if (almanacData.SeedType != customSeedType) continue;
            }
            else
            {
                if (almanacData.ZombieType != customSeedType) continue;
            }

            description = StringUtils.RemoveHtmlText(almanacData.EntryDescription.Split("\n").First());

            return true;
        }

        description = string.Empty;
        return false;
    }
}