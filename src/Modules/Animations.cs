namespace ReplantedOnline.Modules;

/// <summary>
/// Provides a centralized collection of animation identifier constants used for character and action animations.
/// </summary>
internal static class Animations
{
    internal static readonly string GARGANTUAR_THROW = "anim_gargantuar_throw";

    internal static readonly (string Anim, float AnimRate) IMP_FLYING = ("anim_imp_flying", 18f);

    internal static readonly (string Anim, float Fps) CHOMPER_IDLE = ("idle", 10.26f);
    internal static readonly (string Anim, float Fps) CHOMPER_BITE = ("bite", 30f);
    internal static readonly (string Anim, float Fps) CHOMPER_CHEW = ("chew", 15f);

    internal static readonly (string Anim, float Fps) SQUASH_IDLE = ("idle", 10.0531f);
    internal static readonly (string Anim, float Fps) SQUASH_LOOKLEFT = ("lookleft", 24f);
    internal static readonly (string Anim, float Fps) SQUASH_LOOKRIGHT = ("lookright", 24f);
}
