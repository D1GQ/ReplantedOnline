using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
internal sealed class ChinaJalapenoNetworkComponent : PlantSpecialNetworkComponent
{
    private enum ChinaJalapenoRpcs : byte
    {
        Activate
    }

    private Texture _awakeTexture = default!;
    private Texture _sleepingTexture = default!;

    internal sealed override void OnInit()
    {
        if (Net.Plant == null) return;

        _awakeTexture = Net.Plant.mController.m_meshRenderer.material.mainTexture;
        _sleepingTexture = ReplantedOnlineMod.Assets.Sprites.Character.JalapenoSleeping.texture;
        Net.Plant.mSeedType = SeedType.None;
        Net.Plant.SetSleeping(true);
        Net.Plant.PlayIdleAnim(0);
        Net.Plant.mPlantHealth = int.MaxValue;
        Net.Plant.mPlantMaxHealth = int.MaxValue;
        Net.Plant.mX -= 40;
        Net.Plant.mController.m_visualOffset = Net.Plant.mController.m_visualOffset + new Vector3(100f, 0f, 0f);
        Net.Plant.mController.m_shadowController.gameObject.SetActive(false);

        _lastHighContrast = !Instances.GameplayActivity.SettingsService.HighContrast;
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);
    }

    private bool _activated;
    internal sealed override void Update()
    {
        if (Net.Plant?.mController == null) return;

        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);

        if (Net.AmOwner)
        {
            foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
            {
                if (zombie.mRow != Net.GridY) continue;
                if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                if (zombie.IsDeadOrDying()) continue;

                if (Net.Plant.mX + 100 >= zombie.mX)
                {
                    if (!_activated)
                    {
                        Net.Plant.mSeedType = SeedType.Jalapeno;
                        Net.Plant.SetSleeping(false);
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
            Net.Plant.mController.m_meshRenderer.material.mainTexture = _awakeTexture;
            base.Update();
        }
        else
        {
            Net.Plant.mBlinkCountdown = 100;
            Net.Plant.mController.m_meshRenderer.material.mainTexture = _sleepingTexture;
        }
    }

    private bool _lastHighContrast;
    private void UpdateHighContrast(bool useHighContrast)
    {
        if (useHighContrast == _lastHighContrast) return;
        _lastHighContrast = useHighContrast;

        if (useHighContrast)
        {
            Net.Plant?.mController?.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0.4f);
        }
        else
        {
            Net.Plant?.mController?.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0f);
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
            if (Net.Plant != null)
            {
                Net.Plant.mSeedType = SeedType.Jalapeno;
                Net.Plant.SetSleeping(false);
            }
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
