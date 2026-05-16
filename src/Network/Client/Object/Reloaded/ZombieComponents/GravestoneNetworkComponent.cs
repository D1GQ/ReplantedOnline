using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gravestone)]
internal sealed class GravestoneNetworkComponent : ZombieNetworkComponent
{
    private Texture _dirtlessTexture;
    private Texture _poolTexture;
    internal override void OnInit()
    {
        _dirtlessTexture = ReplantedOnlineAssets.Sprites.Character.GravestoneDirtless.texture;
        _poolTexture = ReplantedOnlineAssets.Sprites.Character.GravestonePool.texture;
    }

    internal override void Update()
    {
        if (Net._Zombie.mController == null) return;

        if (Net._Zombie.mBoard.StageHasNoGrass())
        {
            Net._Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _dirtlessTexture;
        }

        if (Net._Zombie.mBoard.IsPoolSquare(Net.GridX, Net.GridY))
        {
            Net._Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _poolTexture;
            Net._Zombie.mController.m_shadowController.gameObject.SetActive(false);
            Net._Zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            Net._Zombie.mAltitude = -2;
        }

        Net._Zombie.mController.m_scale = new(1.15f, 1.15f);
        Net._Zombie.mController.m_visualOffset = new(125f, -335, 0f);
        Net._Zombie.mController.m_shadowController.m_shadowImageOffset = new(-175f, -50.5f, 0f);
        Net._Zombie.mController.m_shadowController.transform.localScale = new(1.15f, 1f, 1f);
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(Net._Zombie);
        Net._Zombie.mGraveX = 0;
        Net._Zombie.mGraveY = 0;
    }
}
