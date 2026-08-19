// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.Collation.Locale;

/// <summary>
/// The static configuration of the locale-selection system (kb/Work PB101, section D): which locales are selectable,
/// which one is the default, and where tailoring files are looked for. Nothing here is a hand-kept list — the
/// supported set is DERIVED from what exists (the tailorings embedded in this assembly, the <c>.tailor</c> files in
/// the searched directories, and every culture .NET recognizes), and the default is the run unit's L2 user default
/// (owner decision Q2: <c>COBOL_USER_LOCALE</c>, else the process culture, else the root).
/// </summary>
public static class LocaleConfig
{
    /// <summary>The environment variable naming the default locale (owner decision Q2; <see cref="LocaleState.UserDefaultVariable"/>).</summary>
    public const string DefaultLocaleVariable = LocaleState.UserDefaultVariable;

    /// <summary>The environment variable naming a directory of site tailoring files (<see cref="TailoringRules"/>).</summary>
    public const string TailoringDirectoryVariable = "COBOL_COLLATION_DIR";

    /// <summary>The root locale's tag — the untailored CLDR root order.</summary>
    public const string Root = "";

    /// <summary>The default locale: the L2 user default as the run unit determines it at activation —
    /// <c>COBOL_USER_LOCALE</c> when set (trimmed; blank = root), else <see cref="CultureInfo.CurrentCulture"/>'s
    /// name, else the root. Read afresh on each call so a host that changes the variable before creating a run unit
    /// sees the change; the run unit itself reads it ONCE (determination L3).</summary>
    public static string DefaultLocale =>
        LocaleState.Determine(LocaleState.UserDefaultVariable, () => CultureInfo.CurrentCulture);

    /// <summary>The locales that carry TAILORING rules: the tailorings embedded in this assembly plus every
    /// <c>.tailor</c> file in the searched directories (<c>COBOL_COLLATION_DIR</c>, <c>Collation/</c> and
    /// <c>Collation/Locales/</c> beside the application). Sorted, distinct, hyphen-normalized.</summary>
    public static IReadOnlyList<string> TailoredLocales =>
        TailoringRules.EmbeddedNames()
            .Concat(TailoringRules.SearchDirectories().SelectMany(TailoringRules.TailoringsIn))
            .Select(n => n.Replace('_', '-'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>The locales <see cref="LocaleManager.SetLocale"/> accepts: the root, every tailored locale, and every
    /// culture .NET recognizes (which collates by the root order unless a tailoring exists for it or its language).
    /// The recognized-culture set is large; this lists the root and the tailored locales explicitly and describes the
    /// rest through <see cref="IsSupported"/>.</summary>
    public static IReadOnlyList<string> SupportedLocales => new[] { Root }.Concat(TailoredLocales).ToArray();

    /// <summary>Is <paramref name="localeName"/> selectable? — the root, a tailored locale (exact tag or its language),
    /// or a culture .NET recognizes ("de-DE", "ja", …). Case-insensitive; "_" and "-" interchangeable.</summary>
    public static bool IsSupported(string? localeName)
    {
        if (localeName is null) return false;
        string tag = localeName.Trim().Replace('_', '-');
        if (tag.Length == 0 || tag.Equals("root", StringComparison.OrdinalIgnoreCase)) return true;
        if (TailoringRules.ForLocale(tag) is not null) return true;
        return IsKnownCulture(tag);
    }

    /// <summary>True when .NET's culture data knows the tag (predefined cultures only — never a synthesized one).</summary>
    public static bool IsKnownCulture(string tag)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
