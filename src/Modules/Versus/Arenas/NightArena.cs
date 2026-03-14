using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Arenas;

/// <inheritdoc/>
internal class NightArena : IArena
{
    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.Night;

    /// <inheritdoc/>
    public void SetupArena(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnStart(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayStart(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}
