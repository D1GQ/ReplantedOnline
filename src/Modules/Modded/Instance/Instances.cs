using Il2CppReloaded;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Services;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.DataModels;
using Il2CppTekly.PanelViews;

namespace ReplantedOnline.Modules.Modded.Instance;

/// <summary>
/// Provides centralized access to critical game instances used throughout ReplantedOnline.
/// </summary>
internal static class Instances
{
    internal static AppCore AppCore => GlobalInstance<AppCore>.Instance!;
    internal static GameplayDataProvider GameplayDataProvider => GlobalInstance<GameplayDataProvider>.Instance!;
    internal static GameplayActivity GameplayActivity => GlobalInstance<GameplayActivity>.Instance!;
    internal static LocalizationActivity LocalizationActivity { get; set; } = GlobalInstance<LocalizationActivity>.Instance;
    internal static IDataService IDataService => GlobalInstance<DataServiceActivity>.Instance.Service!;
    internal static VersusDataModel VersusDataModel => GlobalInstance<VersusDataModel>.Instance!;
    internal static PanelViewContainer GlobalPanels { get; set; } = default!;
}