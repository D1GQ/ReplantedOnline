using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.Components;

/// <inheritdoc/>
internal class ZombieCustomPoolLogicNetworkComponent : ZombieNetworkComponent
{
    private enum ZombieInPoolRpcs : byte
    {
        Drown
    }

    private bool _inPool;
    private bool _isDrowning;

    private WhiteWaterEffect _whiteWaterEffect;

    internal override void OnInit()
    {
        if (!CanGoInWater())
        {
            return;
        }

        var groundType = Net.Zombie.mBoard.mPlantRow[Net.Zombie.mRow];
        if (groundType == PlantRowType.Pool)
        {
            Net.Zombie.mController.AssignRenderGroupToTrack("Zombie_duckytube", 1);

            // remove arms overlay to appear over the water
            Net.Zombie.mController.SetImageOverride("whitewater", string.Empty);
        }

        _whiteWaterEffect ??= WhiteWaterEffect.Create(Net.Zombie.mController, false);
    }

    internal override void Update()
    {
        if (!CanGoInWater())
        {
            return;
        }

        var zombie = Net.Zombie;
        if (zombie == null) return;
        if (zombie.mBoard == null) return;
        if (zombie.mController == null) return;

        int leftGrid = zombie.mBoard.PixelToGridX(zombie.mPosX + 75f, zombie.mRow);
        int rightGrid = zombie.mBoard.PixelToGridX(zombie.mPosX + 45f, zombie.mRow);
        bool onPoolLeft = zombie.mBoard.IsPoolSquare(leftGrid, zombie.mRow);
        bool onPoolRight = zombie.mBoard.IsPoolSquare(rightGrid, zombie.mRow);
        bool onPool = onPoolLeft && onPoolRight;

        if (Net.Event == EventState.PushBack)
        {
            onPool = false;
        }

        if (!_inPool && onPool && zombie.mPosX < 680f)
        {
            _inPool = true;
            zombie.mZombieHeight = ZombieHeight.InToPool;
            zombie.StartWalkAnim(0);
            zombie.PoolSplash(true);
            zombie.mController.m_shadowController.m_spriteRenderer.color = Color.white * 0f;
        }
        else if (_inPool && !onPool)
        {
            zombie.mZombieHeight = ZombieHeight.OutOfPool;
            zombie.PoolSplash(true);
            zombie.mController.ClearClipRect();
            zombie.mController.m_shadowController.m_spriteRenderer.color = Color.white;
            _inPool = false;
        }

        UpdateWhiteWaterEffect(onPool);

        if (_inPool)
        {
            zombie.mController.ClipRect(new(-500, -500, 1000, 615));

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

    private void UpdateWhiteWaterEffect(bool onPool)
    {
        if (_whiteWaterEffect == null) return;

        var zombie = Net.Zombie;
        var active = _inPool && !zombie.mDead && zombie.mZombiePhase != ZombiePhase.RisingFromGrave && onPool && zombie.mAltitude < -35f;
        _whiteWaterEffect.gameObject.SetActive(active);

        switch (Net.ZombieType)
        {
            case ZombieType.TrashCan:
                if (Net.Zombie.mShieldType != ShieldType.None)
                {
                    _whiteWaterEffect.transform.localPosition = new(-15f, 110f, 0f);
                    _whiteWaterEffect.transform.localScale = new(1.05f, 1f, 1f);
                    return;
                }
                break;
            case ZombieType.Door:
                if (Net.Zombie.mShieldType != ShieldType.None)
                {
                    _whiteWaterEffect.transform.localPosition = new(-25f, 110f, 0f);
                    _whiteWaterEffect.transform.localScale = new(1.1f, 1f, 1f);
                    return;
                }
                break;
            case ZombieType.Newspaper:
                if (Net.Zombie.mShieldType != ShieldType.None)
                {
                    _whiteWaterEffect.transform.localPosition = new(-30f, 114f, 0f);
                    _whiteWaterEffect.transform.localScale = new(1.2f, 1f, 1f);
                }
                else
                {
                    _whiteWaterEffect.transform.localPosition = new(-20f, 114f, 0f);
                    _whiteWaterEffect.transform.localScale = new(1.1f, 1f, 1f);
                }
                return;
            case ZombieType.JackInTheBox:
                _whiteWaterEffect.transform.localPosition = new(-5.9f, 114f, 0f);
                _whiteWaterEffect.transform.localScale = new(Net.Zombie.mZombieRect.width * 0.020f, 1f, 1f);
                return;
            case ZombieType.Football:
                _whiteWaterEffect.transform.localPosition = new(0f, 105f, 0f);
                _whiteWaterEffect.transform.localScale = new(1.25f, 1f, 1f);
                return;
            default:
                break;
        }

        _whiteWaterEffect.transform.localPosition = new(5f, 110f, 0f);
        _whiteWaterEffect.transform.localScale = new(Net.Zombie.mZombieRect.width * 0.020f, 1f, 1f);
    }

    private bool CanGoInWater()
    {
        bool typeCheck = Net.ZombieType is not (ZombieType.Gravestone or ZombieType.Bungee or ZombieType.DolphinRider or ZombieType.Snorkel);
        bool phaseCheck = Net.Zombie.mZombiePhase is not (ZombiePhase.BalloonFlying or ZombiePhase.BalloonPopping
            or ZombiePhase.ImpGettingThrown or ZombiePhase.ImpLanding);
        return typeCheck && phaseCheck && Net.Zombie.mController?.gameObject.active == true;
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
        float target = Net.Zombie.mAltitude - 150;
        while (Net.Zombie.mAltitude > target)
        {
            Net.Zombie.mAltitude -= 25f;
            Net.Zombie.mVelX = 0;
            Net.Zombie.UpdateAnimSpeed();
            Net.Zombie.PoolSplash(true);
            Net.Zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            yield return null;
        }
        Net.Dead = true;
        Net.Zombie.DieNoLootOriginal();
        Net.IsReadyToDespawn = true;
    }
}
