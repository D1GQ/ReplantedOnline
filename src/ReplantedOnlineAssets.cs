using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline;

/// <summary>
/// Provides access to custom assets including sprites and sounds for the Replanted Online.
/// </summary>
internal static class ReplantedOnlineAssets
{
    internal const string CUSTOM_ASSET_REF_GUID_PREFIX = "replant-online:";

    /// <summary>
    /// Contains all custom sprite assets.
    /// </summary>
    internal static class Sprites
    {
        internal static Sprite ModIcon
        {
            get
            {
                if (field == null)
                {
                    field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.PVZR-Online-Logo-BG.png");
                }

                return field;
            }
        }

        internal static Sprite PromoCompressed
        {
            get
            {
                if (field == null)
                {
                    field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.PVZR-Online-Promo-Logo-Compressed.png");
                }

                return field;
            }
        }

        /// <summary>
        /// Contains arena-related sprite assets.
        /// </summary>
        internal static class Arena
        {
            internal static Sprite RoofDayThumbnail
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Roofday.png");
                    }

                    return field;
                }
            }

            internal static Sprite RoofNightThumbnail
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Roofnight.png");
                    }

                    return field;
                }
            }

            internal static Sprite ChinaThumbnail
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.China.png");
                    }

                    return field;
                }
            }

            internal static Sprite RoofBowlingline
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.Roof-Overlay.png", 100f);
                    }

                    return field;
                }
            }

            internal static Sprite ChinaBowlingline
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.China-Overlay.png", 100f);
                    }

                    return field;
                }
            }

            internal static Sprite PoolBowlingline
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.Poolline.png", 100f);
                    }

                    return field;
                }
            }
        }

        /// <summary>
        /// Contains character-related sprite assets.
        /// </summary>
        internal static class Character
        {
            internal static Sprite JalapenoSleeping
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Characters.Jalapeno-Sleeping.png");
                    }

                    return field;
                }
            }

            internal static Sprite GravestoneDirtless
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Characters.Gravestone-Dirtless.png");
                    }

                    return field;
                }
            }
        }

        /// <summary>
        /// Contains seed packet-related sprite assets.
        /// </summary>
        internal static class SeedPacket
        {
            internal static Sprite HiddenSeedPacketIcon
            {
                get
                {
                    if (field == null)
                    {
                        field = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Icons.Hidden-Seedpacket.png");
                    }

                    return field;
                }
            }
        }
    }

    /// <summary>
    /// Contains all custom sound assets.
    /// </summary>
    internal static class Sounds
    {
        internal static AudioClip CrazyDaveMainThemeCompressed
        {
            get
            {
                if (field == null)
                {
                    field = ModInfo.Assembly.LoadWavFromResources("ReplantedOnline.Resources.Sounds.CrazyDaveMainTheme-Compressed.wav");
                    field.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                }

                return field;
            }
        }
    }
}
