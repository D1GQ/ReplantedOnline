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

    internal sealed override void OnInit()
    {
        if (Net.Zombie?.mController == null)
            return;

        _originalTexture = Net.Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture;
        _dirtlessTexture = ReplantedOnlineMod.Assets.Sprites.Character.GravestoneDirtless.texture;
        _poolTexture = ReplantedOnlineMod.Assets.Sprites.Character.GravestonePool.texture;
    }

    internal override void OnEnabled()
    {
        if (Net.Zombie?.mController == null)
            return;

        if (Net.SpawnType == SpawnType.RiseFromPool)
        {
            var theX = Instances.GameplayActivity.Board.GridToPixelX(Net.GridX, Net.GridY);
            Net.Zombie.mPosX = theX - 25;
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

    internal sealed override void Update()
    {
        if (Net.Zombie?.mController == null)
            return;

        Net.Zombie.mGroanCounter = int.MaxValue;

        if (Net.Zombie.mBoard.StageHasNoGrass())
        {
            Net.Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _dirtlessTexture;
        }

        if (Net.Zombie.mBoard.IsPoolSquare(Net.GridX, Net.GridY))
        {
            Net.Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _poolTexture;
            Net.Zombie.mController.m_shadowController.gameObject.SetActive(false);
            Net.Zombie.mController.ClipRect(new(-500, -500, 1000, 615));
            Net.Zombie.mAltitude = -2;
        }

        Net.Zombie.mController.m_scale = new(1.15f, 1.15f);
        Net.Zombie.mController.m_visualOffset = new(125f, -335, 0f);
        Net.Zombie.mController.m_shadowController.m_shadowImageOffset = new(-175f, -50.5f, 0f);
        Net.Zombie.mController.m_shadowController.transform.localScale = new(1.15f, 1f, 1f);
    }

    internal sealed override void OnDeath(DeathReason deathReason)
    {
        if (Net.Zombie?.mController == null)
            return;

        Net.Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _originalTexture;
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(Net.Zombie);
        Net.Zombie.mGraveX = 0;
        Net.Zombie.mGraveY = 0;
    }
}
