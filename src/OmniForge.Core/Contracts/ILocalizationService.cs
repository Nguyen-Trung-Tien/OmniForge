using OmniForge.Core.Models;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Provides application-wide localization, dynamic hot-reload language switching,
/// and type-safe string formatting without requiring application restarts.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current active application language.
    /// </summary>
    AppLanguage CurrentLanguage { get; }

    /// <summary>
    /// Gets the BCP-47 language tag for the current language (e.g. "en-US", "vi-VN").
    /// </summary>
    string CurrentCultureTag { get; }

    /// <summary>
    /// Gets the list of officially supported languages.
    /// </summary>
    IReadOnlyList<LanguageItem> SupportedLanguages { get; }

    /// <summary>
    /// Resolves a localized string for the specified key with automatic fallback to English and key name.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Resolves a localized string and formats it with supplied parameters using the active culture.
    /// </summary>
    string GetFormattedString(string key, params object[] args);

    /// <summary>
    /// Changes the active language dynamically, triggers UI hot-reload, and persists the user preference.
    /// </summary>
    Task SetLanguageAsync(AppLanguage language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event triggered when the active language changes.
    /// </summary>
    event EventHandler<AppLanguage>? LanguageChanged;
}

