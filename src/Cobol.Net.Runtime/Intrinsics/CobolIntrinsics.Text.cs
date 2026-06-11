// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F3 — character intrinsics (ISO §15). CHAR and ORD are PCS-relative (the program collating sequence,
/// §15.15 / §15.70): the parameterless overloads realize the NATIVE sequence (ordinal char codes — the greenfield
/// <c>CollatingModel</c> normalizes STANDARD-1/STANDARD-2/NATIVE to this identity); the <c>ushort[]</c> overloads
/// take the program's emitted <c>__COLLATE</c> weights table (<c>Positions[c]</c> = c's 0-based collating
/// position — the same table <c>CobolString.Compare</c> uses), passed ONLY when the binder flagged a non-identity
/// PCS (hazard H5 — the field does not exist otherwise).
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>CHAR (§15.15.4): the character in ORDINAL position <paramref name="n"/> (1-based) of the
    /// alphanumeric program collating sequence — native sequence: position n is char code n−1 (IF105A asserts
    /// CHAR(37) = '$', ASCII 36). Out-of-range ordinal → EC-ARGUMENT default one-space result (§15.3).</summary>
    public static string Char(long n)
    {
        long c = n - 1;
        if (c is < 0 or > 0xFFFF)
        {
            // EC-ARGUMENT-FUNCTION raise point (the string-result twin of the long sites): the §15.3
            // default one-space result when checking is off; the raise when enabled.
            Exceptions.ExceptionState.ArgumentError($"CHAR argument {n} outside the collating sequence (§15.15.3 rule 1)");
            return " ";
        }
        return ((char)c).ToString();
    }

    /// <summary>CHAR under a non-identity PCS (§15.15.4 rule 2): the FIRST (lowest-coded) character whose
    /// collating weight is n−1 — "the first character defined for that position" when ALSO-grouped characters
    /// share one position.</summary>
    public static string Char(long n, ushort[] weights)
    {
        long wanted = n - 1;
        for (int i = 0; i < weights.Length; i++)
            if (weights[i] == wanted)
                return ((char)i).ToString();
        Exceptions.ExceptionState.ArgumentError($"CHAR argument {n} has no character at that collating position (§15.15.3 rule 1)");
        return " ";                                          // no character at that position → EC default (§15.3)
    }

    /// <summary>ORD (§15.70.4): the 1-based ordinal position of the argument's (single) character in the
    /// alphanumeric program collating sequence — native: char code + 1. The inverse of CHAR.</summary>
    public static long Ord(string s) => s.Length == 0
        ? Exceptions.ExceptionState.ArgumentError("ORD argument is empty (§15.70.3 — a one-character argument is required)")
        : s[0] + 1;

    /// <summary>ORD under a non-identity PCS: the character's collating weight + 1 (<c>Positions[c]</c> is the
    /// 0-based position). A char beyond the table keeps its native ordinal (the table covers the alphabet's
    /// domain; ALSO members share one position).</summary>
    public static long Ord(string s, ushort[] weights)
    {
        if (s.Length == 0)
            return Exceptions.ExceptionState.ArgumentError("ORD argument is empty (§15.70.3 — a one-character argument is required)");
        char c = s[0];
        return c < weights.Length ? weights[c] + 1L : c + 1L;
    }

    /// <summary>UPPER-CASE (§15.97.4): every lowercase letter replaced by its uppercase correspondent; result
    /// length = argument length (the fixed-width field image in carries the width out).</summary>
    public static string UpperCase(string s) => s.ToUpperInvariant();

    /// <summary>LOWER-CASE (§15.57.4): every uppercase letter replaced by its lowercase correspondent.</summary>
    public static string LowerCase(string s) => s.ToLowerInvariant();

    /// <summary>REVERSE (§15.78.4): the argument's characters in reverse order; same length.</summary>
    public static string Reverse(string s)
    {
        char[] a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    /// <summary>LENGTH (§15.50.4) — the RUNTIME residue of the bind-time fold (deep-dive D7): the length in
    /// character positions of a value only the backend rendered (a nested string-function result, whose padded
    /// fixed-width image length IS its character-position count). Fixed items and literals fold at bind time and
    /// never reach here.</summary>
    public static long Length(string s) => s.Length;
}
