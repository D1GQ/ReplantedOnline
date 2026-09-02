using BloomEngine.Config;
using BloomEngine.Config.Inputs;
using BloomEngine.ModMenu;
using MelonLoader;
using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Patches.Steam;

namespace ReplantedOnline.Managers.Modded;

/// <summary>
/// Manages integration with BloomEngine.
/// </summary>
internal static class BloomEngineManager
{
    /// <summary>
    /// Initializes BloomEngine features.
    /// </summary>
    /// <param name="replantedOnline">The active MelonMod instance.</param>
    internal static void InitializeBloom(MelonMod replantedOnline)
    {
        BloomConfigs.Init();

        var mod = ModMenuService.CreateEntry(replantedOnline);
        mod.AddIcon(ReplantedOnlineMod.Assets.Sprites.ModIcon.Asset);
        mod.AddDisplayName("$MOD_CONFIG_NAME");
        mod.AddDescription("$MOD_CONFIG_DESCRIPTION");
        mod.AddConfigInputs(BloomConfigs.TransportModeConfig, BloomConfigs.AppServerConfig, BloomConfigs.ModifyMusicConfig);
        mod.Register();
    }

    /// <summary>
    /// Holds BloomEngine config fields and related initialization logic.
    /// </summary>
    internal static class BloomConfigs
    {
        internal static EnumConfigInput<TransportMode> TransportModeConfig = default!;
        internal static EnumConfigInput<AppIds> AppServerConfig = default!;
        internal static BoolConfigInput ModifyMusicConfig = default!;

        /// <summary>
        /// Initializes BloomEngine config fields and related event handlers.
        /// </summary>
        internal static void Init()
        {
            TransportModeConfig = ConfigService.CreateEnum(
                "$MOD_CONFIG_TRANSPORT_MODE",
                "$MOD_CONFIG_TRANSPORT_MODE_DESCRIPTION",
                TransportMode.Steam
            );
            TransportModeConfig.OnValueChanged += ReloadedLobby.SetTransportMode;

            AppServerConfig = ConfigService.CreateEnum(
                "$MOD_CONFIG_APP_SERVER",
                "$MOD_CONFIG_APP_SERVER_DESCRIPTION",
                AppIds.Replanted
            );
            AppServerConfig.OnValueChanged += SteamClientPatch.SetApp;

            ModifyMusicConfig = ConfigService.CreateBool(
                "$MOD_CONFIG_MODIFY_MUSIC",
                "$MOD_CONFIG_MODIFY_MUSIC_DESCRIPTION",
                true
            );
            ModifyMusicConfig.OnValueChanged += @bool =>
            {
                AudioManager.OnModifyMusic(@bool, true);
            };
        }
    }
}
