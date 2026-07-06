// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <see cref="CobolBool"/> boolean-operation semantics (Phase-4 track (a) increment 2 — the boolean
/// OPERATORS leg; runtime half, landed ahead of the grammar/binder integration). The oracle is ISO/IEC
/// 1989:2023 Annex A Table A.2 (Examples of boolean operations): <c>1100 B-AND 0101 = 0100</c>,
/// <c>B-OR = 1101</c>, <c>B-XOR = 1001</c>, <c>B-NOT 1100 = 0011</c>. The length rules are §8.8.2 rule 9 (shorter
/// operand right-zero-extended, no error) / rule 10 (result length = the larger operand); equality is
/// §8.8.4.2.8 (zero-extension, two empty operands EQUAL); a simple boolean condition is §8.8.4.3.4 GR1
/// (true iff value 1); the COMPUTE F2 store resize is §14.9.8 GR3 / §14.6.8.6.
/// </summary>
public sealed class CobolBoolTests
{
    // ── Annex A Table A.2 — the canonical oracle ───────────────────────────────────────────────────────────

    [Fact] public void And_TableA2() => Assert.Equal("0100", CobolBool.And("1100", "0101"));
    [Fact] public void Or_TableA2() => Assert.Equal("1101", CobolBool.Or("1100", "0101"));
    [Fact] public void Xor_TableA2() => Assert.Equal("1001", CobolBool.Xor("1100", "0101"));
    [Fact] public void Not_TableA2() => Assert.Equal("0011", CobolBool.Not("1100"));

    // ── Rule 9 (right-zero-extension of the shorter operand) + rule 10 (result = larger length) ─────────────

    [Theory]
    [InlineData("11", "1010", "1010")]     // AND: "11" → "1100"; & "1010" = "1000"? no — 1100 & 1010 = 1000
    [InlineData("1", "1", "1")]
    public void And_UnequalLengths_RightZeroExtends(string a, string b, string _)
    {
        // Verify the extension explicitly rather than trust the inline datum:
        string expected = CobolBool.And(a.PadRight(System.Math.Max(a.Length, b.Length), '0'),
                                        b.PadRight(System.Math.Max(a.Length, b.Length), '0'));
        Assert.Equal(expected, CobolBool.And(a, b));
        Assert.Equal(System.Math.Max(a.Length, b.Length), CobolBool.And(a, b).Length);   // rule 10
    }

    [Fact]
    public void And_ShortenedOperand_ConcreteExample()
    {
        // "11" extends to "1100"; 1100 B-AND 1010 = 1000 (rule 9 right-zero-extension).
        Assert.Equal("1000", CobolBool.And("11", "1010"));
    }

    [Fact]
    public void Or_ShorterRightExtended()
    {
        // "1" → "1000"; 1000 B-OR 0101 = 1101.
        Assert.Equal("1101", CobolBool.Or("1", "0101"));
    }

    [Fact]
    public void ZeroLengthOperands_ProduceZeroLengthResult()   // NOTE 2 (:9418)
    {
        Assert.Equal("", CobolBool.And("", ""));
        Assert.Equal("", CobolBool.Or("", ""));
        Assert.Equal("", CobolBool.Xor("", ""));
        Assert.Equal("", CobolBool.Not(""));
    }

    // ── Equality (§8.8.4.2.2 / §8.8.4.2.8) ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("10", "10", true)]
    [InlineData("10", "1000", true)]    // shorter zero-extends: "10" ≡ "1000"
    [InlineData("10", "100", true)]
    [InlineData("10", "11", false)]
    [InlineData("", "", true)]          // two zero-length operands EQUAL (:9689)
    [InlineData("", "0", true)]         // "" zero-extends to "0"
    [InlineData("", "1", false)]
    public void Equal_ZeroExtends(string a, string b, bool eq) => Assert.Equal(eq, CobolBool.Equal(a, b));

    // ── Simple boolean condition (§8.8.4.3.4 GR1) ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    public void IsTrue_TrueIffValueOne(string a, bool t) => Assert.Equal(t, CobolBool.IsTrue(a));

    // ── COMPUTE F2 receiver resize (§14.9.8 GR3 / §14.6.8.6) ────────────────────────────────────────────────

    [Theory]
    [InlineData("101", 5, "10100")]   // right zero-fill
    [InlineData("10111", 3, "101")]   // right truncate
    [InlineData("101", 3, "101")]     // exact
    [InlineData("101", 0, "")]
    public void Resize_LeftAlignRightFillOrTruncate(string v, int w, string expected) =>
        Assert.Equal(expected, CobolBool.Resize(v, w));

    // ── Figurative ALL "bits" operands (§8.3.3.6.4 GR2 — repeat/truncate to the concrete length) ────────────

    [Fact]
    public void AndAll_PatternRepeatsToConcreteLength()
    {
        // concrete "110011" (len 6); ALL "10" → "101010"; AND = "100010".
        Assert.Equal("100010", CobolBool.AndAll("110011", "10"));
        Assert.Equal(6, CobolBool.AndAll("110011", "10").Length);
    }

    [Fact]
    public void OrAll_And_XorAll_PatternRepeat()
    {
        Assert.Equal("111011", CobolBool.OrAll("110011", "10"));   // | "101010"
        Assert.Equal("011001", CobolBool.XorAll("110011", "10"));  // ^ "101010"
    }

    [Fact]
    public void AllAll_EmptyPattern_IsBooleanZeros()
    {
        Assert.Equal("0000", CobolBool.AndAll("1111", ""));   // ALL of nothing → zeros
        Assert.Equal("1111", CobolBool.OrAll("1111", ""));
    }

    [Fact]
    public void EqualAll_MaterializesPatternToConcreteLength()
    {
        Assert.True(CobolBool.EqualAll("1010", "10"));    // ALL "10" → "1010"
        Assert.False(CobolBool.EqualAll("1011", "10"));
        Assert.True(CobolBool.EqualAll("0000", ""));      // ALL "" → zeros
    }

    // ── Null tolerance (defensive — the emitter never passes null, but the store rules define "" behavior) ──

    [Fact]
    public void NullOperands_TreatedAsEmpty()
    {
        Assert.Equal("", CobolBool.And(null, null));
        Assert.False(CobolBool.IsTrue(null));
        Assert.True(CobolBool.Equal(null, ""));
    }
}
