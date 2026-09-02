using Il2CppReloaded.Services;
using ReplantedOnline.Data.Asset.Resource;
using ReplantedOnline.Data.Json;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Unity;

namespace ReplantedOnline.Managers.Modded;

/// <summary>
/// Manages localization for the ReplantedOnline mod, loading language files from embedded resources
/// </summary>
internal static class LocalizationManager
{
    /// <summary>
    /// The JSON resource asset for the master localization configuration.
    /// </summary>
    private static readonly JsonResourceAsset<LocalizationConfig> LocalizationAsset = new("ReplantedOnline.Resources.Localization.Localizations.json");

    /// <summary>
    /// Stores all loaded localizations keyed by <see cref="Language"/>.
    /// </summary>
    internal static Dictionary<Language, Dictionary<string, string>> Localizations = [];

    /// <summary>
    /// Initializes the localization system by reading the master localization configuration
    /// and loading all referenced language files.
    /// </summary>
    internal static void Initialize()
    {
        LocalizationAsset.Load();

        if (LocalizationAsset.Loadded)
        {
            var config = LocalizationAsset.Asset;
            foreach (var fileName in config.LanguageFiles)
            {
                ParseLocalizationFile(fileName);
            }
        }

        AddPrefixToAllKeys();
    }

    /// <summary>
    /// Parses a single language file from embedded resources and adds its translations to <see cref="Localizations"/>.
    /// </summary>
    /// <param name="fileName">The name of the language file.</param>
    private static void ParseLocalizationFile(string fileName)
    {
        var langAsset = new JsonResourceAsset<Dictionary<string, string>>($"ReplantedOnline.Resources.Localization.{fileName}");
        langAsset.Load();

        if (langAsset.Loadded)
        {
            var translations = langAsset.Asset;
            if (translations != null)
            {
                if (translations.TryGetValue("LANGUAGE", out var languageStr) && Enum.TryParse<Language>(languageStr, out var language))
                {
                    translations.Remove("LANGUAGE");
                    Localizations[language] = translations;
                }
            }
        }
    }

    /// <summary>
    /// Adds a "$" prefix to every key in all loaded localization dictionaries.
    /// </summary>
    private static void AddPrefixToAllKeys()
    {
        var languages = Localizations.Keys.ToList();

        foreach (var language in languages)
        {
            var originalMap = Localizations[language];
            var newMap = new Dictionary<string, string>();

            foreach (var kvp in originalMap)
            {
                newMap[$"${kvp.Key}"] = kvp.Value;
            }

            Localizations[language] = newMap;
        }
    }

    /// <summary>
    /// Attempts to retrieve a localized string for the current game language.
    /// </summary>
    /// <param name="lid">The localization key to look up.</param>
    /// <param name="localization">When this method returns, contains the localized string if found;
    /// otherwise, an empty string.</param>
    /// <returns><c>true</c> if the localization was found; otherwise, <c>false</c>.</returns>
    internal static bool TryGetLocalization(string lid, out string localization)
    {
        localization = GetLocalization(lid);
        return !string.IsNullOrEmpty(localization) && localization != lid;
    }

    /// <summary>
    /// Retrieves a localized string for the current game language.
    /// </summary>
    /// <param name="lid">The localization id to look up.</param>
    /// <returns>The localized string if found; otherwise, returns the original id.</returns>
    internal static string GetLocalization(string lid)
    {
        if (string.IsNullOrEmpty(lid))
        {
            return lid;
        }

        if (Instances.LocalizationActivity == null)
        {
            return lid;
        }

        var currentLanguage = Instances.LocalizationActivity.m_settings.CurrentLanguage;

        if (Localizations.TryGetValue(currentLanguage, out var localizationMap))
        {
            if (localizationMap.TryGetValue(lid, out var localization))
            {
                return localization;
            }
        }

        if (currentLanguage != Language.English && Localizations.TryGetValue(Language.English, out var englishMap))
        {
            if (englishMap.TryGetValue(lid, out var localizationForce))
            {
                return localizationForce;
            }
        }

        // Can not use Localizer.Instance due to it being a generic type class that enforces a interface,
        // the il2cpp type does not play well in managed runtime,
        // so instead use this janky workaround to be able to localize text from the game.
        // This is due to interfaces being treated as a class in il2cpp, and the generic type being a class, so it does not match the interface type.
        GlobalGameObjects.Localizer.m_Id = lid;
        GlobalGameObjects.Localizer.LocalizeText();
        string gameLocalization = GlobalGameObjects.Localizer.m_format;
        if (!string.IsNullOrEmpty(gameLocalization))
        {
            return gameLocalization;
        }

        return lid;
    }

    /// <summary>
    /// Retrieves a localized string for the current game language and formats it with the specified arguments.
    /// </summary>
    /// <param name="lid">The localization id to look up.</param>
    /// <param name="format">An array of objects to format the localized string with.</param>
    /// <returns>
    /// The formatted localized string if found; otherwise, returns the original id.
    /// </returns>
    internal static string GetLocalizationFormatted(string lid, params string[] format)
    {
        var localization = GetLocalization(lid);
        return localization == lid ? lid : string.Format(localization, format);
    }

    /// <summary>
    /// Represents the master localization configuration that lists all language files to load.
    /// </summary>
    private class LocalizationConfig : JsonObject
    {
        /// <summary>
        /// Gets or sets the list of language file names.
        /// </summary>
        public List<string> LanguageFiles { get; set; } = [];
    }
}