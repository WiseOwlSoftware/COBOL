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

    /// <summary>CONCAT (§15.18.4, 2023): the characters of all arguments in order — argument-1 followed by each
    /// argument-2 (rules 1 &amp; 4). Each argument arrives as its fixed-width display IMAGE (trailing padding
    /// included — §15.18.4 rule 1 "all of the characters"), so the result length is the sum of the argument
    /// widths.</summary>
    public static string Concat(params string[] parts) => string.Concat(parts);

    /// <summary>BASECONVERT (§15.12.4, 2023): the unsigned integer whose digits are <paramref name="value"/> in
    /// base <paramref name="fromBase"/>, re-expressed as a string of 0-9 / A-F digits in base
    /// <paramref name="toBase"/> (both bases 2..16 — §15.12.3). An out-of-range base or a digit invalid for the
    /// source base sets EC-ARGUMENT-FUNCTION and returns the §15.3 default (a zero-length result when checking is
    /// off). Leading/trailing spaces of the fixed-width argument image are ignored.</summary>
    public static string BaseConvert(string value, long fromBase, long toBase)
    {
        if (fromBase is < 2 or > 16 || toBase is < 2 or > 16)
        {
            Exceptions.ExceptionState.ArgumentError($"BASECONVERT base(s) {fromBase}/{toBase} out of the range 2..16 (§15.12.3 rule 1)");
            return "";
        }
        System.Numerics.BigInteger acc = 0;
        foreach (char ch in value.Trim())
        {
            int d = ch is >= '0' and <= '9' ? ch - '0'
                  : ch is >= 'A' and <= 'F' ? ch - 'A' + 10
                  : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : -1;
            if (d < 0 || d >= fromBase)
            {
                Exceptions.ExceptionState.ArgumentError($"BASECONVERT: '{ch}' is not a base-{fromBase} digit (§15.12.3 rule 2)");
                return "";
            }
            acc = acc * fromBase + d;
        }
        if (acc == 0) return "0";
        const string digits = "0123456789ABCDEF";
        var sb = new System.Text.StringBuilder();
        for (; acc > 0; acc /= toBase) sb.Insert(0, digits[(int)(acc % toBase)]);
        return sb.ToString();
    }

    // ── CONVERT (§15.19, 2023) — data-representation conversion ─────────────────────────────────────────────────
    // Source codes: 0 = ANY, 1 = ANUM, 2 = HEX, 3 = NAT.  Dest codes: 1 = ANUM, 3 = NAT, 4 = BYTE.
    // Char-set model: the alphanumeric coded set is 8-bit Latin-1 (code point == byte); national is UTF-16BE, one
    // char/position (D-N1 / §8.1.2 NOTE 2). Substitution char '?' for an untranslatable value (§15.19.4 r1/r3,
    // implementor-defined). Source ANY = the item's canonical display image (typed-native model; §8.1.2 NOTE 2
    // leaves the usage representation implementor-defined) — always paired with a HEX destination (SR8).

    /// <summary>CONVERT (§15.19.4): re-express <paramref name="arg"/> (in source format <paramref name="src"/>) in
    /// the destination format (<paramref name="dst"/> + <paramref name="dstHex"/>). An untranslatable character
    /// yields the substitution character and sets EC-DATA-CONVERSION (nonfatal, rules 1/3).</summary>
    public static string Convert(string arg, int src, int dst, bool dstHex)
    {
        const int ANUM = 1, HEX = 2, NAT = 3, BYTE = 4;

        // Repertoire translation (§15.19.4 r1/r3): both sides character, no HEX — the DISPLAY-OF/NATIONAL-OF map.
        if (!dstHex && dst != BYTE && (src == ANUM || src == NAT))
        {
            var sb = new System.Text.StringBuilder(arg.Length);
            foreach (char c in arg)
                sb.Append(dst == NAT ? c                             // ANUM→NAT: same code point (Latin-1 ⊂ national, D-N4)
                                     : c <= 0xFF ? c : Sub());        // NAT→ANUM: fits a byte, else substitute (r1)
            return sb.ToString();
        }

        // Bit/byte pathway — reduce argument-1 to a byte string per the source format.
        byte[] bytes;
        switch (src)
        {
            case NAT:                                                // national → UTF-16BE (2 bytes/position, r4 basis)
                bytes = new byte[arg.Length * 2];
                for (int i = 0; i < arg.Length; i++) { bytes[2 * i] = (byte)(arg[i] >> 8); bytes[2 * i + 1] = (byte)arg[i]; }
                break;
            case HEX:                                                // a string of hex digits representing complete bytes (SR4)
                bytes = HexDigitsToBytes(arg);
                break;
            default:                                                 // ANUM / ANY → Latin-1 (1 byte/char); ANY = the item's canonical image
                bytes = new byte[arg.Length];
                for (int i = 0; i < arg.Length; i++)
                    bytes[i] = arg[i] <= 0xFF ? (byte)arg[i] : ByteSub();
                break;
        }

        // Render the byte string in the destination format.
        if (dstHex) return ToHex(bytes);                             // r2 (ANUM HEX) / r4 (NAT HEX) — same digit code points (D-N4)
        if (dst == NAT)                                              // HEX → NAT (r3): 2 bytes → one national char, pad a trailing odd byte
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
                sb.Append((char)((bytes[i] << 8) | (i + 1 < bytes.Length ? bytes[i + 1] : 0)));
            return sb.ToString();
        }
        // ANUM (r1) / BYTE (r5): one alphanumeric char per byte.
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
        return new string(chars);
    }

    private static char Sub()
    {
        Exceptions.ExceptionState.DataConversionError("CONVERT: value has no destination-repertoire correspondent (§15.19.4 r1/r3)");
        return '?';
    }
    private static byte ByteSub() { _ = Sub(); return (byte)'?'; }

    private static byte[] HexDigitsToBytes(string s)
    {
        string t = s.Trim();                                         // the fixed-width image's surrounding spaces are not digits
        // A malformed HEX source VIOLATES argument rule SR4 (§15.19.3) — that is a FATAL EC-ARGUMENT-FUNCTION
        // (the §15.3 argument-rule default), NOT the nonfatal EC-DATA-CONVERSION (which is only for an untranslatable
        // RESULT value, §15.19.4 r1/r3). ArgumentError throws when checking is on; else the implementor-defined
        // pad/zero result stands.
        if ((t.Length & 1) != 0)                                     // SR4 — complete bytes ⇒ an even digit count
        { Exceptions.ExceptionState.ArgumentError("CONVERT: HEX source is not a whole number of bytes (§15.19.3 SR4)"); t += "0"; }
        var b = new byte[t.Length / 2];
        for (int i = 0; i < b.Length; i++)
        {
            int hi = HexVal(t[2 * i]), lo = HexVal(t[2 * i + 1]);
            if (hi < 0 || lo < 0)
            { Exceptions.ExceptionState.ArgumentError("CONVERT: HEX source has a non-hex digit (§15.19.3 SR4)"); hi = hi < 0 ? 0 : hi; lo = lo < 0 ? 0 : lo; }
            b[i] = (byte)((hi << 4) | lo);
        }
        return b;
    }
    private static int HexVal(char c) => c is >= '0' and <= '9' ? c - '0'
        : c is >= 'A' and <= 'F' ? c - 'A' + 10 : c is >= 'a' and <= 'f' ? c - 'a' + 10 : -1;

    private static string ToHex(byte[] bytes)
    {
        const string D = "0123456789ABCDEF";
        var sb = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (byte x in bytes) { sb.Append(D[x >> 4]); sb.Append(D[x & 0xF]); }
        return sb.ToString();
    }

    /// <summary>SUBSTITUTE (§15.87.4): argument-1 (<paramref name="source"/>) with each occurrence of a
    /// <paramref name="froms"/> substring replaced by the parallel <paramref name="tos"/> string. Each pair's
    /// <paramref name="modes"/> flags select FIRST (bit 0, only the first occurrence — rule 3.a), LAST (bit 1,
    /// only the last — rule 3.b), or ALL (default), and ANYCASE (bit 2, case-folded matching per LOWER-CASE —
    /// rule 5). The scan is a single left-to-right pass: at each position the first pair (in listed order) whose
    /// argument-2 matches AND is eligible is substituted, then the scan resumes past the matched SOURCE substring
    /// (rules 3/4 — a substituted argument-3 is never re-scanned; occurrences count over argument-1, non-
    /// overlapping). A zero-length argument-1 or any zero-length argument-2 sets EC-ARGUMENT-FUNCTION and returns
    /// a zero-length value (rule 1). FIRST/LAST target the pair's first/last occurrence in the source.</summary>
    public static string Substitute(string source, string[] froms, string[] tos, int[] modes)
    {
        if (source.Length == 0 || froms.Any(f => f.Length == 0))                 // §15.87.4 rule 1
        {
            Exceptions.ExceptionState.ArgumentError("SUBSTITUTE argument-1 or an argument-2 is of zero length (§15.87.4 rule 1)");
            return "";
        }
        int k = froms.Length;
        // ANYCASE (rule 5) folds via LOWER-CASE (ToLowerInvariant — the implementor-defined §15.57 fold, matching
        // FindString), NOT invariant upper-fold: length-preserving, so positions over the lowered images align.
        string srcLower = source.ToLowerInvariant();
        var fromsLower = new string[k];
        for (int p = 0; p < k; p++) fromsLower[p] = froms[p].ToLowerInvariant();
        static bool MatchAt(string s, string sLower, int i, string f, string fLower, bool anycase) =>
            i + f.Length <= s.Length
            && (anycase ? string.CompareOrdinal(sLower, i, fLower, 0, f.Length) == 0
                        : string.CompareOrdinal(s, i, f, 0, f.Length) == 0);
        // The single designated position for a FIRST/LAST pair (rule 3.a/3.b), computed over the SOURCE
        // occurrences (non-overlapping); −1 means "every occurrence" (the default) or "no occurrence".
        var target = new int[k];
        for (int p = 0; p < k; p++)
        {
            bool anycase = (modes[p] & 4) != 0;
            if ((modes[p] & 3) == 0) { target[p] = -1; continue; }               // ALL — no single target
            int first = -1, last = -1;
            for (int i = 0; i <= source.Length - froms[p].Length; )
                if (MatchAt(source, srcLower, i, froms[p], fromsLower[p], anycase))
                { if (first < 0) first = i; last = i; i += froms[p].Length; }
                else i++;
            target[p] = (modes[p] & 1) != 0 ? first : last;                      // FIRST or LAST
        }
        var sb = new System.Text.StringBuilder(source.Length);
        for (int i = 0; i < source.Length; )
        {
            int hit = -1;
            for (int p = 0; p < k; p++)
                if (MatchAt(source, srcLower, i, froms[p], fromsLower[p], (modes[p] & 4) != 0)
                    && (target[p] == -1 ? (modes[p] & 3) == 0 : target[p] == i)) // ALL, or the FIRST/LAST target
                { hit = p; break; }
            if (hit >= 0) { sb.Append(tos[hit]); i += froms[hit].Length; }        // rule 3 — resume past the source match
            else { sb.Append(source[i]); i++; }
        }
        return sb.ToString();
    }

    /// <summary>FIND-STRING (§15.37.4): the 1-based character position of argument-2 (<paramref name="needle"/>)
    /// within argument-1 (<paramref name="hay"/>). With <paramref name="last"/> the LAST occurrence is sought
    /// (rule 1); <paramref name="skip"/> is argument-3 — the number of matches to ignore before determining the
    /// position returned (rule 2, counted from the first occurrence, or from the last when <paramref name="last"/>);
    /// <paramref name="anycase"/> folds case per LOWER-CASE without a locale (rule 4). A zero-length argument-1 or
    /// argument-2 (rule 5), or no remaining match (rule 3), returns 0. An OCCURRENCE is any character position at
    /// which argument-2 matches a substring of argument-1 (§15.37.4 rule 1 — a plain substring match, OVERLAPPING
    /// occurrences included; §15.37 defines no consumption/advance and never references INSPECT's scanning).</summary>
    public static long FindString(string hay, string needle, bool last, long skip, bool anycase)
    {
        if (hay.Length == 0 || needle.Length == 0) return 0;                     // §15.37.4 rule 5
        string h = anycase ? hay.ToLowerInvariant() : hay;                       // rule 4 — the LOWER-CASE fold
        string n = anycase ? needle.ToLowerInvariant() : needle;
        var positions = new List<int>();
        for (int i = 0; (i = h.IndexOf(n, i, StringComparison.Ordinal)) >= 0; i++)   // every match position (overlapping incl., rule 1)
            positions.Add(i + 1);                                                // 1-based character position
        if (positions.Count == 0) return 0;                                      // rule 3 — no match
        if (skip < 0) skip = 0;
        int idx = last ? positions.Count - 1 - (int)skip : (int)skip;            // rule 1 (first/LAST) + rule 2 (ignore skip)
        return idx >= 0 && idx < positions.Count ? positions[idx] : 0;          // exhausted matches → 0 (rule 3)
    }

    /// <summary>TRIM (§15.96.4): the argument with LEADING (<paramref name="mode"/> 1), TRAILING (2), or BOTH
    /// (0) characters that match the delete set removed. The delete set is each argument-2's single character
    /// (§15.96.3 rule 2); with no argument-2 it is a space (rule 3.a). An argument consisting only of delete-set
    /// characters (or of zero length) returns a zero-length string (rule 4).</summary>
    public static string Trim(string s, long mode, params string[] chars)
    {
        char[] set = chars.Length == 0 ? [' '] : chars.Where(c => c.Length > 0).Select(c => c[0]).ToArray();
        if (set.Length == 0) set = [' '];
        return mode switch
        {
            1 => s.TrimStart(set),   // LEADING (rule 1)
            2 => s.TrimEnd(set),     // TRAILING (rule 2)
            _ => s.Trim(set),        // both (rule 3)
        };
    }
}
