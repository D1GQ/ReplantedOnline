using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Plant;

[RegisterCharacterConfig]
internal class IceshroomPlantConfig : IPlantConfig
{
    public SeedType Type => SeedType.Iceshroom;

    /// <inheritdoc/>
    public void SetArenaDefinition(PlantDefinition plantDefinition, ArenaTypes arena)
    {
        plantDefinition.m_versusCost = SeedPacketDefinitions.BaseSeedVersusCost[Type];

        if (arena is ArenaTypes.Night or ArenaTypes.PoolNight or ArenaTypes.RoofNight)
        {
            // Add Cost of instant coffee to balance price
            plantDefinition.m_versusCost += 25;
        }
    }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena) => true;

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Plant plant, int gridX, int gridY) { }
}
