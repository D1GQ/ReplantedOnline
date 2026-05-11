using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Plant;

[RegisterPlantConfig(SeedType.Fumeshroom)]
internal class FumeshroomPlantConfig : IPlantConfig
{
    /// <inheritdoc/>
    public void SetArenaDefinition(PlantDefinition plantDefinition, ArenaTypes arena)
    {
        plantDefinition.m_versusCost = SeedPacketDefinitions.BaseSeedVersusCost[SeedType.Fumeshroom];

        if (arena.IsArenaAtNight())
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
