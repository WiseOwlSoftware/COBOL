// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>The locale categories of ISO/IEC 1989:2023 §8.2.1 / §14.9.39 (SET LOCALE): LC_ALL names every category.</summary>
public enum LocaleCategory
{
    All = 0,
    Collate = 1,
    Ctype = 2,
    Messages = 3,
    Monetary = 4,
    Numeric = 5,
    Time = 6,
}

/// <summary>
/// The run unit's LOCALE state (DESIGN-locale-facility §4.3; ISO §8.2.1 "the current locale", §14.6.6): the two
/// implementor defaults and the locale currently in effect per category. Owned by <see cref="RunUnit"/> — one per run
/// unit, established at activation.
/// <para><b>⚖ DETERMINATION L2 (owner decision Q2, 2026-08-18):</b> the USER default locale is the environment variable
/// <c>COBOL_USER_LOCALE</c>, else the process's <see cref="CultureInfo.CurrentCulture"/>, else the invariant/root
/// locale; the SYSTEM default is <c>COBOL_SYSTEM_LOCALE</c>, else <see cref="CultureInfo.InstalledUICulture"/>, else
/// invariant. Both are read ONCE, when the run unit's state is created (L3). A locale is identified by its BCP-47 /
/// CLDR tag ("es-ES", "fr", ""); the empty tag is the root.</para>
/// <para>This is the T1 seam of the design in its smallest form — the state the LOCALE-based collating sequence
/// (<see cref="LocaleCollation"/>, PB101) needs today. The SET LOCALE formats, saved-locale handles and the other
/// categories' consumers (T1/T4–T6) extend it; nothing else may hold current-locale state (one mechanism).</para>
/// </summary>
public sealed class LocaleState
{
    /// <summary>The environment variable naming the user default locale (Q2).</summary>
    public const string UserDefaultVariable = "COBOL_USER_LOCALE";

    /// <summary>The environment variable naming the system default locale (Q2).</summary>
    public const string SystemDefaultVariable = "COBOL_SYSTEM_LOCALE";

    private readonly string?[] _current = new string?[7];   // per LocaleCategory (index 0 = All, unused); null = the user default

    public LocaleState()
    {
        UserDefault = Determine(UserDefaultVariable, () => CultureInfo.CurrentCulture);
        SystemDefault = Determine(SystemDefaultVariable, () => CultureInfo.InstalledUICulture);
    }

    /// <summary>The user default locale's tag (L2), fixed at run-unit activation.</summary>
    public string UserDefault { get; }

    /// <summary>The system default locale's tag (L2), fixed at run-unit activation.</summary>
    public string SystemDefault { get; }

    /// <summary>The locale currently in effect for <paramref name="category"/> — the user default until a SET LOCALE
    /// changes it (§14.9.39 formats 11/12, T1).</summary>
    public string Current(LocaleCategory category) =>
        category == LocaleCategory.All ? UserDefault : _current[(int)category] ?? UserDefault;

    /// <summary>Make <paramref name="localeTag"/> current for <paramref name="category"/> (LC_ALL: every category);
    /// null restores the user default. The seam SET LOCALE lands on.</summary>
    public void Set(LocaleCategory category, string? localeTag)
    {
        if (category == LocaleCategory.All)
            for (int i = 1; i < _current.Length; i++) _current[i] = localeTag;
        else _current[(int)category] = localeTag;
    }

    /// <summary>The L2 determination: the variable when set (trimmed; "" means root), else the culture's name.</summary>
    internal static string Determine(string variable, Func<CultureInfo> fallback)
    {
        string? env = Environment.GetEnvironmentVariable(variable);
        if (env is not null) return env.Trim();
        try { return fallback().Name; }
        catch (Exception) { return ""; }   // a host without culture data (invariant globalization): the root
    }
}
