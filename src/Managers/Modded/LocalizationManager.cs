using Il2CppReloaded.Services;
using ReplantedOnline.Data.Json;
using ReplantedOnline.Modules.Modded.Instance;
using System.Text.Json;

namespace ReplantedOnline.Managers.Modded;

/// <summary>
/// Manages localization for the ReplantedOnline mod, loading language files from embedded resources
/// </summary>
internal static class LocalizationManager
{
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
        using var masterStream = ReplantedOnlineMod.ModInfo.Assembly
            .GetManifestResourceStream("ReplantedOnline.Resources.Localization.Localizations.json");
        if (masterStream != null)
        {
            using StreamReader reader = new(masterStream);
            string content = reader.ReadToEnd();
            LocalizationConfig config = new();
            config.Deserialize(content);

            foreach (var fileName in config.LanguageFiles)
            {
                ParseLocalizationFile(fileName);
            }
        }

        AddDollarPrefixToAllKeys();
    }

    /// <summary>
    /// Parses a single language file from embedded resources and adds its translations to <see cref="Localizations"/>.
    /// </summary>
    /// <param name="fileName">The name of the language file (e.g., "en.json"). Must be embedded in 
    /// "ReplantedOnline.Resources.Localization." namespace.</param>
    private static void ParseLocalizationFile(string fileName)
    {
        using var langStream = ReplantedOnlineMod.ModInfo.Assembly
            .GetManifestResourceStream($"ReplantedOnline.Resources.Localization.{fileName}");

        if (langStream != null)
        {
            using StreamReader langReader = new(langStream);
            string langContent = langReader.ReadToEnd();
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(langContent);

            if (translations != null)
            {
                if (translations.TryGetValue("Language", out var languageStr) && Enum.TryParse<Language>(languageStr, out var language))
                {
                    translations.Remove("Language");
                    Localizations[language] = translations;
                }
            }
        }
    }

    /// <summary>
    /// Adds a "$" prefix to every key in all loaded localization dictionaries.
    /// </summary>
    private static void AddDollarPrefixToAllKeys()
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
    /// <param name="key">The localization key to look up.</param>
    /// <param name="localization">When this method returns, contains the localized string if found;
    /// otherwise, an empty string.</param>
    /// <returns><c>true</c> if the localization was found; otherwise, <c>false</c>.</returns>
    internal static bool TryGetLocalization(string key, out string localization)
    {
        var currentLanguage = Instances.LocalizationActivity.m_settings.CurrentLanguage;

        if (Localizations.TryGetValue(currentLanguage, out var localizationMap))
        {
            if (localizationMap.TryGetValue(key, out localization!))
            {
                return true;
            }
        }

        if (currentLanguage != Language.English && Localizations.TryGetValue(Language.English, out var englishMap))
        {
            if (englishMap.TryGetValue(key, out localization!))
            {
                return true;
            }
        }

        localization = string.Empty;
        return false;
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