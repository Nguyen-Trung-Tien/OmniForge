namespace OmniForge.Core.Models;

/// <summary>
/// Supported application languages for internationalization (i18n) and localization (l10n).
/// </summary>
public enum AppLanguage
{
    English,
    Vietnamese
}

/// <summary>
/// Strongly-typed metadata describing a supported language item.
/// </summary>
public sealed record LanguageItem(
    AppLanguage Language,
    string Bcp47Tag,
    string NativeName,
    string EnglishName);

