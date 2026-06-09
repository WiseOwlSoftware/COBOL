// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Class-condition predicates over a value's character image (ISO §8.8.4.1.4). ALPHABETIC is the closed Latin set
/// {A–Z, a–z, space} — NOT <c>char.IsLetter</c> (COBOLNET_DESIGN §11.2); NUMERIC is the digits 0–9 with an optional
/// leading or trailing operational sign.
/// </summary>
public static class CobolClass
{
    /// <summary>
    /// True if the character image is all digits 0–9, with one optional leading or trailing separate sign
    /// (<c>+</c>/<c>-</c>). An all-spaces / empty value is not numeric (ISO §8.8.4.1.4). NOTE: an over-punched
    /// operational sign is honored only for a SIGNED item — but a typed-numeric COBOL.NET field IS NUMERIC folds to
    /// <c>true</c> before reaching here, so this runtime check serves alphanumeric content, where over-punch letters
    /// (e.g. the <c>A</c> in <c>"12A"</c>) are ordinary characters and make the value non-numeric.
    /// </summary>
    public static bool IsNumeric(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int start = 0, end = s.Length;
        if (s[0] is '+' or '-') start = 1;                       // leading separate sign
        else if (s[^1] is '+' or '-') end--;                     // trailing separate sign
        if (start >= end) return false;
        for (int i = start; i < end; i++)
            if (s[i] is < '0' or > '9') return false;
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
}
