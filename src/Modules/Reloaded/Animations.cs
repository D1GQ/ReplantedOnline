using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded;

/// <summary>
/// Provides a centralized collection of animation identifier constants used for character and action animations.
/// </summary>
internal static class Animations
{
    internal static readonly string GARGANTUAR_THROW = "anim_gargantuar_throw";

    internal static readonly (string Anim, float AnimRate) IMP_FLYING = ("anim_imp_flying", 18f);

    internal static readonly (string Anim, int Blend, float Fps) CATAPULT_WALK = ("anim_walk", 20, 6f);
    internal static readonly (string Anim, int Blend, float Fps) CATAPULT_SHOOT = ("anim_shoot", 20, 24f);
    internal static readonly (string Anim, int Blend, float Fps) CATAPULT_IDLE = ("anim_idle", 20, 12f);
    internal static readonly (string Slot, string Image) CATAPULT_POLE_OBJECT = ("Zombie_catapult_pole", "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE");

    internal static readonly (string Anim, int Blend, float Fps) POLEVAULTER_VAULT = ("anim_jump", 3, 20f);

    internal static readonly (string Anim, int Blend, float Fps) DOLPHINRIDER_JUMP = ("anim_dolphinjump", 3, 10f);

    internal static readonly (string Anim, float Fps) CHOMPER_IDLE = ("idle", 10.26f);
    internal static readonly (string Anim, float Fps) CHOMPER_BITE = ("bite", 30f);
    internal static readonly (string Anim, float Fps) CHOMPER_CHEW = ("chew", 15f);

    internal static readonly (string Anim, float Fps) SQUASH_IDLE = ("idle", 10.0531f);
    internal static readonly (string Anim, float Fps) SQUASH_LOOKLEFT = ("lookleft", 24f);
    internal static readonly (string Anim, float Fps) SQUASH_LOOKRIGHT = ("lookright", 24f);

    internal static readonly string KERNELPULT_BUTTER_OBJECT = "Cornpult_butter";
    internal static readonly string KERNELPULT_KERNAL_OBJECT = "Cornpult_kernal";

    internal static readonly (string Anim, float Fps) MOWER_IDLE = ("normal", 0f);
    internal static readonly (string Anim, float Fps) SUPER_MOWER_IDLE = (string.Empty, 0f);
    internal static readonly (string Anim, float Fps) ROOF_MOWER_IDLE = ("default", 0f);
    internal static readonly (string Anim, float Fps) POOL_MOWER_IDLE = ("land", 0f);

    internal static readonly (string Slot, string Image) WHITEWATER_OBJECT = ("whitewater", string.Empty);

    /// <summary>
    /// Initiates a "fall from sky" animation for a zombie at the specified grid position.
    /// </summary>
    /// <param name="zombie">The zombie instance to animate.</param>
    /// <param name="gridY">The vertical grid coordinate that determines fall height and swing pitch.</param>
    internal static void PlayFallFromSky(Zombie zombie, int gridY)
    {
        zombie.mController.StartCoroutine(CoPlayFallFromSky(zombie, gridY));
    }

    private static IEnumerator CoPlayFallFromSky(Zombie zombie, int gridY)
    {
        if (VersusState.IsInCountDown) yield break;

        Instances.GameplayActivity.m_audioService.PlayFoleyPitch(FoleyType.Swing, Mathf.Lerp(-25f, -15, Mathf.Clamp01(gridY / 5)));
        float startAltitude = zombie.mAltitude;

        zombie.mController.m_shadowController.m_spriteRenderer.gameObject.SetActive(false);
        zombie.mZombieRect = zombie.mZombieRect.AsNonInteractable();
        zombie.mZombieAttackRect = zombie.mZombieAttackRect.AsNonInteractable();
        zombie.mAltitude = 300 + 100 * gridY;
        while (zombie.mAltitude > startAltitude)
        {
            zombie.mAltitude -= 1500f * Time.deltaTime;
            yield return null;
        }
        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_VASE_BREAKING);
        zombie.mZombieRect = zombie.mZombieRect.AsInteractable();
        zombie.mZombieAttackRect = zombie.mZombieAttackRect.AsInteractable();
        zombie.mAltitude = startAltitude;
        zombie.mController.m_shadowController.m_spriteRenderer.gameObject.SetActive(true);
    }
}
