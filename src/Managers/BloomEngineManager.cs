using BloomEngine.Config;
using BloomEngine.Config.Inputs;
using BloomEngine.ModMenu;
using MelonLoader;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using System.Reflection;

namespace ReplantedOnline.Managers;

/// <summary>
/// Handles initialization and configuration bridging between
/// MelonPreferences and BloomEngine configuration menus.
/// </summary>
internal static class BloomEngineManager
{
    /// <summary>
    /// Initializes BloomEngine menu integration and registers
    /// the mod's configuration UI.
    /// </summary>
    /// <param name="replantedOnline">The active MelonMod instance.</param>
    internal static void InitializeBloom(MelonMod replantedOnline)
    {
        BloomConfigs.Init();

        var mod = ModMenuService.CreateEntry(replantedOnline);
        mod.AddIcon(Assembly.GetExecutingAssembly().LoadSpriteFromResources("ReplantedOnline.Resources.Images.PVZR-Online-Logo-BG.png"));
        mod.AddDisplayName(ModInfo.MOD_NAME);
        mod.AddDescription("Replanted Online is a mod that adds online support to versus!");
        mod.AddConfigInputs(BloomConfigs.UseLan, BloomConfigs.ModifyMusic);
        mod.Register();
    }

    /// <summary>
    /// BloomEngine-facing configuration definitions.
    /// </summary>
    internal static class BloomConfigs
    {
        internal static BoolConfigInput ModifyMusic;
        internal static BoolConfigInput UseLan;

        /// <summary>
        /// Initializes BloomEngine config fields and syncs values
        /// with MelonPreferences.
        /// </summary>
        internal static void Init()
        {
            var ModifyMusicConfig =
            ModifyMusic = ConfigService.CreateBool(
                "Modify Music",
                "Modifies music tracks.",
                true
            );
            ModifyMusic.OnValueChanged += @bool =>
            {
                AudioManager.OnModifyMusic(@bool, true);
            };

            UseLan = ConfigService.CreateBool(
                "(LAN) Mode",
                "Bypass Steam servers and connect directly via local network for Testing.",
                false
            );
            UseLan.OnValueChanged += @bool =>
            {
                if (@bool)
                {
                    NetLobby.SetTransportMode(1);
                }
                else
                {
                    NetLobby.SetTransportMode(0);
                }
            };
        }
    }
}
