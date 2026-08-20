// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Class-condition predicates over a value's character image (ISO §8.8.4.1.4). ALPHABETIC is the closed Latin set
/// {A–Z, a–z, space} — NOT <c>char.IsLetter</c> (COBOLNET_DESIGN §11.2); NUMERIC over an alphanumeric operand is the
/// digits 0–9 only (no operational sign — §8.8.4.4 rule 2).
/// </summary>
public static class CobolClass
{
    /// <summary>
    /// The NUMERIC class test for the content that reaches this runtime check (ISO §8.8.4.4 GR2): true iff the value
    /// consists ENTIRELY of the digits 0–9. An operational sign is NOT a valid character here — rule 2 governs a
    /// NON-numeric (alphanumeric / edited) operand, the only kind that reaches this method (a numeric-category COBOL.NET
    /// field IS NUMERIC folds to <c>true</c> at compile time, GR1). So <c>"+1234"</c> and <c>"12A"</c> are both
    /// non-numeric; an all-spaces / empty value is non-numeric.
    /// </summary>
    public static bool IsNumeric(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }

    /// <summary>ALPHABETIC under a CHARACTER CLASSIFICATION locale (ISO §8.8.4.4.4 GR3 b1 — "consists only of characters
    /// identified as alphabetic in locale category LC_CTYPE"; kb/Work PB64 T5): a Unicode LETTER per the locale's
    /// culture — the POSIX <c>alpha</c> class. ⚠ As the rule reads, the locale case names letters ONLY: space is not
    /// <c>alpha</c> (b2, the coded-character-set case, lists space explicitly; b1 does not) — documented in
    /// CONFORMANCE.md §4 item 5. <paramref name="facts"/> null (no classification, or the coded character set's) is the
    /// closed Latin set of <see cref="IsAlphabetic(string?)"/>.</summary>
    public static bool IsAlphabetic(string? s, Globalization.LocaleFacts? facts)
    {
        facts = facts?.Require(LocaleCategory.Ctype, "class condition ALPHABETIC under the CHARACTER CLASSIFICATION", "ISO §8.8.4.4.4 GR3 b1 / §12.3.6.4 GR7b");
        if (facts is null || !facts.HasCultureData) return IsAlphabetic(s);
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var r in s.EnumerateRunes()) if (!System.Text.Rune.IsLetter(r)) return false;
        return true;
    }

    /// <summary>ALPHABETIC-UPPER under a classification locale (§8.8.4.4.4 GR3 d1 — "uppercase alphabetic in LC_CTYPE",
    /// the POSIX <c>upper</c> class): a letter that is uppercase, or a letter the locale's case mapping LOWERS to
    /// something else (the round-trip test — Turkish dotted/dotless I under <c>tr</c>); null facts → the Latin set.</summary>
    public static bool IsAlphabeticUpper(string? s, Globalization.LocaleFacts? facts)
    {
        facts = facts?.Require(LocaleCategory.Ctype, "class condition ALPHABETIC-UPPER under the CHARACTER CLASSIFICATION", "ISO §8.8.4.4.4 GR3 d1 / §12.3.6.4 GR7b");
        if (facts is null || !facts.HasCultureData) return IsAlphabeticUpper(s);
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var r in s.EnumerateRunes())
            if (!System.Text.Rune.IsLetter(r) || !(System.Text.Rune.IsUpper(r) || (r.IsBmp && facts.TextInfo.ToLower((char)r.Value) != (char)r.Value))) return false;
        return true;
    }

    /// <summary>ALPHABETIC-LOWER under a classification locale (§8.8.4.4.4 GR3 c1 — the POSIX <c>lower</c> class): a letter
    /// that is lowercase, or one the locale's case mapping UPPERS to something else; null facts → the Latin set.</summary>
    public static bool IsAlphabeticLower(string? s, Globalization.LocaleFacts? facts)
    {
        facts = facts?.Require(LocaleCategory.Ctype, "class condition ALPHABETIC-LOWER under the CHARACTER CLASSIFICATION", "ISO §8.8.4.4.4 GR3 c1 / §12.3.6.4 GR7b");
        if (facts is null || !facts.HasCultureData) return IsAlphabeticLower(s);
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var r in s.EnumerateRunes())
            if (!System.Text.Rune.IsLetter(r) || !(System.Text.Rune.IsLower(r) || (r.IsBmp && facts.TextInfo.ToUpper((char)r.Value) != (char)r.Value))) return false;
        return true;
    }

    /// <summary>The runtime membership test of a class condition whose class is an ALPHABET-NAME (ISO §8.8.4.4.4
    /// GR3 a — "consists entirely of characters in the coded character set identified by alphabet-name-1"; kb/Work
    /// PB109). The compile-time binder mapped the alphabet's phrase to the SET's membership rule (§12.3.7.4 GR7
    /// Table 6 + the CodedCharacterSet determinations).</summary>
    public enum CodedSetKind
    {
        /// <summary>NATIVE, UTF-16, or a literal-phrase alphabet: the set contains EVERY native character (GR7 k4
        /// places the whole native set in a literal alphabet's code set), so the condition is true for any content —
        /// deliberate, not vacuous (the SET is total even where the SEQUENCE is remapped).</summary>
        AllNative,
        /// <summary>STANDARD-1 / STANDARD-2 — the 128 ISO/IEC 646 IRV characters (GR7 c: the identity correspondence
        /// on U+0000–U+007F).</summary>
        Ascii,
        /// <summary>UCS-4 / UTF-8 — the ISO/IEC 10646 scalar values (GR7 f/g): every character except an unpaired
        /// surrogate code unit, which is not a character of these sets.</summary>
        ScalarValues,
    }

    /// <summary>The GR3 a class condition: true iff <paramref name="s"/> consists entirely of characters of the
    /// coded character set (a zero-length operand is FALSE — GR1, enforced like the other class tests).</summary>
    public static bool IsInCodedSet(string? s, CodedSetKind kind)
    {
        if (string.IsNullOrEmpty(s)) return false;
        switch (kind)
        {
            case CodedSetKind.AllNative: return true;
            case CodedSetKind.Ascii:
                foreach (char c in s) if (c > 0x7F) return false;
                return true;
            default:   // ScalarValues — well-formed UTF-16 (no unpaired surrogate)
                for (int i = 0; i < s.Length; i++)
                {
                    if (char.IsHighSurrogate(s[i]))
                    {
                        if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1])) return false;
                        i++;
                    }
                    else if (char.IsLowSurrogate(s[i])) return false;
                }
                return true;
        }
    }

    /// <summary>True if every character is A–Z, a–z, or space (ISO §8.8.4.1.4).</summary>
    public static bool IsAlphabetic(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;   // a zero-length operand: the class condition is false (ISO §8.8.4.4.4 GR1)
        foreach (char c in s)
            if (c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or ' ')) return false;
        return true;
    }

    /// <summary>True if every character is A–Z or space.</summary>
    public static bool IsAlphabeticUpper(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;   // a zero-length operand: the class condition is false (ISO §8.8.4.4.4 GR1)
        foreach (char c in s)
            if (c is not (>= 'A' and <= 'Z' or ' ')) return false;
        return true;
    }

    /// <summary>True if every character is a–z or space.</summary>
    public static bool IsAlphabeticLower(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;   // a zero-length operand: the class condition is false (ISO §8.8.4.4.4 GR1)
        foreach (char c in s)
            if (c is not (>= 'a' and <= 'z' or ' ')) return false;
        return true;
    }

    /// <summary>The NUMERIC class test of a SIGNED zoned item's character image (ISO §8.8.4.4 rule 3 — the
    /// content must be numeric with a valid operational sign). <paramref name="signMode"/> 1 = overpunch (the
    /// sign zone shares the digit position: a plain digit, or the ASCII zoned overpunch sets <c>{A–I</c> for
    /// +0..+9 / <c>}J–R</c> for −0..−9); 2 = SEPARATE (a leading/trailing <c>+</c>/<c>-</c> character position,
    /// §13.18.49). Every other position must be a digit.</summary>
    public static bool IsNumericZoned(string? s, int signMode, bool leading)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int signIdx = leading ? 0 : s.Length - 1;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (i == signIdx)
            {
                bool ok = signMode == 2
                    ? c is '+' or '-'
                    : c is >= '0' and <= '9' or '{' or '}' or (>= 'A' and <= 'I') or (>= 'J' and <= 'R');
                if (!ok) return false;
                continue;
            }
            if (c is < '0' or > '9') return false;
        }
        return true;
    }

    /// <summary>A USER-DEFINED class test (ISO §8.8.4.1.4 with a SPECIAL-NAMES class-name, §12.3.7): true iff the
    /// value consists ENTIRELY of the class's member characters (<paramref name="members"/> — the clause's
    /// literals/THRU ranges expanded at compile time). Spaces are members only if listed; a zero-length item is
    /// FALSE (ISO §8.8.4.4.4 GR1 — every class condition on a zero-length operand is false; zero-length items are
    /// 2002+, reachable via a DYNAMIC-LENGTH item, an ODO group with count 0, or ref-mod X(1:0)).</summary>
    public static bool IsInClass(string? s, string members)
    {
        if (string.IsNullOrEmpty(s)) return false;   // a zero-length operand: the class condition is false (ISO §8.8.4.4.4 GR1)
        foreach (char c in s)
            if (!members.Contains(c)) return false;
        return true;
    }
}
