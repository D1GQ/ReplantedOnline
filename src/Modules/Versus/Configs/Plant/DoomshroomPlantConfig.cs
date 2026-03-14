using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Plant;

[RegisterCharacterConfig]
internal class DoomshroomPlantConfig : IPlantConfig
{
    public SeedType Type => SeedType.Doomshroom;

    /// <inheritdoc/>
    public void SetArenaDefinition(PlantDefinition plantDefinition, ArenaTypes arena)
    {
        plantDefinition.m_versusCost = SeedPacketDefinitions.BaseSeedVersusCost[Type];

        if (arena == ArenaTypes.Night)
        {
            // Add Cost of instant coffee to balance price
            plantDefinition.m_versusCost += 25;
        }
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Plant plant, int gridX, int gridY) { }
}
