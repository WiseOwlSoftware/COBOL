// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Globalization;

namespace CobolNet.Runtime.Collation.Locale;

/// <summary>
/// The locale SELECTION system of the collation subsystem (kb/Work PB101, section D): select a locale by tag, load its
/// tailoring rules, and make the engine collate by them — for the run unit that is current.
/// <para><b>Where the state lives.</b> There is exactly ONE current-locale state in COBOL.NET: the run unit's
/// <see cref="LocaleState"/> (<c>RunUnit.Current.Locale</c>, ISO/IEC 1989:2023 §8.2.1 / §14.6.6 — DESIGN-locale-facility
/// §4.3). <see cref="SetLocale"/> writes it (every category, the <c>SET LOCALE LC_ALL</c> shape) and
/// <see cref="CurrentLocale"/> reads its LC_COLLATE category; the LOCALE-based collating sequence
/// (<see cref="LocaleCollation.Current"/> — an <c>ALPHABET … IS LOCALE</c> program collating sequence, a SORT/MERGE or file
/// key sequence) resolves that same state at each use, so a selection made here is what a running COBOL program's
/// locale-based comparisons see. This class adds no second store.</para>
/// <para><b>How a tailoring is applied.</b> Tables are immutable: selecting a locale never mutates the root table.
/// <see cref="TailoringRules.ForLocale"/> finds the rules (a <c>.tailor</c> file in <c>COBOL_COLLATION_DIR</c>, in
/// <c>Collation/</c> or <c>Collation/Locales/</c> beside the application, or one embedded in this assembly — exact tag,
/// then language), <see cref="CollationEngine.TableForLocale"/> applies them once (<c>CollationTable.WithTailoring</c>) and
/// caches the resulting table, and <see cref="CollationEngine.ForLocale"/> hands out the cached collator. That is the
/// repository's form of "apply the tailoring to the collation table": a derived table per locale, shared and cached,
/// never a global mutation — a run unit may use several locales at once (a SORT under one, a file under another).</para>
/// <para><b>Validation.</b> A tag is accepted when it is the root, has tailoring rules (its own or its language's), or
/// is a culture .NET recognizes (it then collates by the root order — the CLDR order for English, French, German …);
/// anything else is a <see cref="CultureNotFoundException"/>. Tags are normalized to the hyphen form.</para>
/// <para><b>Start-up.</b> No call is required: a run unit's <see cref="LocaleState"/> initializes itself from the L2
/// defaults (owner decision Q2 — <c>COBOL_USER_LOCALE</c>, else the process culture, else the root) when the run unit
/// is created. A host that wants to override that for the program it is about to run calls
/// <c>LocaleManager.SetLocale("es-ES")</c> before invoking the program's entry point (the compiled <c>Main</c> needs
/// no change); the future <c>SET LOCALE</c> statement (design increment T1) writes the same <see cref="LocaleState"/>.</para>
/// </summary>
public static class LocaleManager
{
    private static readonly ConcurrentDictionary<string, LocaleInfo> s_infos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The locale currently selected for collation in the ambient run unit — its LC_COLLATE locale.</summary>
    public static LocaleInfo CurrentLocale => GetLocale(RunUnit.Current.Locale.Current(LocaleCategory.Collate));

    /// <summary>The engine collator for <see cref="CurrentLocale"/> (tertiary, non-ignorable — determination L11).</summary>
    public static Collator CurrentCollator => CurrentLocale.Collator;

    /// <summary>Select <paramref name="localeName"/> for the ambient run unit — every category (LC_ALL). Validates the
    /// tag, loads and applies its tailoring (cached), and records it on <c>RunUnit.Current.Locale</c>. Null, "" or
    /// "root" selects the root order.</summary>
    /// <exception cref="CultureNotFoundException">The tag is neither tailored nor a culture .NET recognizes.</exception>
    /// <exception cref="FormatException">The locale's tailoring file is malformed (the message names file and line).</exception>
    public static void SetLocale(string? localeName)
    {
        var info = GetLocale(localeName);        // validates + loads (throws on an unknown tag / a malformed file)
        RunUnit.Current.Locale.Set(LocaleCategory.All, info.Name.Length == 0 ? "" : info.Name);
    }

    /// <summary>Restore the run unit's L2 user default (the locale it started with).</summary>
    public static void ResetLocale() => RunUnit.Current.Locale.Set(LocaleCategory.All, null);

    /// <summary>The <see cref="LocaleInfo"/> of a tag WITHOUT selecting it (validated, tailoring loaded, cached).</summary>
    /// <exception cref="CultureNotFoundException">The tag is neither tailored nor a culture .NET recognizes.</exception>
    public static LocaleInfo GetLocale(string? localeName)
    {
        string tag = Normalize(localeName);
        return s_infos.GetOrAdd(tag, static t =>
        {
            if (t.Length == 0)
                return new LocaleInfo("", null, CollationTable.Root, recognizedCulture: true);
            var rules = TailoringRules.ForLocale(t);
            bool known = LocaleConfig.IsKnownCulture(t);
            if (rules is null && !known)
                throw new CultureNotFoundException(nameof(localeName), t,
                    $"locale '{t}' is not selectable: no tailoring file ({TailoringRules.DescribeSearch(t)}) and not a culture .NET recognizes");
            var table = CollationEngine.TableForLocale(t);
            // The canonical spelling: the tailoring's own @locale when it names this very tag ("es_es" → "es-ES");
            // a language-fallback match ("es-MX" → es.tailor) keeps the tag as selected.
            string name = rules?.Locale is { } declared && declared.Equals(t, StringComparison.OrdinalIgnoreCase) ? declared : t;
            return new LocaleInfo(name, rules?.Source, table, known);
        });
    }

    /// <summary>Try-form of <see cref="GetLocale"/>: false (and null) for an unselectable tag.</summary>
    public static bool TryGetLocale(string? localeName, out LocaleInfo? info)
    {
        try { info = GetLocale(localeName); return true; }
        catch (CultureNotFoundException) { info = null; return false; }
    }

    /// <summary>The selectable tags this system knows by name (root + tailored); see <see cref="LocaleConfig.IsSupported"/>
    /// for the recognized-culture rule that admits the rest.</summary>
    public static IReadOnlyList<string> SupportedLocales => LocaleConfig.SupportedLocales;

    /// <summary>Forget the cached <see cref="LocaleInfo"/>s (a test that writes a tailoring file at run time, or a
    /// host that changes <c>COBOL_COLLATION_DIR</c>); the engine's own table cache is unaffected.</summary>
    public static void ClearCache() => s_infos.Clear();

    private static string Normalize(string? localeName)
    {
        if (localeName is null) return "";
        string tag = localeName.Trim().Replace('_', '-');
        return tag.Equals("root", StringComparison.OrdinalIgnoreCase) ? "" : tag;
    }
}
