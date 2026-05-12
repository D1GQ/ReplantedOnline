using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Patches.Hooks;

[DetourHook]
internal static class ChallengeHook
{
    [DetourHook(typeof(Challenge), nameof(Challenge.IsZombieSeedType))]
    private static bool Challenge_IsZombieSeedType_Hook(Func<SeedType, bool> orig, SeedType seed)
    {
        CustomSeedType customSeedType = seed;

        if (customSeedType.HasValidZombieType())
        {
            return true;
        }

        return orig(seed);
    }

    [DetourHook(typeof(Challenge), nameof(Challenge.IZombieSeedTypeToZombieType))]
    private static ZombieType Challenge_IZombieSeedTypeToZombieType_Hook(Func<SeedType, ZombieType> orig, SeedType seed)
    {
        CustomSeedType customSeedType = seed;

        if (customSeedType.HasValidZombieType())
        {
            return customSeedType;
        }

        return orig(seed);
    }
}