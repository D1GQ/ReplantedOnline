using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class ZombieWithBungeeComponent : ZombieNetworkComponent
{
    private enum BungeeSpawningRpcs : byte
    {
        SetPartner
    }

    private ZombieNetworked _partner;
    private bool _hasPartner;

    internal override void Init()
    {
        base.Init();

        if (ZombieNetworked.ZombieType != ZombieType.Bungee)
        {
            Instances.GameplayActivity.StartCoroutine(CoDrop());
        }
        else
        {
            if (ZombieNetworked._Zombie.mController.Il2CppTryCast<ZombieBungeeController>(out var zombieBungeeController))
            {
                zombieBungeeController.m_bungeeTargetSpriteRenderer.color = new(1f, 1f, 1f, 0.6f);
            }
        }
    }

    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;
        if (_partner == null) return;
        if (!_hasPartner) return;

        if (ZombieNetworked.ZombieType == ZombieType.Bungee)
        {
            if (ZombieNetworked._Zombie.mZombiePhase is ZombiePhase.BungeeDiving)
            {
                ZombieNetworked._Zombie.BungeeLiftTarget();
            }
        }
    }

    internal void SendSetPartnerRpc(ZombieNetworked partner)
    {
        if (!_hasPartner)
        {
            _hasPartner = true;
            _partner = partner;
            SendNetworkComponentRpc(BungeeSpawningRpcs.SetPartner, partner);
        }
    }

    [RpcHandler(BungeeSpawningRpcs.SetPartner)]
    private void HandleSetPartnerRpc(ZombieNetworked partnerNetworked)
    {
        if (!_hasPartner)
        {
            _hasPartner = true;
            _partner = partnerNetworked;
        }
    }

    private IEnumerator CoDrop()
    {
        var oldVelX = ZombieNetworked._Zombie.mVelX;
        var oldRenderOrder = ZombieNetworked._Zombie.RenderOrder;
        var oldRect = ZombieNetworked._Zombie.mZombieRect;

        // Temporarily hide and make zombie invulnerable
        ZombieNetworked._Zombie.mZombieRect = new Rect(9999, 9999, 0, 0);
        ZombieNetworked._Zombie.mController.gameObject.SetActive(false);

        if (ZombieNetworked.AmOwner)
        {
            yield return CoSpawnBungee();
        }

        while (!_hasPartner)
        {
            yield return null;
        }

        // Make bungee Zombie invulnerable
        _partner.Dead = true;
        _partner._Zombie.mZombieRect = new Rect(9999, 9999, 0, 0);
        var partnerComponent = _partner.GetNetworkComponent<ZombieWithBungeeComponent>();
        while (!partnerComponent._hasPartner)
        {
            yield return null;
        }

        var theX = Instances.GameplayActivity.Board.GridToPixelX(ZombieNetworked.GridX, ZombieNetworked.GridY);
        ZombieNetworked._Zombie.mPosX = theX - 25;
        ZombieNetworked._Zombie.mController.gameObject.SetActive(true);

        while (_partner._Zombie.mZombiePhase != ZombiePhase.BungeeAtBottom)
        {
            // Animate zombie coming down with bungee zombie
            ZombieNetworked._Zombie.mBungeeOffsetY = _partner._Zombie.mController.GetTrackPosition("").y - 50;
            ZombieNetworked._Zombie.RenderOrder = _partner._Zombie.RenderOrder + 1;
            ZombieNetworked._Zombie.mVelX = 0;
            ZombieNetworked._Zombie.UpdateAnimSpeed();
            yield return null;
        }
        _partner._Zombie.mZombiePhase = ZombiePhase.BungeeRising;

        var component = ZombieNetworked.NetworkComponents.First();
        if (component is ZombieNetworkComponent zombieComponent)
        {
            ZombieNetworked.LogicComponent = zombieComponent;
        }

        // Set zombie up
        ZombieNetworked._Zombie.mBungeeOffsetY = 0;
        ZombieNetworked._Zombie.RenderOrder = oldRenderOrder;
        ZombieNetworked._Zombie.mVelX = oldVelX;
        ZombieNetworked._Zombie.mZombieRect = oldRect;
        ZombieNetworked._Zombie.UpdateAnimSpeed();

        yield return new WaitForSeconds(1f);

        // Despawn bungie zombie
        if (_partner.AmOwner)
        {
            _partner.DespawnAndDestroy();
        }
    }

    private IEnumerator CoSpawnBungee()
    {
        var bungeeNetworked = SeedPacketDefinitions.SpawnZombie(ZombieType.Bungee, ZombieNetworked.GridX, ZombieNetworked.GridY, SpawnType.ZombieWithBungee, true).ZombieNetworked;
        while (!bungeeNetworked.IsOnNetwork)
        {
            yield return null;
        }

        yield return null;

        bungeeNetworked.GetNetworkComponent<ZombieWithBungeeComponent>().SendSetPartnerRpc(ZombieNetworked);
        SendSetPartnerRpc(bungeeNetworked);
    }
}
