// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Runtime.Globalization;

/// <summary>
/// A BCP 47 locale tag in its ONE canonical form (kb/Work PB2763): the spelling every locale cache of the runtime is
/// keyed by and every locale consumer compares. The rules are written here and nowhere else:
/// <list type="bullet">
/// <item>surrounding white space is ignored, and <c>_</c> is accepted for <c>-</c> (the CLDR file spelling
/// <c>de_AT</c> and the POSIX spelling <c>de_AT</c> are the tag <c>de-AT</c>);</item>
/// <item>the root locale is <c>""</c>, <c>root</c> or <c>und</c> as the first subtag (BCP 47's "undetermined", CLDR's
/// root) — its <see cref="Base"/> is <c>""</c>;</item>
/// <item>the base tag (before the Unicode locale extension) takes the BCP 47 case conventions: the language lower-case,
/// a four-letter script title-case, a two-letter or three-digit region upper-case, anything else as written;</item>
/// <item>the Unicode locale extension (<c>-u-</c> and what follows) is case-insensitive and is kept lower-case, its empty
/// subtags dropped.</item>
/// </list>
/// What the tag MEANS is someone else's: an external identification's POSIX spelling (<c>.codeset</c>, <c>@modifier</c>,
/// <c>C</c>) is <see cref="LocaleIdentification.Normalize"/>'s (DESIGN-locale-facility L1), the extension's collation keys
/// are <see cref="Collation.Cldr.CldrLocaleTag"/>'s, and availability is the collation engine's
/// (<see cref="Collation.CollationEngine.IsKnownLocale"/>). Immutable; <see cref="Parse"/> never throws.
/// </summary>
public sealed class LocaleTag
{
    private LocaleTag(string @base, string extension)
    {
        Base = @base;
        Extension = extension;
        Canonical = extension.Length == 0 ? @base : (@base.Length == 0 ? "und" : @base) + "-u-" + extension;
    }

    /// <summary>The root locale: <c>""</c>.</summary>
    public static LocaleTag Root { get; } = new("", "");

    /// <summary>The base tag without the extension, in canonical case ("de-AT", "sr-Latn-RS"); "" for the root.</summary>
    public string Base { get; }

    /// <summary>The Unicode locale extension's subtags after <c>-u-</c>, lower-case and joined by <c>-</c>
    /// ("co-phonebk-kf-upper"); "" when the tag has none.</summary>
    public string Extension { get; }

    /// <summary>The canonical spelling: <see cref="Base"/>, then <c>-u-</c> and <see cref="Extension"/> when there is one
    /// (the root with an extension is spelt <c>und-u-…</c>, the one form BCP 47 gives it). The key of every locale cache.</summary>
    public string Canonical { get; }

    /// <summary>True for the root locale (an empty <see cref="Base"/>).</summary>
    public bool IsRoot => Base.Length == 0;

    /// <summary>True when every subtag of <see cref="Base"/> is one to eight ASCII letters or digits — the shape RFC 5646
    /// §2.1 gives a subtag. A base that is not (a POSIX <c>@modifier</c> L1 does not translate, a <c>.codeset</c> in a
    /// name L1 never normalized, a space) names no locale, whatever its language subtag would find. The extension is
    /// not judged here: its collation keys are <see cref="Collation.Cldr.CldrLocaleTag"/>'s, which reports a key it
    /// cannot read (and L1 writes a collation type by its LDML name, "phonebook", longer than a BCP 47 value).</summary>
    public bool IsWellFormed
    {
        get
        {
            if (Base.Length == 0) return true;
            foreach (string subtag in Base.Split('-'))
                if (subtag.Length is 0 or > 8 || !subtag.All(char.IsAsciiLetterOrDigit)) return false;
            return true;
        }
    }

    /// <summary>The language subtag of <see cref="Base"/> ("es" of "es-MX"); "" for the root.</summary>
    public string Language
    {
        get
        {
            int dash = Base.IndexOf('-');
            return dash < 0 ? Base : Base[..dash];
        }
    }

    /// <summary>Parse any spelling of a tag into its canonical form (the class summary's rules). Never throws.</summary>
    public static LocaleTag Parse(string? text)
    {
        string t = (text ?? "").Trim().Replace('_', '-');
        if (t.Length == 0) return Root;
        string baseText = t, extension = "";
        int u = t.IndexOf("-u-", StringComparison.OrdinalIgnoreCase);   // not a COBOL word: a locale identification (BCP 47 text)
        if (u >= 0)
        {
            baseText = t[..u];
            extension = string.Join('-', t[(u + 3)..].Split('-', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }
        string @base = CanonicalBase(baseText);
        return @base.Length == 0 && extension.Length == 0 ? Root : new LocaleTag(@base, extension);
    }

    /// <summary>The predefined .NET culture of a base tag (never a synthesized one), or null when .NET's culture data does
    /// not know it — the ONE culture probe of the runtime: whether a locale is a culture .NET recognizes
    /// (<see cref="Collation.CollationEngine.IsKnownLocale"/>) and which culture a locale's LC_CTYPE / LC_MONETARY /
    /// LC_TIME facts come from (<see cref="LocaleFacts"/>) both ask it.</summary>
    public static CultureInfo? PredefinedCulture(string baseTag)
    {
        try { return CultureInfo.GetCultureInfo(baseTag, predefinedOnly: true); }
        catch (CultureNotFoundException) { return null; }
        catch (ArgumentException) { return null; }
    }

    public override string ToString() => Canonical;

    /// <summary>The base tag in BCP 47 case; "" for the root (<c>root</c> / <c>und</c> as the first subtag).</summary>
    private static string CanonicalBase(string tag)
    {
        var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        if (parts[0].Equals("root", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("und", StringComparison.OrdinalIgnoreCase)) return "";   // not a COBOL word: a locale identification (BCP 47 text)
        var outParts = new string[parts.Length];
        outParts[0] = parts[0].ToLowerInvariant();
        for (int i = 1; i < parts.Length; i++)
        {
            string p = parts[i];
            outParts[i] = p.Length == 4 && p.All(char.IsLetter) ? char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()
                : (p.Length == 2 && p.All(char.IsLetter)) || (p.Length == 3 && p.All(char.IsDigit)) ? p.ToUpperInvariant()
                : p;
        }
        return string.Join('-', outParts);
    }
}
