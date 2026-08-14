using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Enums.Versus;
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

    private Texture _originalTexture = default!;
    private Texture _sleepingTexture = default!;

    internal sealed override void OnInit(Plant plant)
    {
        _originalTexture = plant.mController.m_meshRenderer.material.mainTexture;
        _sleepingTexture = ReplantedOnlineMod.Assets.Sprites.Character.JalapenoSleeping.Asset.texture;
        plant.mSeedType = SeedType.None;
        plant.SetSleeping(true);
        plant.PlayIdleAnim(0);
        plant.mPlantHealth = int.MaxValue;
        plant.mPlantMaxHealth = int.MaxValue;
        plant.mX -= 40;
        plant.mController.m_visualOffset = plant.mController.m_visualOffset + new Vector3(100f, 0f, 0f);
        plant.mController.m_shadowController.gameObject.SetActive(false);

        _lastHighContrast = !Instances.GameplayActivity.SettingsService.HighContrast;
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);
    }

    private bool _activated;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (plant.mController == null)
            return;

        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);

        if (Net.AmOwner)
        {
            foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
            {
                if (zombie.mRow != Net.BoardUnitY.Grid) continue;
                if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                if (zombie.IsDeadOrDying()) continue;

                if (plant.mX + 100 >= zombie.mX)
                {
                    if (!_activated)
                    {
                        plant.mSeedType = SeedType.Jalapeno;
                        plant.SetSleeping(false);
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
            plant.mController.m_meshRenderer.material.mainTexture = _originalTexture;
            UpdateHealthSync(plant);
        }
        else
        {
            plant.mBlinkCountdown = 100;
            plant.mController.m_meshRenderer.material.mainTexture = _sleepingTexture;
        }
    }

    private bool _lastHighContrast;
    private void UpdateHighContrast(bool useHighContrast)
    {
        if (useHighContrast == _lastHighContrast)
            return;

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

    internal override void OnDeath(Plant? plant, DeathReason deathReason)
    {
        if (plant == null)
            return;

        plant.mController.m_meshRenderer.material.mainTexture = _originalTexture;
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
        var plant = Net.Plant;
        if (plant == null)
            return;

        if (!_activated)
        {
            _activated = true;
            if (Net.Plant != null)
            {
                plant.mSeedType = SeedType.Jalapeno;
                plant.SetSleeping(false);
            }
        }
    }

    protected override void DoSpecial_PlantSide()
    {
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (zombie.mRow != Net.BoardUnitY.Grid) continue;
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
