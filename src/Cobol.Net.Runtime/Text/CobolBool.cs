// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// COBOL boolean-expression value semantics over the D-B1 '0'/'1' character representation (ISO/IEC 1989:2023
/// §8.8.2 boolean expressions; §8.7.2 the operators B-AND / B-OR / B-XOR / B-NOT). A boolean value IS a string of
/// '0'/'1' positions (the same substrate <see cref="CobolString"/> stores category-boolean items on, D-B1), so
/// these operators combine two such strings positionwise. This is the runtime half of Phase-4 track (a)
/// increment 2 (the boolean OPERATORS leg); category-boolean DATA (MOVE / compare / VALUE) rides
/// <see cref="CobolString"/> with the boolean-zero pad.
/// </summary>
/// <remarks>
/// <para><b>Length rules (§8.8.2 rule 9 / rule 10).</b> A binary operation combines its operands positionwise
/// left-to-right, "without regard to usage"; when the operands differ in length the shorter is treated as
/// extended on the RIGHT with boolean zeros — no error, no exception condition (rule 9). The result length is
/// the length of the LARGER operand referenced in that operation (rule 10; <see cref="Not"/> preserves its
/// operand's length). Two zero-length operands combine to a zero-length result (NOTE 2).</para>
/// <para><b>Content.</b> The operators combine per-position with the natural bit logic; a position outside
/// {'0','1'} is undefined-result territory (§14.6.13.2) and is treated as boolean zero here — the
/// EC-DATA-INCOMPATIBLE bridge is named residue.</para>
/// </remarks>
public static class CobolBool
{
    private static char Bit(string s, int i) => i < s.Length && s[i] == '1' ? '1' : '0';

    /// <summary>Conjunction (ISO §8.7.2 B-AND): positionwise AND, shorter operand right-zero-extended, result
    /// length = max (rules 9/10). Example (Annex A Table A.2): <c>1100 B-AND 0101 = 0100</c>.</summary>
    public static string And(string? a, string? b) => Combine(a, b, static (x, y) => x == '1' && y == '1');

    /// <summary>Inclusive disjunction (ISO §8.7.2 B-OR): positionwise OR. Example: <c>1100 B-OR 0101 = 1101</c>.</summary>
    public static string Or(string? a, string? b) => Combine(a, b, static (x, y) => x == '1' || y == '1');

    /// <summary>Exclusive disjunction (ISO §8.7.2 B-XOR): positionwise XOR. Example: <c>1100 B-XOR 0101 = 1001</c>.</summary>
    public static string Xor(string? a, string? b) => Combine(a, b, static (x, y) => x != y);

    /// <summary>Negation (ISO §8.7.2 B-NOT): flip every position, length preserved (rule 10 — the operand's
    /// length). Example: <c>B-NOT 1100 = 0011</c>.</summary>
    public static string Not(string? a)
    {
        a ??= "";
        var arr = new char[a.Length];
        for (int i = 0; i < a.Length; i++) arr[i] = a[i] == '1' ? '0' : '1';
        return new string(arr);
    }

    /// <summary>Equality of two boolean values (ISO §8.8.4.2.2 Format 2 / §8.8.4.2.8): a positionwise VALUE
    /// comparison, usage-independent, the shorter operand right-extended with boolean zeros; two zero-length
    /// operands are EQUAL. (Boolean relations are equality-only — no ordering is defined for class boolean.)</summary>
    public static bool Equal(string? a, string? b)
    {
        a ??= ""; b ??= "";
        int n = System.Math.Max(a.Length, b.Length);
        for (int i = 0; i < n; i++)
            if (Bit(a, i) != Bit(b, i)) return false;
        return true;
    }

    /// <summary>The truth value of a boolean value used as a condition (ISO §8.8.4.3.4 GR1 — a simple boolean
    /// condition is true iff the value is boolean 1). The bind guarantees a length-1 operand (SR1); a longer
    /// value defensively tests position 1.</summary>
    public static bool IsTrue(string? a) => a is { Length: > 0 } && a[0] == '1';

    /// <summary>Resize a boolean value to <paramref name="width"/> positions (ISO §14.9.8 GR3 / §14.6.8.6): the
    /// value is left-aligned, right-zero-filled when shorter, right-truncated when longer — the boolean store
    /// discipline for a COMPUTE Format-2 receiver.</summary>
    public static string Resize(string? v, int width)
    {
        v ??= "";
        if (width <= 0) return "";
        if (v.Length == width) return v;
        return v.Length > width ? v[..width] : v.PadRight(width, '0');
    }

    /// <summary>A binary operation against a figurative <c>ALL "bits"</c> operand (ISO §8.3.3.6.4 GR2): the
    /// pattern <paramref name="bits"/> is repeated/truncated to the concrete operand's length, then combined —
    /// so the concrete side is evaluated ONCE (never double-rendered). The result length is the concrete
    /// operand's length (the ALL side is positionless, rule 10).</summary>
    public static string AndAll(string? concrete, string bits) => CombineAll(concrete, bits, And);
    public static string OrAll(string? concrete, string bits) => CombineAll(concrete, bits, Or);
    public static string XorAll(string? concrete, string bits) => CombineAll(concrete, bits, Xor);

    /// <summary>Equality against a figurative <c>ALL "bits"</c> operand: the pattern materializes to the
    /// concrete operand's length (§8.3.3.6.4 GR2) before the §8.8.4.2.8 compare.</summary>
    public static bool EqualAll(string? concrete, string bits) =>
        Equal(concrete, Fill(bits, (concrete ?? "").Length));

    private static string Combine(string? a, string? b, System.Func<char, char, bool> op)
    {
        a ??= ""; b ??= "";
        int n = System.Math.Max(a.Length, b.Length);
        if (n == 0) return "";
        var arr = new char[n];
        for (int i = 0; i < n; i++) arr[i] = op(Bit(a, i), Bit(b, i)) ? '1' : '0';
        return new string(arr);
    }

    private static string CombineAll(string? concrete, string bits, System.Func<string?, string?, string> op)
    {
        concrete ??= "";
        return op(concrete, Fill(bits, concrete.Length));
    }

    /// <summary>The figurative <c>ALL "bits"</c> pattern repeated (and truncated) to <paramref name="width"/>
    /// positions (ISO §8.3.3.6.4 GR2). An empty pattern yields boolean zeros.</summary>
    private static string Fill(string bits, int width)
    {
        if (width <= 0) return "";
        if (bits.Length == 0) return new string('0', width);
        var sb = new System.Text.StringBuilder(width);
        while (sb.Length < width) sb.Append(bits);
        return sb.ToString()[..width];
    }
}
