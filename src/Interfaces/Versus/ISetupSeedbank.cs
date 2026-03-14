using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using static Il2CppReloaded.Gameplay.SeedChooserScreen;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for configuring seed banks for different teams in versus mode.
/// </summary>
internal interface ISetupSeedbank
{
    /// <summary>
    /// Configures the seed bank for a specific team with initial seeds.
    /// </summary>
    /// <param name="seedBank">The seed bank instance to configure</param>
    /// <param name="seedBankInfo">The seed bank info instance to configure</param>
    /// <param name="team">The team whose seed bank is being configured</param>
    void SetupSeedbank(SeedBank seedBank, SeedBankInfo seedBankInfo, PlayerTeam team);
}