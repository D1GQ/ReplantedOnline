using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gravestone)]
internal sealed class GravestoneNetworkComponent : ZombieNetworkComponent
{
    private Texture _originalTexture = default!;
    private Texture _dirtlessTexture = default!;
    private Texture _poolTexture = default!;
    private WhiteWaterEffect? _whiteWaterEffect = null;

    internal sealed override void OnInit(Zombie zombie)
    {
        if (zombie.mController == null)
            return;

        _originalTexture = zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture;
        _dirtlessTexture = ReplantedOnlineMod.Assets.Sprites.Character.GravestoneDirtless.texture;
        _poolTexture = ReplantedOnlineMod.Assets.Sprites.Character.GravestonePool.texture;
    }

    internal override void OnEnabled()
    {
        if (Net.Zombie?.mController == null)
            return;

        if (Net.SpawnType == SpawnType.RiseFromPool)
        {
            Net.Zombie.mPosX = Net.BoardUnitX.Pos + 5;
            Net.Zombie.PoolSplash(true);
            _whiteWaterEffect = WhiteWaterEffect.Create(Net.Zombie.mController, false);
            _whiteWaterEffect.transform.localPosition = new(15f, -5f, 0f);
            _whiteWaterEffect.transform.localScale = new(0.8f, 1f, 1f);
            Net.StartCoroutine(CoEnableWhiteWaterEffect());
        }
    }

    internal override void OnDestroyed()
    {
        if (_whiteWaterEffect != null)
        {
            UnityEngine.Object.Destroy(_whiteWaterEffect.gameObject);
        }
    }

    private IEnumerator CoEnableWhiteWaterEffect()
    {
        while (Net.Zombie?.mZombiePhase == ZombiePhase.RisingFromGrave)
        {
            yield return null;
        }

        if (_whiteWaterEffect != null)
        {
            _whiteWaterEffect.gameObject.SetActive(true);
        }
    }

    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mBoard.StageHasNoGrass())
        {
            zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _dirtlessTexture;
        }

        if (zombie.mBoard.IsPoolSquare(Net.BoardUnitX.Grid, Net.BoardUnitY.Grid))
        {
            zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _poolTexture;
            zombie.mController.m_shadowController.gameObject.SetActive(false);
            zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            zombie.mAltitude = -2;
        }

        zombie.mController.m_scale = new(1.15f, 1.15f);
        zombie.mController.m_visualOffset = new(125f, -335, 0f);
        zombie.mController.m_shadowController.m_shadowImageOffset = new(-175f, -50.5f, 0f);
        zombie.mController.m_shadowController.transform.localScale = new(1.15f, 1f, 1f);
    }

    internal sealed override void OnDeath(Zombie? zombie, DeathReason deathReason)
    {
        if (zombie == null)
            return;

        zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _originalTexture;
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(Net.Zombie);
        zombie.mGraveX = 0;
        zombie.mGraveY = 0;
    }
}
