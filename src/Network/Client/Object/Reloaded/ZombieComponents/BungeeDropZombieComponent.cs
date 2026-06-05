using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Utilities.Unity;
using System.Collections;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
internal sealed class BungeeDropZombieComponent : ZombieNetworkComponent
{
    internal override void OnEnabled()
    {
        _screamRng = Math.Min(_screamRng, 2);
        Net.StartCoroutine(CoBungeeDropZombie());
    }

    private int _screamRng;

    private IEnumerator CoBungeeDropZombie()
    {
        var zombie = Net.Zombie;
        if (zombie == null) yield break;

        var originalVelX = zombie.mVelX;
        var originalRenderOrder = zombie.RenderOrder;

        // Temporarily hide and make zombie invulnerable
        zombie.mZombieRect = zombie.mZombieRect.AsNonInteractable();
        zombie.mZombieAttackRect = zombie.mZombieAttackRect.AsNonInteractable();
        zombie.mController.gameObject.SetActive(false);

        // Spawn bungee zombie
        var bungee = SeedPacketDefinitions.SpawnZombie(ZombieType.Bungee, Net.GridX, Net.GridY, SpawnType.None, false).Zombie;
        bungee.BungeeLiftTarget(); // Make arms close with 
        bungee.mZombieRect = RectUtils.NonInteractableRect; // Make invulnerable

        // Position zombie under bungee
        zombie.mController.gameObject.SetActive(true);

        if (Net.SpawnType == SpawnType.BungeeDropZombieNoTarget)
        {
            SeedPacketDefinitions.SetBungeeTarget(bungee, false);
            bungee.mZombiePhase = ZombiePhase.BungeeDivingScreaming;
            bungee.mAltitude = 500 + 100 * Net.GridY;
        }

        // Animate descent
        while (bungee.mZombiePhase is not (ZombiePhase.BungeeAtBottom or ZombiePhase.BungeeRising))
        {
            bungee.mPosX = zombie.mPosX + 25;
            SeedPacketDefinitions.SetBungeeRenderOrder(bungee);
            zombie.mBungeeOffsetY = -bungee.mAltitude;
            zombie.RenderOrder = bungee.RenderOrder + 1;
            zombie.mVelX = 0;
            zombie.UpdateAnimSpeed();
            yield return null;
        }

        if (Net.SpawnType == SpawnType.BungeeDropZombieNoTarget)
        {
            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_ZOMBIE_FALLING_1);
        }

        bungee.mZombiePhase = ZombiePhase.BungeeRising;

        // Restore og network component
        var firstComponent = Net.NetworkComponents.FirstOrDefault();
        if (firstComponent is ZombieNetworkComponent zombieComponent)
        {
            Net.LogicComponent = zombieComponent;
        }

        // Restore original zombie state
        zombie.mBungeeOffsetY = 0;
        zombie.RenderOrder = originalRenderOrder;
        zombie.mVelX = originalVelX;
        zombie.mZombieRect = zombie.mZombieRect.AsInteractable();
        zombie.mZombieAttackRect = zombie.mZombieAttackRect.AsInteractable();
        zombie.UpdateAnimSpeed();
    }
}
