using ReplantedOnline.Data.Asset.Resource;

namespace ReplantedOnline;

/// <inheritdoc/>
internal partial class ReplantedOnlineMod
{
    /// <summary>
    /// Provides access to custom assets including sprites and sounds for Replanted Online.
    /// </summary>
    internal static class Assets
    {
        /// <summary>
        /// Preloads all asset resources to ensure they are loaded into memory before they are needed.
        /// </summary>
        internal static void PreloadAssets()
        {
            // Load all sprite assets
            Sprites.ModIcon.Load();
            Sprites.PromoCompressed.Load();
            Sprites.TimerPanel.Load();

            // Arena sprites
            Sprites.Arena.RoofDayThumbnail.Load();
            Sprites.Arena.RoofNightThumbnail.Load();
            Sprites.Arena.ChinaThumbnail.Load();
            Sprites.Arena.RoofBowlingline.Load();
            Sprites.Arena.ChinaBowlingline.Load();
            Sprites.Arena.PoolBowlingline.Load();

            // Character sprites
            Sprites.Character.JalapenoSleeping.Load();
            Sprites.Character.GravestoneDirtless.Load();
            Sprites.Character.GravestonePool.Load();

            // Seed packet sprites
            Sprites.SeedPacket.HiddenSeedPacketIcon.Load();
            Sprites.SeedPacket.DolphinriderSeedPacketIcon.Load();
            Sprites.SeedPacket.SnorkelSeedPacketIcon.Load();
            Sprites.SeedPacket.BackupDancerSeedPacketIcon.Load();
            Sprites.SeedPacket.YetiSeedPacketIcon.Load();

            // White Water sprite sheet
            Sprites.WhiteWaterSpriteSheet.Load();

            // Sound assets
            Sounds.CrazyDaveMainThemeCompressed.Load();
        }

        /// <summary>
        /// Contains all custom sprite assets.
        /// </summary>
        internal static class Sprites
        {
            /// <summary>
            /// Gets the mod icon sprite asset.
            /// </summary>
            internal static SpriteResourceAsset ModIcon { get; } = new("ReplantedOnline.Resources.Images.PVZR-Online-Logo-BG.png");

            /// <summary>
            /// Gets the promo logo sprite asset (compressed version).
            /// </summary>
            internal static SpriteResourceAsset PromoCompressed { get; } = new("ReplantedOnline.Resources.Images.PVZR-Online-Promo-Logo-Compressed.png");

            /// <summary>
            /// Gets the timer panel sprite asset.
            /// </summary>
            internal static SpriteResourceAsset TimerPanel { get; } = new("ReplantedOnline.Resources.Images.Timer-Panel.png");

            /// <summary>
            /// Contains arena-related sprite assets.
            /// </summary>
            internal static class Arena
            {
                /// <summary>
                /// Gets the roof day arena thumbnail sprite asset.
                /// </summary>
                internal static SpriteResourceAsset RoofDayThumbnail { get; } = new("ReplantedOnline.Resources.Images.Arenas.Roofday.png");

                /// <summary>
                /// Gets the roof night arena thumbnail sprite asset.
                /// </summary>
                internal static SpriteResourceAsset RoofNightThumbnail { get; } = new("ReplantedOnline.Resources.Images.Arenas.Roofnight.png");

                /// <summary>
                /// Gets the China arena thumbnail sprite asset.
                /// </summary>
                internal static SpriteResourceAsset ChinaThumbnail { get; } = new("ReplantedOnline.Resources.Images.Arenas.China.png");

                /// <summary>
                /// Gets the roof bowling line overlay sprite asset.
                /// </summary>
                internal static SpriteResourceAsset RoofBowlingline { get; } = new("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.Roof-Overlay.png", 100f);

                /// <summary>
                /// Gets the China bowling line overlay sprite asset.
                /// </summary>
                internal static SpriteResourceAsset ChinaBowlingline { get; } = new("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.China-Overlay.png", 100f);

                /// <summary>
                /// Gets the pool bowling line overlay sprite asset.
                /// </summary>
                internal static SpriteResourceAsset PoolBowlingline { get; } = new("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.Poolline.png", 100f);
            }

            /// <summary>
            /// Contains character-related sprite assets.
            /// </summary>
            internal static class Character
            {
                /// <summary>
                /// Gets the sleeping Jalapeno character sprite asset.
                /// </summary>
                internal static SpriteResourceAsset JalapenoSleeping { get; } = new("ReplantedOnline.Resources.Images.Characters.Jalapeno-Sleeping.png");

                /// <summary>
                /// Gets the dirtless gravestone character sprite asset.
                /// </summary>
                internal static SpriteResourceAsset GravestoneDirtless { get; } = new("ReplantedOnline.Resources.Images.Characters.Gravestone-Dirtless.png");

                /// <summary>
                /// Gets the pool gravestone character sprite asset.
                /// </summary>
                internal static SpriteResourceAsset GravestonePool { get; } = new("ReplantedOnline.Resources.Images.Characters.Gravestone-Pool.png");
            }

            /// <summary>
            /// Contains seed packet-related sprite assets.
            /// </summary>
            internal static class SeedPacket
            {
                /// <summary>
                /// Gets the hidden seed packet icon sprite asset.
                /// </summary>
                internal static SpriteResourceAsset HiddenSeedPacketIcon { get; } = new("ReplantedOnline.Resources.Images.Icons.Hidden-Seedpacket.png");

                /// <summary>
                /// Gets the dolphin rider seed packet icon sprite asset.
                /// </summary>
                internal static SpriteResourceAsset DolphinriderSeedPacketIcon { get; } = new("ReplantedOnline.Resources.Images.Icons.Dolphinrider-Seedpacket.png");

                /// <summary>
                /// Gets the snorkel seed packet icon sprite asset.
                /// </summary>
                internal static SpriteResourceAsset SnorkelSeedPacketIcon { get; } = new("ReplantedOnline.Resources.Images.Icons.Snorkel-Seedpacket.png");

                /// <summary>
                /// Gets the backup dancer seed packet icon sprite asset.
                /// </summary>
                internal static SpriteResourceAsset BackupDancerSeedPacketIcon { get; } = new("ReplantedOnline.Resources.Images.Icons.BackupDancer-Seedpacket.png");

                /// <summary>
                /// Gets the yeti seed packet icon sprite asset.
                /// </summary>
                internal static SpriteResourceAsset YetiSeedPacketIcon { get; } = new("ReplantedOnline.Resources.Images.Icons.Yeti-Seedpacket.png");
            }

            /// <summary>
            /// Gets the White Water sprite sheet asset.
            /// </summary>
            internal static SpriteSheetResourceAsset WhiteWaterSpriteSheet { get; } = new("ReplantedOnline.Resources.Images.Characters.White-Water.png", 3, 1);
        }

        /// <summary>
        /// Contains all custom sound assets.
        /// </summary>
        internal static class Sounds
        {
            /// <summary>
            /// Gets the compressed Crazy Dave main theme audio asset.
            /// </summary>
            internal static AudioClipResourceAsset CrazyDaveMainThemeCompressed { get; } = new("ReplantedOnline.Resources.Sounds.CrazyDaveMainTheme-Compressed.wav");
        }
    }
}