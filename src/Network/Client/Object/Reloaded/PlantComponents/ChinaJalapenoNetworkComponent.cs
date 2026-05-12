using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

/// <inheritdoc/>
internal sealed class ChinaJalapenoNetworkComponent : PlantSpecialNetworkComponent
{
    private enum ChinaJalapenoRpcs : byte
    {
        Activate
    }

    private Texture _awakeTexture;
    private Texture _sleepingTexture;

    internal override void OnInit()
    {
        _awakeTexture = Net._Plant.mController.m_meshRenderer.material.mainTexture;
        _sleepingTexture = ReplantedOnlineAssets.Sprites.Character.JalapenoSleeping.texture;
        Net._Plant.mSeedType = SeedType.None;
        Net._Plant.SetSleeping(true);
        Net._Plant.PlayIdleAnim(0);
        Net._Plant.mPlantHealth = int.MaxValue;
        Net._Plant.mPlantMaxHealth = int.MaxValue;
        Net._Plant.mX -= 40;
        Net._Plant.mController.m_visualOffset = Net._Plant.mController.m_visualOffset + new Vector3(100f, 0f, 0f);
        Net._Plant.mController.m_shadowController.gameObject.SetActive(false);

        _lastHighContrast = !Instances.GameplayActivity.SettingsService.HighContrast;
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);
    }

    private bool _activated;
    internal override void Update()
    {
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);

        if (Net.AmOwner)
        {
            foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
            {
                if (zombie.mRow != Net.GridY) continue;
                if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                if (zombie.IsDeadOrDying()) continue;

                if (Net._Plant.mX + 100 >= zombie.mX)
                {
                    if (!_activated)
                    {
                        Net._Plant.mSeedType = SeedType.Jalapeno;
                        Net._Plant.SetSleeping(false);
                        SendActivateRpc();
                    }

                    if (zombie.mBodyHealth - 25 > 10)
                    {
                        zombie.TakeDamage(zombie.mBodyHealth - 25, DamageFlags.BypassesShield);
                    }
                    zombie.ApplyBurn();
                }
            }
        }

        if (_activated)
        {
            Net._Plant.mController.m_meshRenderer.material.mainTexture = _awakeTexture;
            base.Update();
        }
        else
        {
            Net._Plant.mBlinkCountdown = 100;
            Net._Plant.mController.m_meshRenderer.material.mainTexture = _sleepingTexture;
        }
    }

    private bool _lastHighContrast;
    private void UpdateHighContrast(bool useHighContrast)
    {
        if (useHighContrast == _lastHighContrast) return;
        _lastHighContrast = useHighContrast;

        if (useHighContrast)
        {
            Net._Plant.mController.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0.4f);
        }
        else
        {
            Net._Plant.mController.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0f);
        }
    }

    internal void SendActivateRpc()
    {
        if (!_activated)
        {
            _activated = true;
            SendNetworkComponentRpc(ChinaJalapenoRpcs.Activate);
        }
    }

    [RpcHandler(ChinaJalapenoRpcs.Activate)]
    internal void HandleActivateRpc()
    {
        if (!_activated)
        {
            _activated = true;
            Net._Plant.mSeedType = SeedType.Jalapeno;
            Net._Plant.SetSleeping(false);
        }
    }

    protected override void DoSpecial_PlantSide()
    {
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (zombie.mRow != Net.GridY) continue;
            if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
            if (zombie.IsDeadOrDying()) continue;
            if (zombie.mVelX <= 0f) continue;

            if (zombie.mBodyHealth - 25 > 10)
            {
                zombie.TakeDamage(zombie.mBodyHealth - 25, DamageFlags.BypassesShield);
            }

            zombie.ApplyBurn();
        }
    }
}
