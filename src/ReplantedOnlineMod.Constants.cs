using ReplantedOnline.Structs;

namespace ReplantedOnline;

/// <inheritdoc/>
internal partial class ReplantedOnlineMod
{
    /// <summary>
    /// Provides constant values used throughout Replanted Online.
    /// </summary>
    internal static class Constants
    {
        internal static class Assets
        {
            internal const string CUSTOM_ASSET_REF_GUID_PREFIX = "replant-online:";
        }

        internal static class Network
        {
            internal const int MAX_NETWORK_CHILDREN = 5;
            internal const string MOD_VERSION_KEY = "mod_version";
            internal const string GAME_CODE_KEY = "game_code";

            internal static class ObjectStates
            {
                internal const string NULL_STATE = "null";
                internal const string UPDATE_STATE = "update";

                internal const string ZOMBIE_MIND_CONTROLLED_STATE = "mind_controlled";
                internal const string ZOMBIE_CHEWING_PLANT_STATE = "chewing_plant";
                internal const string ZOMBIE_MOWED_DOWN_STATE = "mowed_down";

                internal const string GARGANTUAR_SMASH_STATE = "gargantuar_smash";
                internal const string GARGANTUAR_TARGET_STATE = "gargantuar_target";

                internal const string LADDER_ZOMBIE_PLACED_LADDER = "placed_ladder";

                internal const string CHOMPER_BITE_STATE = "chomper_bite";
            }
        }

        internal static class Reloaded
        {
            internal const int DEFAULT_PLAYER_INDEX = -1;
            internal const int LOCAL_PLAYER_INDEX = 0;
            internal const int OPPONENT_PLAYER_INDEX = 1;
            internal const float X2_SEEDPACKET_COOLDOWN_TIME_END = 60f;
            internal const string REDIRECT_ALMANAC_PREFIX = "ALMANAC-REDIRECT:";

            internal static class Production
            {
                internal static readonly IntTime INITIAL_SKY_RATE = 12f;
                internal static readonly (IntTime MIN, IntTime MAX) INITIAL_PLANT_OR_GRAVE_RATE_RANGE = (8f, 12f);
                internal static readonly IntTime SKY_RATE = 22f;
                internal static readonly IntTime PLANT_RATE = 14f;
                internal static readonly IntTime GRAVE_RATE = 34f;
            }
        }
    }
}
