using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gravestone)]
internal sealed class GravestoneNetworkComponent : ZombieNetworkComponent
{
    private Texture _dirtlessTexture;
    internal override void OnInit()
    {
        _dirtlessTexture = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Characters.Gravestone-Dirtless.png").texture;
    }

    internal override void Update()
    {
        // Do not sync position

        if (Net._Zombie.mBoard.StageHasNoGrass())
        {
            Net._Zombie.mController.m_materialEffectController.m_colorMaterial.mainTexture = _dirtlessTexture;
        }
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(Net._Zombie);
        Net._Zombie.mGraveX = 0;
        Net._Zombie.mGraveY = 0;
    }
}
