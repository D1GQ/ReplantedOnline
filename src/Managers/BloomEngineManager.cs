using BloomEngine.Config;
using BloomEngine.Config.Inputs;
using BloomEngine.Menu;
using MelonLoader;
using ReplantedOnline.Enums;

namespace ReplantedOnline.Managers;

/// <summary>
/// Handles initialization and configuration bridging between
/// MelonPreferences and BloomEngine configuration menus.
/// </summary>
internal static class BloomEngineManager
{
    private static MelonPreferences_Category m_configCategory;
    internal static MelonPreferences_Entry<uint> m_gameServer;

    /// <summary>
    /// Ensures MelonPreferences are only initialized once.
    /// </summary>
    private static bool m_hasInit;

    /// <summary>
    /// Initializes MelonPreferences entries and validates stored values.
    /// Safe to call multiple times.
    /// </summary>
    internal static void InitMelon()
    {
        if (m_hasInit) return;
        m_hasInit = true;

        m_configCategory = MelonPreferences.CreateCategory(
            ModInfo.MOD_NAME.Replace(" ", ""),
            "configs"
        );

        m_gameServer = m_configCategory.CreateEntry(
            "GameServerId",
            (uint)AppIdServers.PVZ_Replanted,
            "Steam Game ID Server",
            "The Steam App ID to connect to for P2P"
        );

        // Reset to default if an invalid enum value was stored
        if (!Enum.GetValues<AppIdServers>().Contains((AppIdServers)m_gameServer.Value))
        {
            m_gameServer.Value = (uint)AppIdServers.PVZ_Replanted;
        }
    }

    /// <summary>
    /// Initializes BloomEngine menu integration and registers
    /// the mod's configuration UI.
    /// </summary>
    /// <param name="replantedOnline">The active MelonMod instance.</param>
    internal static void InitBloom(MelonMod replantedOnline)
    {
        InitMelon();
        BloomConfigs.Init();

        var mod = ModMenu.CreateEntry(replantedOnline);
        mod.AddDisplayName(ModInfo.MOD_NAME);
        mod.AddDescription("PVZR Online is a mod that adds online support to versus!");
        mod.AddConfig(typeof(BloomConfigs));
        mod.Register();
    }

    /// <summary>
    /// BloomEngine-facing configuration definitions.
    /// </summary>
    internal static class BloomConfigs
    {
        internal static EnumInputField GameServer;

        /// <summary>
        /// Initializes BloomEngine config fields and syncs values
        /// with MelonPreferences.
        /// </summary>
        internal static void Init()
        {
            MelonPreferences.Save();

            GameServer = ConfigMenu.CreateEnumInput(
                "Game Server (Restart Required)",
                (AppIdServers)m_gameServer.Value,
                value =>
                {
                    if (value is AppIdServers appIdServer)
                    {
                        m_gameServer.Value = (uint)appIdServer;
                        MelonPreferences.Save();
                    }
                },
                validateValue: value =>
                {
                    return Enum.GetValues<AppIdServers>()
                        .Contains((AppIdServers)value);
                }
            );
        }
    }
}
