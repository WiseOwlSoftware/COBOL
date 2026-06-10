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

    /// <summary>True if every character is A–Z, a–z, or space (ISO §8.8.4.1.4).</summary>
    public static bool IsAlphabetic(string? s)
    {
        if (s is null) return false;
        foreach (char c in s)
            if (c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or ' ')) return false;
        return true;
    }

    /// <summary>True if every character is A–Z or space.</summary>
    public static bool IsAlphabeticUpper(string? s)
    {
        if (s is null) return false;
        foreach (char c in s)
            if (c is not (>= 'A' and <= 'Z' or ' ')) return false;
        return true;
    }

    /// <summary>True if every character is a–z or space.</summary>
    public static bool IsAlphabeticLower(string? s)
    {
        if (s is null) return false;
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
    /// literals/THRU ranges expanded at compile time). Spaces are members only if listed; an empty value is true
    /// vacuously (no character violates membership — zero-length items are 2002+).</summary>
    public static bool IsInClass(string? s, string members)
    {
        if (s is null) return false;
        foreach (char c in s)
            if (!members.Contains(c)) return false;
        return true;
    }
}
