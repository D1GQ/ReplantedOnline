using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class ChinaJalapenoNetworkComponent : PlantSpecialNetworkComponent
{
    private enum ChinaJalapenoRpcs : byte
    {
        Activate
    }

    internal override void OnInit()
    {
        PlantNetworked._Plant.mSeedType = SeedType.None;
        PlantNetworked._Plant.SetSleeping(true);
        PlantNetworked._Plant.PlayIdleAnim(0);
        PlantNetworked._Plant.mPlantHealth = int.MaxValue;
        PlantNetworked._Plant.mPlantMaxHealth = int.MaxValue;
        PlantNetworked._Plant.mX -= 40;
        PlantNetworked._Plant.mController.m_visualOffset = PlantNetworked._Plant.mController.m_visualOffset + new Vector3(100f, 0f, 0f);
        PlantNetworked._Plant.mController.m_shadowController.gameObject.SetActive(false);
    }

    private bool _activated;
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
            {
                if (zombie.mRow != PlantNetworked.GridY) continue;
                if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                if (zombie.IsDeadOrDying()) continue;

                if ((PlantNetworked._Plant.mX + 100) >= zombie.mX)
                {
                    if (!_activated)
                    {
                        PlantNetworked._Plant.mSeedType = SeedType.Jalapeno;
                        PlantNetworked._Plant.SetSleeping(false);
                        SendActivateRpc();
                    }

                    if ((zombie.mBodyHealth - 25) > 10)
                    {
                        zombie.TakeDamage(zombie.mBodyHealth - 25, DamageFlags.BypassesShield);
                    }
                    zombie.ApplyBurn();
                }
            }
        }

        if (_activated)
        {
            base.Update();
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

            if ((zombie.mBodyHealth - 25) > 10)
            {
                zombie.TakeDamage(zombie.mBodyHealth - 25, DamageFlags.BypassesShield);
            }

            zombie.ApplyBurn();
        }
    }
}
