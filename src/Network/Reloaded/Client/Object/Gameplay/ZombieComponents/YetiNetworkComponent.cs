using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Data.Json.Config.Reloaded;
using ReplantedOnline.Managers.Reloaded;
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
        Enraged
    }

    internal enum YetiState
    {
        Curious,
        Runningback,
        Enraged
    }

    private ZombieConfig _yetiConfig = VersusGameplayManager.GetVersusModeConfig().GetZombieConfig(ZombieType.Yeti);

    internal override void OnInit(Zombie zombie)
    {
        zombie.mPhaseCounter = int.MaxValue;
        zombie.mBodyMaxHealth = 100000;
        zombie.mBodyHealth = 100000;
        zombie.mShieldMaxHealth = 0;
        zombie.mShieldHealth = 0;
    }

    internal YetiState CurrentState = YetiState.Curious;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        switch (CurrentState)
        {
            case YetiState.Curious:
                UpdateCurious(zombie);
                break;
            case YetiState.Runningback:
                UpdateRunningback(zombie);
                break;
            case YetiState.Enraged:
                UpdateEnraged(zombie);
                break;
        }
    }

    private void UpdateCurious(Zombie zombie)
    {
        float t = Mathf.InverseLerp(750f, 350f, zombie.mPosX);
        SetSpeed(Mathf.Lerp(1f, 0.1f, t));

        if (Net.AmOwner)
        {
            if (TryGoIntoEnragedState(zombie))
            {
                return;
            }

            if (zombie.mPosX < 350 && CurrentState != YetiState.Runningback)
            {
                CurrentState = YetiState.Runningback;
                SendRunBackRpc();
            }
        }
    }

    private void UpdateRunningback(Zombie zombie)
    {
        if (Net.AmOwner)
        {
            if (TryGoIntoEnragedState(zombie))
            {
                return;
            }
        }

        float t = Mathf.InverseLerp(400f, 350f, zombie.mPosX);
        SetSpeed(Mathf.Lerp(0.8f, 0.2f, t));
    }

    private bool TryGoIntoEnragedState(Zombie zombie)
    {
        if (zombie.mBodyHealth <= (100000 - _yetiConfig.ArmorHealth))
        {
            CurrentState = YetiState.Enraged;
            SendEnragedRpc();
            return true;
        }

        return false;
    }

    private void UpdateEnraged(Zombie zombie)
    {
        float t = Mathf.InverseLerp(500, 0, zombie.mBodyHealth);
        SetSpeed(Mathf.Lerp(0.8f, 1.4f, t));
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

    private void SendEnragedRpc()
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.mBodyMaxHealth = _yetiConfig.BodyHealth;
        Net.Zombie.mBodyHealth = _yetiConfig.BodyHealth;
        Net.Zombie.DropArm(DamageFlags.DoesntCauseFlash);
        Instances.GameplayActivity.m_audioService.PlayFoleyPitch(FoleyType.NewspaperRarrgh, -18);
        SendNetworkComponentRpc(YetiRpcs.Enraged);
    }

    [RpcHandler(YetiRpcs.Enraged)]
    private void HandleEnragedRpc()
    {
        if (Net.Zombie == null)
            return;

        if (CurrentState != YetiState.Enraged)
        {
            CurrentState = YetiState.Enraged;
            Net.Zombie.mBodyMaxHealth = _yetiConfig.BodyHealth;
            Net.Zombie.mBodyHealth = _yetiConfig.BodyHealth;
            Net.Zombie.DropArm(DamageFlags.DoesntCauseFlash);
            Instances.GameplayActivity.m_audioService.PlayFoleyPitch(FoleyType.NewspaperRarrgh, -18);
        }
    }
}
