using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Plant;

[RegisterPlantConfig(SeedType.InstantCoffee)]
internal sealed class InstantCoffeePlantConfig : IPlantConfig
{
    /// <inheritdoc/>
    public void SetArenaDefinition(PlantDefinition plantDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena)
    {
        return arena is ArenaTypes.Day or ArenaTypes.Pool or ArenaTypes.Roof or ArenaTypes.China;
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Plant plant, int gridX, int gridY) { }
}
