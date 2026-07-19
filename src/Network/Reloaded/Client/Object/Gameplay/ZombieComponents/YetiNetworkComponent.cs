using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Yeti)]
internal sealed class YetiNetworkComponent : ZombieNetworkComponent
{
    private enum YetiRpcs : byte
    {
        RunBack
    }

    internal override void OnInit()
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.mPhaseCounter = int.MaxValue;
    }

    private bool _isRunningBack;
    internal sealed override void Update()
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.mBodyMaxHealth = 1000;
        Net.Zombie.mBodyHealth = 1000;

        if (Net.Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            float t = Mathf.InverseLerp(750f, 350f, Net.Zombie.mPosX);
            Net.Zombie.mVelX = Mathf.Lerp(1f, 0.1f, t);
            Net.Zombie.UpdateAnimSpeed();
        }
        else if (Net.Zombie.mZombiePhase == ZombiePhase.YetiRunning)
        {
            float t = Mathf.InverseLerp(400f, 350f, Net.Zombie.mPosX);
            Net.Zombie.mVelX = Mathf.Lerp(0.8f, 0.2f, t);
            Net.Zombie.UpdateAnimSpeed();
        }

        if (Net.AmOwner)
        {
            if (Net.Zombie.mPosX < 350 && !_isRunningBack)
            {
                _isRunningBack = true;
                Net.Zombie.mPhaseCounter = 0;
                SendRunBackRpc();
            }
        }
    }

    private void SendRunBackRpc()
    {
        SendNetworkComponentRpc(YetiRpcs.RunBack);
    }

    [RpcHandler(YetiRpcs.RunBack)]
    private void HandleRunBackRpc()
    {
        if (Net.Zombie == null)
            return;

        if (!_isRunningBack)
        {
            _isRunningBack = true;
            Net.Zombie.mPhaseCounter = 0;
        }
    }
}
