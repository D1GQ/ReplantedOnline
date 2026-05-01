using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class BungeeDropZombieComponent : ZombieNetworkComponent
{
    internal override void Enabled()
    {
        _screamRng = Math.Min(_screamRng, 2);
        ZombieNetworked.StartCoroutine(CoBungeeDropZombie());
    }

    private int _screamRng;

    private IEnumerator CoBungeeDropZombie()
    {
        var zombie = ZombieNetworked._Zombie;

        var originalVelX = zombie.mVelX;
        var originalRenderOrder = zombie.RenderOrder;
        var originalRect = zombie.mZombieRect;

        // Temporarily hide and make zombie invulnerable
        zombie.mZombieRect = new Rect(9999, 9999, 0, 0);
        zombie.mController.gameObject.SetActive(false);

        // Spawn bungee zombie
        var bungee = SeedPacketDefinitions.SpawnZombie(ZombieType.Bungee, ZombieNetworked.GridX, ZombieNetworked.GridY, SpawnType.None, false).Zombie;
        bungee.BungeeLiftTarget(); // Make arms close with 
        bungee.mZombieRect = new Rect(9999, 9999, 0, 0); // Make invulnerable

        // Position zombie under bungee
        var targetX = Instances.GameplayActivity.Board.GridToPixelX(ZombieNetworked.GridX, ZombieNetworked.GridY);
        zombie.mPosX = targetX - 25;
        zombie.mController.gameObject.SetActive(true);

        if (ZombieNetworked.SpawnType == SpawnType.BungeeDropZombieNoTarget)
        {
            SeedPacketDefinitions.SetBungeeTarget(bungee, false);
            bungee.mZombiePhase = ZombiePhase.BungeeDivingScreaming;
            bungee.mAltitude = 500 + (100 * ZombieNetworked.GridY);
        }

        // Animate descent
        while (bungee.mZombiePhase != ZombiePhase.BungeeAtBottom)
        {
            SeedPacketDefinitions.SetBungeeRenderOrder(bungee);
            zombie.mBungeeOffsetY = -bungee.mAltitude;
            zombie.RenderOrder = bungee.RenderOrder + 1;
            zombie.mVelX = 0;
            zombie.UpdateAnimSpeed();
            yield return null;
        }

        if (ZombieNetworked.SpawnType == SpawnType.BungeeDropZombieNoTarget)
        {
            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_ZOMBIE_FALLING_1);
        }

        bungee.mZombiePhase = ZombiePhase.BungeeRising;

        // Restore og network component
        var firstComponent = ZombieNetworked.NetworkComponents.FirstOrDefault();
        if (firstComponent is ZombieNetworkComponent zombieComponent)
        {
            ZombieNetworked.LogicComponent = zombieComponent;
        }

        // Restore original zombie state
        zombie.mBungeeOffsetY = 0;
        zombie.RenderOrder = originalRenderOrder;
        zombie.mVelX = originalVelX;
        zombie.mZombieRect = originalRect;
        zombie.UpdateAnimSpeed();
    }
}
