// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Globalization;

/// <summary>How a locale-phrase of the CHARACTER CLASSIFICATION clause names its locale (ISO/IEC 1989:2023 §12.3.6.2:
/// <c>locale-name-n | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT</c>; §12.3.6.4 GR5 a–d / f–i).</summary>
public enum LocalePhraseKind
{
    /// <summary>The phrase is absent — the classification of the coded character set (GR5 e/j, GR6).</summary>
    None = 0,
    /// <summary>A SPECIAL-NAMES locale-name (GR5 a/f) — its L1-normalized tag travels.</summary>
    Named = 1,
    /// <summary>The word LOCALE — the locale CURRENT at activation (GR5 b/g; §14.6.6 r2).</summary>
    Current = 2,
    /// <summary>SYSTEM-DEFAULT (GR5 c/h).</summary>
    SystemDefault = 3,
    /// <summary>USER-DEFAULT (GR5 d/i).</summary>
    UserDefault = 4,
}

/// <summary>
/// The CHARACTER CLASSIFICATION in effect for one runtime module (ISO §12.3.6 OBJECT-COMPUTER; DESIGN-locale-facility
/// §4.5; kb/Work PB64 T5): the LC_CTYPE facts for alphanumeric characters and for national characters, each null
/// when "the character classification associated with the computer's coded character set" applies (§12.3.6.4 GR5
/// e/j, GR6 — the implementor's correspondence, §15.57.4 r4 / §15.97.4 r4). Resolved at the module's ACTIVATION
/// (§12.3.6.4 GR8 — "effective with the initial state of the runtime modules to which they apply"; §14.6.6 r2 —
/// "On activation of a runtime element, if the CHARACTER CLASSIFICATION clause is specified … category LC_CTYPE in
/// the specified locale is used"), so the word LOCALE binds the locale current WHEN THE PROGRAM IS ENTERED, not at
/// each use. Read by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§15.57.4 r3 / §15.97.4 r3) and by the class
/// tests ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER (§8.8.4.4.4 GR3 b/c/d) — GR7 a/b.
/// </summary>
public sealed class CharacterClassification
{
    /// <summary>No clause — the coded character set's classification for both classes (GR6).</summary>
    public static readonly CharacterClassification None = new(null, null);

    private CharacterClassification(LocaleFacts? alphanumeric, LocaleFacts? national)
    {
        Alphanumeric = alphanumeric;
        National = national;
    }

    /// <summary>LC_CTYPE for alphanumeric characters, or null for the coded character set's classification.</summary>
    public LocaleFacts? Alphanumeric { get; }

    /// <summary>LC_CTYPE for national characters, or null for the coded character set's classification.</summary>
    public LocaleFacts? National { get; }

    /// <summary>The facts for an operand of the given class.</summary>
    public LocaleFacts? For(bool national) => national ? National : Alphanumeric;

    /// <summary>Resolve the clause's two phrases for the module being activated (§12.3.6.4 GR5 — the eight cases;
    /// DETERMINATION: a named locale that is unavailable or has no culture data yields the invariant facts here, and
    /// the operations that use it raise EC-LOCALE-MISSING / EC-LOCALE-INVALID at their own sites).</summary>
    public static CharacterClassification Resolve(LocalePhraseKind alphanumericKind, string? alphanumericTag,
        LocalePhraseKind nationalKind, string? nationalTag)
    {
        var an = Facts(alphanumericKind, alphanumericTag);
        var nat = Facts(nationalKind, nationalTag);
        return an is null && nat is null ? None : new CharacterClassification(an, nat);
    }

    private static LocaleFacts? Facts(LocalePhraseKind kind, string? tag)
    {
        switch (kind)
        {
            case LocalePhraseKind.None: return null;
            case LocalePhraseKind.Named: return LocaleFacts.For(tag);
            case LocalePhraseKind.Current: return LocaleFacts.For(RunUnit.Current.Locale.Current(LocaleCategory.Ctype));
            case LocalePhraseKind.SystemDefault: return LocaleFacts.For(RunUnit.Current.Locale.SystemDefault.Ctype);
            default: return LocaleFacts.For(RunUnit.Current.Locale.UserDefault.Ctype);
        }
    }

    public override string ToString() => $"classification(alphanumeric: {Alphanumeric?.ToString() ?? "coded character set"}; national: {National?.ToString() ?? "coded character set"})";
}
