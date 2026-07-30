using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

/// <inheritdoc/>
internal sealed class ZombieCustomPoolLogicNetworkComponent : ZombieNetworkComponent
{
    private enum ZombieInPoolRpcs : byte
    {
        Drown
    }

    internal bool InPool;
    private bool _isDrowning;

    internal WhiteWaterEffect? WhiteWaterEffect { get; private set; } = null;

    internal sealed override void OnInit()
    {
        var zombie = Net.Zombie;
        if (!CanGoInWater() || zombie == null)
        {
            return;
        }

        var groundType = zombie.mBoard.mPlantRow[zombie.mRow];
        if (groundType == PlantRowType.Pool)
        {
            zombie.mController.AssignRenderGroupToTrack("Zombie_duckytube", 1);

            // remove arms overlay to appear over the water
            zombie.mController.SetImageOverride(Animations.WHITEWATER_OBJECT.Slot, Animations.WHITEWATER_OBJECT.Image);
        }

        WhiteWaterEffect ??= WhiteWaterEffect.Create(zombie.mController, false);
    }

    internal sealed override void OnDestroyed()
    {
        if (WhiteWaterEffect != null)
        {
            UnityEngine.Object.Destroy(WhiteWaterEffect.gameObject);
        }
    }

    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (!CanGoInWater())
        {
            return;
        }

        if (zombie == null)
            return;

        if (zombie.mBoard == null)
            return;

        if (zombie.mController == null)
            return;

        var groundType = zombie.mBoard.mPlantRow[zombie.mRow];
        if (groundType != PlantRowType.Pool)
        {
            return;
        }

        bool onPool = zombie.mPosX > 0f && zombie.mPosX < 680f;

        if (Net.Event == EventState.PushBack)
        {
            onPool = false;
        }

        if (!InPool && onPool)
        {
            InPool = true;
            zombie.mZombieHeight = ZombieHeight.InToPool;
            zombie.StartWalkAnim(0);
            zombie.PoolSplash(true);
        }
        else if (InPool && !onPool)
        {
            zombie.mZombieHeight = ZombieHeight.OutOfPool;
            zombie.PoolSplash(true);
            InPool = false;
        }

        if (InPool || zombie.mAltitude < 5)
        {
            zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            zombie.mController.m_shadowController.m_spriteRenderer.color = Color.clear;
        }
        else
        {
            zombie.mController.ClearClipRect();
            zombie.mController.m_shadowController.m_spriteRenderer.color = Color.white;
        }

        UpdateWhiteWaterEffect(zombie, onPool);

        if (InPool)
        {
            if (Net.AmOwner)
            {
                if (!_isDrowning && zombie.mZombieType == ZombieType.Imp && zombie.mZombiePhase == ZombiePhase.ZombieNormal)
                {
                    _isDrowning = true;
                    SendDrownRpc();
                    Net.StartCoroutine(CoDrown());
                    Net.DespawnAndDestroyWhenNullOrDead(true);
                }
            }
        }
    }

    private void UpdateWhiteWaterEffect(Zombie zombie, bool onPool)
    {
        if (WhiteWaterEffect == null)
            return;

        var active = InPool && !zombie.mDead && zombie.mZombiePhase != ZombiePhase.RisingFromGrave && onPool && zombie.mAltitude < -35f;
        WhiteWaterEffect.gameObject.SetActive(active);

        switch (Net.ZombieType)
        {
            case ZombieType.TrashCan:
                if (zombie.mShieldType != ShieldType.None)
                {
                    WhiteWaterEffect.transform.localPosition = new(-15f, 110f, 0f);
                    WhiteWaterEffect.transform.localScale = new(1.05f, 1f, 1f);
                    return;
                }
                break;
            case ZombieType.Door:
                if (zombie.mShieldType != ShieldType.None)
                {
                    WhiteWaterEffect.transform.localPosition = new(-25f, 110f, 0f);
                    WhiteWaterEffect.transform.localScale = new(1.1f, 1f, 1f);
                    return;
                }
                break;
            case ZombieType.Newspaper:
                if (zombie.mShieldType != ShieldType.None)
                {
                    WhiteWaterEffect.transform.localPosition = new(-30f, 114f, 0f);
                    WhiteWaterEffect.transform.localScale = new(1.2f, 1f, 1f);
                }
                else
                {
                    WhiteWaterEffect.transform.localPosition = new(-20f, 114f, 0f);
                    WhiteWaterEffect.transform.localScale = new(1.1f, 1f, 1f);
                }
                return;
            case ZombieType.JackInTheBox:
                WhiteWaterEffect.transform.localPosition = new(-5.9f, 114f, 0f);
                WhiteWaterEffect.transform.localScale = new(zombie.mZombieRect.width * 0.020f, 1f, 1f);
                return;
            case ZombieType.Football:
                WhiteWaterEffect.transform.localPosition = new(0f, 105f, 0f);
                WhiteWaterEffect.transform.localScale = new(1.25f, 1f, 1f);
                return;
            case ZombieType.Yeti:
                WhiteWaterEffect.transform.localPosition = new(15f, 155f, 0f);
                WhiteWaterEffect.transform.localScale = new(1.7f, 1f, 1f);
                return;
            default:
                break;
        }

        WhiteWaterEffect.transform.localPosition = new(5f, 110f, 0f);
        WhiteWaterEffect.transform.localScale = new(zombie.mZombieRect.width * 0.020f, 1f, 1f);
    }

    private bool CanGoInWater()
    {
        var zombie = Net.Zombie;
        if (zombie == null)
            return false;

        bool typeCheck = zombie.mZombieType is not (ZombieType.Gravestone or ZombieType.Bungee or ZombieType.DolphinRider or ZombieType.Snorkel);
        bool phaseCheck = zombie.mZombiePhase is not (ZombiePhase.BalloonFlying or ZombiePhase.BalloonPopping
            or ZombiePhase.ImpGettingThrown or ZombiePhase.ImpLanding);
        return typeCheck && phaseCheck && zombie.mController?.gameObject.active == true;
    }

    private void SendDrownRpc()
    {
        SendNetworkComponentRpc(ZombieInPoolRpcs.Drown);
    }

    [RpcHandler(ZombieInPoolRpcs.Drown)]
    private void HandleDrownRpc()
    {
        if (!_isDrowning)
        {
            _isDrowning = true;
            Net.StartCoroutine(CoDrown());
        }
    }

    private IEnumerator CoDrown()
    {
        var zombie = Net.Zombie;
        if (zombie == null)
            yield break;

        float target = zombie.mAltitude - 150;
        while (zombie.mAltitude > target)
        {
            zombie.mAltitude -= 25f;
            zombie.mVelX = 0;
            zombie.UpdateAnimSpeed();
            zombie.PoolSplash(true);
            zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            yield return null;
        }
        zombie.DieNoLootOriginal();
        Net.Dying = true;
        Net.IsReadyToDespawn = true;
    }
}
