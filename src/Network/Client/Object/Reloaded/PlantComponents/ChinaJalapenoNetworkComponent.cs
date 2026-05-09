using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Utilities.Modded;
using System.Reflection;
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
        _awakeTexture = PlantNetworked._Plant.mController.m_meshRenderer.material.mainTexture;
        _sleepingTexture = Assembly.GetExecutingAssembly().LoadSpriteFromResources("ReplantedOnline.Resources.Images.Characters.Jalapeno-Sleeping.png").texture;
        PlantNetworked._Plant.mSeedType = SeedType.None;
        PlantNetworked._Plant.SetSleeping(true);
        PlantNetworked._Plant.PlayIdleAnim(0);
        PlantNetworked._Plant.mPlantHealth = int.MaxValue;
        PlantNetworked._Plant.mPlantMaxHealth = int.MaxValue;
        PlantNetworked._Plant.mX -= 40;
        PlantNetworked._Plant.mController.m_visualOffset = PlantNetworked._Plant.mController.m_visualOffset + new Vector3(100f, 0f, 0f);
        PlantNetworked._Plant.mController.m_shadowController.gameObject.SetActive(false);

        _lastHighContrast = !Instances.GameplayActivity.SettingsService.HighContrast;
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);
    }

    private bool _activated;
    internal override void Update()
    {
        UpdateHighContrast(Instances.GameplayActivity.SettingsService.HighContrast);

        if (PlantNetworked.AmOwner)
        {
            foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
            {
                if (zombie.mRow != PlantNetworked.GridY) continue;
                if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                if (zombie.IsDeadOrDying()) continue;

                if (PlantNetworked._Plant.mX + 100 >= zombie.mX)
                {
                    if (!_activated)
                    {
                        PlantNetworked._Plant.mSeedType = SeedType.Jalapeno;
                        PlantNetworked._Plant.SetSleeping(false);
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
            PlantNetworked._Plant.mController.m_meshRenderer.material.mainTexture = _awakeTexture;
            base.Update();
        }
        else
        {
            PlantNetworked._Plant.mBlinkCountdown = 100;
            PlantNetworked._Plant.mController.m_meshRenderer.material.mainTexture = _sleepingTexture;
        }
    }

    private bool _lastHighContrast;
    private void UpdateHighContrast(bool useHighContrast)
    {
        if (useHighContrast == _lastHighContrast) return;
        _lastHighContrast = useHighContrast;

        if (useHighContrast)
        {
            PlantNetworked._Plant.mController.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0.4f);
        }
        else
        {
            PlantNetworked._Plant.mController.m_materialEffectController.SetHighContrastColor(new Color(1f, 1f, 0f), 0f);
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
            PlantNetworked._Plant.mSeedType = SeedType.Jalapeno;
            PlantNetworked._Plant.SetSleeping(false);
        }
    }

    protected override void DoSpecial_PlantSide()
    {
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (zombie.mRow != PlantNetworked.GridY) continue;
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
