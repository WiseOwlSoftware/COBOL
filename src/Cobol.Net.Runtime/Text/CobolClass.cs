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
}
