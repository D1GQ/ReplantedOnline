using Il2CppReloaded.DataModels;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.DataModels;
using Il2CppTekly.PanelViews;

namespace ReplantedOnline.Modules;

/// <summary>
/// Provides centralized access to critical game instances used throughout ReplantedOnline.
/// </summary>
internal static class Instances
{
    internal static GameplayDataProvider GameplayDataProvider => InstanceWrapper<GameplayDataProvider>.Instance ?? default;
    internal static GameplayActivity GameplayActivity => InstanceWrapper<GameplayActivity>.Instance ?? default;
    internal static VersusDataModel VersusDataModel => InstanceWrapper<VersusDataModel>.Instance ?? default;
    internal static PanelViewContainer GlobalPanels { get; set; }
}