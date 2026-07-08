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
        mod.AddIcon(ReplantedOnlineMod.Assets.Sprites.ModIcon);
        mod.AddDisplayName(ReplantedOnlineMod.ModInfo.MOD_NAME);
        mod.AddDescription("Replanted Online is a mod that adds online support to versus!");
        mod.AddConfigInputs(BloomConfigs.TransportModeConfig, BloomConfigs.AppServerConfig, BloomConfigs.ModifyMusicConfig);
        mod.Register();
    }

    /// <summary>
    /// Holds BloomEngine config fields and related initialization logic.
    /// </summary>
    internal static class BloomConfigs
    {
        internal static BoolConfigInput ModifyMusicConfig = default!;
        internal static EnumConfigInput<TransportMode> TransportModeConfig = default!;
        internal static EnumConfigInput<AppIds> AppServerConfig = default!;

        /// <summary>
        /// Initializes BloomEngine config fields and related event handlers.
        /// </summary>
        internal static void Init()
        {
            TransportModeConfig = ConfigService.CreateEnum(
                "Transport Mode",
                "Choose what network transport to use when hosting a lobby.",
                TransportMode.Steam
            );
            TransportModeConfig.OnValueChanged += ReloadedLobby.SetTransportMode;

            AppServerConfig = ConfigService.CreateEnum(
                "App Server",
                "Choose what app id the steam matchmaking uses.",
                AppIds.Replanted
            );
            AppServerConfig.OnValueChanged += SteamClientPatch.SetApp;

            ModifyMusicConfig = ConfigService.CreateBool(
                "Modify Music",
                "Modifies music tracks.",
                true
            );
            ModifyMusicConfig.OnValueChanged += @bool =>
            {
                AudioManager.OnModifyMusic(@bool, true);
            };
        }
    }
}
