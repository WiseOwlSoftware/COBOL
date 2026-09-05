// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB297 — WHICH OPERAND sizes a figurative constant in a relation condition, at every edition.
///
/// <para>ISO §8.3.3.6.4 GR2 repeats a figurative "until the size of the resultant string is greater than or equal
/// to the number of character positions in the associated data item, literal, or intermediate result", and its
/// NOTE 1 names comparison as an association ("moved to it, COMPARED WITH IT, or paired with it in a binary
/// operation"). §8.4.3.3.4 GR5 makes a reference-modified operand "a unique data item" whose positions are the
/// SLICE's, so <c>X(1:1) = LOW-VALUE</c> over a <c>PIC X(2)</c> item associates the figurative with ONE position.
/// The compiler sized it from the BASE item's PICTURE instead, and §8.8.4.2.7 rule 2's space extension then made
/// the answer FALSE — silently, and for every figurative except SPACE, whose value IS the pad.</para>
///
/// <para>⛔ THIS CLASS HOLDS THE ARMS A SINGLE GOLDEN CANNOT ([[two_arm_dispatch]]): the figurative on the LEFT as
/// well as the right; a length that exists only at RUNTIME; the length-UNSPECIFIED §8.3.3.6.4 GR3 case where the
/// operand pair has no associated data item at all; and the SPACE case that was right before the fix and must
/// stay right after it. The value behaviour over plain alphanumeric operands is pinned by the corpus golden
/// <c>85/pb297_figurative_refmod_relation</c> and the category/collating arms by
/// <c>2002/pb297_figurative_sizing_categories</c>; this class pins the EDITION axis those two cannot — reference
/// modification, figurative constants and ALL literal-1 are all COBOL-85 constructs and no Annex E change touches
/// §8.3.3.6.4 or §8.4.3.3.4, so every edition shall answer identically.</para>
/// </summary>
public sealed class FigurativeSizingTests
{
    private static string Program(string decls, string body) =>
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB297T.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
        + decls + "\nPROCEDURE DIVISION.\nMAIN.\n" + body + "\n    STOP RUN.\n";

    private static void AssertAnswers(int edition, string decls, string body, string expected)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Program(decls, body), edition);
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.Trim());
    }

    private static string Test(string condition) =>
        $"    IF {condition} DISPLAY \"Y\" ELSE DISPLAY \"N\" END-IF";

    /// <summary>§8.4.3.3.4 GR5 + §8.3.3.6.4 GR2: the associated data item is the SLICE, one character position
    /// wide, so the figurative is one LOW-VALUE and the operands are equal. The whole defect in one line.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ANarrowSliceOfALowValueItem_EqualsLowValue(int edition) =>
        AssertAnswers(edition, "01 X PIC X(2) VALUE LOW-VALUES.", Test("X(1:1) = LOW-VALUE"), "Y");

    /// <summary>The figurative on the LEFT of the relation — the arm a right-hand-side-only fix would miss.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheFigurativeOnTheLeft_SizesToTheSliceToo(int edition) =>
        AssertAnswers(edition, "01 X PIC X(2) VALUE LOW-VALUES.", Test("LOW-VALUE = X(1:1)"), "Y");

    /// <summary>ORDERING, not only equality: two equal-length equal operands are neither less nor greater
    /// (§8.8.4.2.7 rule 1), so a base-width figurative — which made the slice look SHORTER and therefore
    /// space-extended — inverted the answer of every inequality as well.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ANarrowSliceOfAHighValueItem_IsNotLessThanHighValue(int edition) =>
        AssertAnswers(edition, "01 H PIC X(2) VALUE HIGH-VALUES.", Test("H(1:1) < HIGH-VALUE"), "N");

    /// <summary>A leftmost-position and length that are DATA ITEMS: §8.4.3.3.4 GR5 b/c evaluate them at runtime,
    /// so the associated operand's character-position count has no compile-time value at all. No table of
    /// per-operand-kind widths could ever have answered this one — which is why the sizing belongs at runtime.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ARefModWithComputedBounds_SizesTheFigurativeAtRuntime(int edition) =>
        AssertAnswers(edition,
            "01 X PIC X(4) VALUE LOW-VALUES.\n01 I PIC 9 VALUE 2.\n01 L PIC 9 VALUE 2.",
            Test("X(I:L) = LOW-VALUES"), "Y");

    /// <summary>§8.4.3.3.4 GR5 c) "If length is not specified, the unique data item extends from and includes the
    /// position identified by leftmost-position up to and including the rightmost position" — two positions here,
    /// not the item's four.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ARefModWithOmittedLength_SizesToTheRemainder(int edition) =>
        AssertAnswers(edition, "01 X PIC X(4) VALUE LOW-VALUES.", Test("X(3:) = LOW-VALUES"), "Y");

    /// <summary>An <c>ALL literal-1</c> is sized by the same GR2 rule: repeated to the slice's three positions and
    /// truncated from the right — "ab" → "abab" → "aba", which is what A(1:3) holds.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnAllLiteral_IsRepeatedToTheSlicesLength(int edition) =>
        AssertAnswers(edition, "01 A PIC X(4) VALUE \"abab\".", Test("A(1:3) = ALL \"ab\""), "Y");

    /// <summary>⛔ THE LENGTH-UNSPECIFIED CASE. With figuratives on BOTH sides there is no associated data item,
    /// so GR2 does not apply and §8.3.3.6.4 GR3 c) gives each operand "the length of literal-1": "ab" and "aba".
    /// §8.8.4.2.7 rule 2 then space-extends the shorter to "ab ", which differs from "aba" at position 3.
    /// Sizing BOTH operands to one of them — the shape the old anchor-width code produced — answered TRUE.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TwoAllLiterals_KeepTheirOwnLengths(int edition) =>
        AssertAnswers(edition, "01 FILLER PIC X.", Test("ALL \"ab\" = ALL \"aba\""), "N");

    /// <summary>§8.3.3.6.4 GR3 b) "When a figurative constant is other than ALL literal-1, the length of the
    /// string is one character" — the same length-unspecified rule for a bare figurative pair.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TwoBareFiguratives_AreOneCharacterEach(int edition) =>
        AssertAnswers(edition, "01 FILLER PIC X.", Test("SPACE = ALL \"  \""), "Y");

    /// <summary>⛔ THE NO-REGRESSION GUARD, and the reason the defect survived: §8.8.4.2.7 rule 2 extends the
    /// shorter operand "by sufficient alphanumeric SPACES", so a SPACE-valued test came out right under the wrong
    /// sizing and every corpus test written with SPACES was silent about the bug. It must stay right.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ANarrowSliceOfASpaceItem_StillEqualsSpace(int edition) =>
        AssertAnswers(edition, "01 S PIC X(2) VALUE SPACES.", Test("S(1:1) = SPACE"), "Y");

    /// <summary>The UNMODIFIED operand still sizes the figurative to its own full width — the other arm of the
    /// same dispatch, which a fix that simply narrowed every figurative to one character would break.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnUnmodifiedItem_StillSizesTheFigurativeToItsFullWidth(int edition) =>
        AssertAnswers(edition,
            "01 G.\n   05 G1 PIC X(2) VALUE LOW-VALUES.\n   05 G2 PIC X(2) VALUE SPACES.",
            Test("G = LOW-VALUES") + "\n" + Test("G(1:2) = LOW-VALUES"), "N\nY");
}
