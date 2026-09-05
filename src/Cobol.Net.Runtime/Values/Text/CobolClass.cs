// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Class-condition predicates over a value's character image (ISO §8.8.4.4). ALPHABETIC is the closed Latin set
/// {A–Z, a–z, space} — NOT <c>char.IsLetter</c> (COBOLNET_DESIGN §11.2); NUMERIC over an alphanumeric operand is the
/// digits 0–9 only (no operational sign — §8.8.4.4.4 GR3 n)2).
/// </summary>
public static class CobolClass
{
    /// <summary>
    /// The NUMERIC class test of a NON-NUMERIC-category operand (ISO §8.8.4.4.4 GR3 n)2 — "If the category of the
    /// data item referenced by identifier-1 is not numeric, the condition is true if the content of the data item
    /// referenced by identifier-1 consists entirely of the characters 0, 1, 2, 3, …, 9"): true iff the value
    /// consists ENTIRELY of those digits. An operational sign is NOT a valid character here — n)2 admits none. So
    /// <c>"+1234"</c> and <c>"12A"</c> are both non-numeric; an empty value is non-numeric (GR1 — every class
    /// condition on a zero-length item is false).
    /// <para>A NUMERIC-category operand takes n)1 instead, which is keyed on the item's USAGE and lives in
    /// <see cref="CobolNum.IsNumericImage"/>; for the common native-carrier leaf the compiler folds it to the
    /// constant <c>true</c>, since such a leaf can only hold digits. This method is reached for a numeric-category
    /// item only as n)1.a's zoned delegate.</para>
    /// </summary>
    public static bool IsNumeric(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }

    /// <summary>The BOOLEAN class test (ISO §8.8.4.4.4 GR3 e): "the condition is true if the content of the data
    /// item referenced by identifier-1 consists entirely of the boolean values '0' and '1'". A zero-length operand
    /// is FALSE, like every other class condition (§8.8.4.4.4 GR1).
    /// <para>⛔ THE ZERO-LENGTH ANSWER IS WHY <see cref="HasNonBooleanPosition"/> EXISTS BESIDE THIS. §14.6.13.2
    /// rule 1 asks a NEARLY identical question of a boolean SENDING operand — but the two differ exactly at zero
    /// length, where the class condition is false while rule 1 has no content to call invalid (the clause's closing
    /// paragraph: "If the content of a sending operand is not referenced by a given execution of a statement, any
    /// incompatible data in that operand is not detected"), and a zero-length boolean operand is ordinary
    /// (§8.5.4; §8.8.2 NOTE 2 combines two of them into a zero-length result). So the SCAN is written once and the
    /// two callers put their own zero-length answer on it, rather than one of them inheriting the other's.</para></summary>
    public static bool IsBoolean(string? s) => !string.IsNullOrEmpty(s) && !HasNonBooleanPosition(s);

    /// <summary>Does the value carry a position that is neither <c>'0'</c> nor <c>'1'</c>? The scan behind
    /// <see cref="IsBoolean"/> and behind <see cref="CobolBool.Sending"/>'s §14.6.13.2 rule 1 test — see the
    /// zero-length note there. A null value has no positions.</summary>
    public static bool HasNonBooleanPosition(string? s)
    {
        if (s is null) return false;
        foreach (char c in s)
            if (c is not ('0' or '1')) return true;
        return false;
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

    /// <summary>True if every character is A–Z, a–z, or space (ISO §8.8.4.4).</summary>
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

    /// <summary>The NUMERIC class test of a SIGNED zoned item's character image (ISO §8.8.4.4.4 GR3 n)1.a — "the
    /// presence or absence of an operational sign … is in agreement with the data description … and … the content,
    /// except for the operational sign, consists entirely of the characters 0, 1, 2, 3, …, 9"). <paramref name="signMode"/> 1 = overpunch (the
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

    /// <summary>A USER-DEFINED class test (ISO §8.8.4.4 with a SPECIAL-NAMES class-name, §12.3.7): true iff the
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
