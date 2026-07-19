using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Yeti)]
internal sealed class YetiNetworkComponent : ZombieNetworkComponent
{
    private enum YetiRpcs : byte
    {
        RunBack,
        Angry
    }

    internal enum YetiState
    {
        Curious,
        Runningback,
        Angry
    }

    internal override void OnInit()
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.mPhaseCounter = int.MaxValue;
        Net.Zombie.mBodyMaxHealth = 100000;
        Net.Zombie.mBodyHealth = 100000;
    }

    internal YetiState CurrentState = YetiState.Curious;
    internal sealed override void Update()
    {
        if (Net.Zombie == null)
            return;

        switch (CurrentState)
        {
            case YetiState.Curious:
                UpdateCurious();
                break;
            case YetiState.Runningback:
                UpdateRunningback();
                break;
            case YetiState.Angry:
                UpdateAngry();
                break;
        }
    }

    private void UpdateCurious()
    {
        if (Net.Zombie == null)
            return;

        float t = Mathf.InverseLerp(750f, 350f, Net.Zombie.mPosX);
        Net.Zombie.mVelX = Mathf.Lerp(1f, 0.1f, t);
        Net.Zombie.UpdateAnimSpeed();

        if (Net.AmOwner)
        {
            if (TryGoIntoAngryState())
            {
                return;
            }

            if (Net.Zombie.mPosX < 350 && CurrentState != YetiState.Runningback)
            {
                CurrentState = YetiState.Runningback;
                Net.Zombie.mZombiePhase = ZombiePhase.YetiRunning;
                SendRunBackRpc();
            }
        }
    }

    private void UpdateRunningback()
    {
        if (Net.Zombie == null)
            return;

        if (Net.AmOwner)
        {
            if (TryGoIntoAngryState())
            {
                return;
            }
        }

        float t = Mathf.InverseLerp(400f, 350f, Net.Zombie.mPosX);
        Net.Zombie.mVelX = Mathf.Lerp(0.8f, 0.2f, t);
        Net.Zombie.UpdateAnimSpeed();
    }

    private bool TryGoIntoAngryState()
    {
        if (Net.Zombie == null)
            return false;

        if (Net.Zombie.mBodyHealth <= (100000 - 1500))
        {
            CurrentState = YetiState.Angry;
            SendAngryRpc();
            return true;
        }

        return false;
    }

    private void UpdateAngry()
    {
        if (Net.Zombie == null)
            return;

        float t = Mathf.InverseLerp(500, 0, Net.Zombie.mBodyHealth);
        Net.Zombie.mVelX = Mathf.Lerp(0.8f, 1.4f, t);
        Net.Zombie.UpdateAnimSpeed();

    }

    private void SendRunBackRpc()
    {
        if (Net.Zombie == null)
            return;

        SendNetworkComponentRpc(YetiRpcs.RunBack);
    }

    [RpcHandler(YetiRpcs.RunBack)]
    private void HandleRunBackRpc()
    {
        if (Net.Zombie == null)
            return;

        if (CurrentState == YetiState.Curious)
        {
            CurrentState = YetiState.Runningback;
        }
    }

    private void SendAngryRpc()
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.mBodyMaxHealth = 500;
        Net.Zombie.mBodyHealth = 500;
        Net.Zombie.DropArm(DamageFlags.DoesntCauseFlash);
        Instances.GameplayActivity.m_audioService.PlayFoleyPitch(FoleyType.NewspaperRarrgh, -18);
        SendNetworkComponentRpc(YetiRpcs.Angry);
    }

    [RpcHandler(YetiRpcs.Angry)]
    private void HandleAngryRpc()
    {
        if (Net.Zombie == null)
            return;

        if (CurrentState != YetiState.Angry)
        {
            CurrentState = YetiState.Angry;
            Net.Zombie.mBodyMaxHealth = 500;
            Net.Zombie.mBodyHealth = 500;
            Net.Zombie.DropArm(DamageFlags.DoesntCauseFlash);
            Instances.GameplayActivity.m_audioService.PlayFoleyPitch(FoleyType.NewspaperRarrgh, -18);
        }
    }
}
