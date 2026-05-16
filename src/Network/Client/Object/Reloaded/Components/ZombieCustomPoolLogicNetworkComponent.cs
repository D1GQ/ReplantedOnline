using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Enums.Versus;
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

    internal override void OnInit()
    {
        if (!CanGoInWater())
        {
            return;
        }

        var groundType = Net._Zombie.mBoard.mPlantRow[Net._Zombie.mRow];
        if (groundType == PlantRowType.Pool)
        {
            Net._Zombie.mController.AssignRenderGroupToTrack("Zombie_duckytube", 1);

            // remove arms overlay to appear over the water
            Net._Zombie.mController.SetImageOverride("whitewater", string.Empty);
        }
    }

    internal override void Update()
    {
        if (!CanGoInWater())
        {
            return;
        }

        var zombie = Net._Zombie;
        if (zombie == null) return;
        if (zombie.mBoard == null) return;

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

    private bool CanGoInWater()
    {
        bool typeCheck = Net.ZombieType is not (ZombieType.Bungee or ZombieType.DolphinRider or ZombieType.Snorkel);
        bool phaseCheck = Net._Zombie.mZombiePhase is not (ZombiePhase.BalloonFlying or ZombiePhase.BalloonPopping);
        return typeCheck && phaseCheck;
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
        float target = Net._Zombie.mAltitude - 150;
        while (Net._Zombie.mAltitude > target)
        {
            Net._Zombie.mAltitude -= 25f;
            Net._Zombie.mVelX = 0;
            Net._Zombie.UpdateAnimSpeed();
            Net._Zombie.PoolSplash(true);
            yield return null;
        }
        Net.Dead = true;
        Net._Zombie.DieNoLootOriginal();
        Net.IsReadyToDespawn = true;
    }
}
