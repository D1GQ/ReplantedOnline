using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.Components;

/// <inheritdoc/>
internal class ZombieInPoolNetworkComponent : ZombieNetworkComponent
{
    private bool _inPool;
    internal override void Update()
    {
        var zombie = ZombieNetworked._Zombie;
        if (zombie == null) return;
        if (zombie.mBoard == null) return;

        int leftGrid = zombie.mBoard.PixelToGridX(zombie.mPosX + 75f, zombie.mRow);
        int rightGrid = zombie.mBoard.PixelToGridX(zombie.mPosX + 45f, zombie.mRow);
        bool onPoolLeft = zombie.mBoard.IsPoolSquare(leftGrid, zombie.mRow);
        bool onPoolRight = zombie.mBoard.IsPoolSquare(rightGrid, zombie.mRow);
        bool onPool = onPoolLeft && onPoolRight;

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
            zombie.mController.ClipRect(new(0, -50, 999, 170));
        }
    }
}
