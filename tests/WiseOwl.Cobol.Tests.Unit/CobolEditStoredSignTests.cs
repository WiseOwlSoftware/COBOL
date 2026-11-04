// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The editing sign follows the value the receiver ENDS UP HOLDING, never the sending value (kb/Work PB3664).
/// ISO §13.18.40.5 5): "Table 8, Results of fixed insertion editing, shows the character(s) produced by an editing
/// sign control symbol, depending on the value of the data item" — its first column is "Positive or zero value".
/// §14.9.25.4 6) sends a numeric-edited receiver to §14.6.8.2 5) ("further alignment, zero fill or truncation, and
/// transfer of digits take place as described in the general rules and editing rules in 13.18.40"), so a sending
/// -0.001 into an item with two fraction digits stores ZERO (§8.8.4.2.4 says the same of comparisons: "Zero is
/// considered a unique value regardless of the sign"). Every expected string is hand-derived from those rules.
/// </summary>
public sealed class CobolEditStoredSignTests
{
    [Theory]
    // A negative sender truncated to zero at the LOW end: every fixed sign shows its zero-or-positive column.
    [InlineData("-9.99", -1, 3, " 0.00")]
    [InlineData("+9.99", -1, 3, "+0.00")]
    [InlineData("9.99CR", -1, 3, "0.00  ")]
    [InlineData("9.99DB", -1, 3, "0.00  ")]
    [InlineData("9.99-", -1, 3, "0.00 ")]
    // A negative sender truncated to zero at the HIGH end (-100 into two integer digits holds 00).
    [InlineData("-99", -100, 0, " 00")]
    [InlineData("99-", -100, 0, "00 ")]
    [InlineData("99CR", -100, 0, "00  ")]
    // The floating sign strings read the same sign (rule 6 a): zero lands the positive-or-zero character.
    [InlineData("--9.99", -1, 3, "  0.00")]
    [InlineData("++9.99", -1, 3, " +0.00")]
    // Zero suppression beside a fixed sign (the fz05181 shape: -Z(7)9.99).
    [InlineData("-ZZZZZZZ9.99", -92, 6, "        0.00")]
    // A value that stays nonzero after truncation keeps its sign.
    [InlineData("-9.99", -1, 2, "-0.01")]
    [InlineData("9.99CR", -1, 2, "0.01CR")]
    [InlineData("--9.99", -1, 2, " -0.01")]
    [InlineData("-99", -101, 0, "-01")]
    public void Format_SignIsTheStoredValues(string picture, long value, int scale, string expected) =>
        Assert.Equal(expected, CobolEdit.Format(value, scale, picture));

    // ISO §13.18.40.5 7) b) and 6) b): "the value of the data to be stored" is zero when the digits the picture HOLDS
    // are all zero, so a value whose high-order digits truncate to zero is all asterisks (the point kept) or all
    // spaces (kb/Work PB4515). A stored nonzero value keeps rule 7 a)'s suppression to the left of the point.
    [Theory]
    [InlineData("***.**", 1000, 0, "***.**")]
    [InlineData("ZZZ.ZZ", 1000, 0, "      ")]
    [InlineData("++++.++", 1000, 0, "       ")]
    [InlineData("----.--", 1000, 0, "       ")]
    [InlineData("*,***.**", 10000, 0, "*****.**")]
    [InlineData("***.**", 10005, 1, "***.50")]
    [InlineData("ZZZ.ZZ", 10005, 1, "   .50")]
    [InlineData("***.**", 0, 0, "***.**")]
    public void Format_AllSuppressedPicture_TestsTheStoredDigits(string picture, long value, int scale, string expected) =>
        Assert.Equal(expected, CobolEdit.Format(value, scale, picture));

    // ISO §14.9.25.4 6) d) 1: de-editing "establishes the operand's numeric value, which may be signed" — the sign a
    // floating string landed (Table 9: '-' for a negative value of either string, '+' only for a positive or zero '+' string), read
    // at the position, for either symbol (kb/Work PB4514).
    [Theory]
    [InlineData("++++9.99", "-1234.50", -123450)]
    [InlineData("++++9.99", "+1234.50", 123450)]
    [InlineData("++++9.99", "   -5.25", -525)]
    [InlineData("++++9.99", "   +0.00", 0)]
    [InlineData("----9.99", "-1234.50", -123450)]
    [InlineData("----9.99", " 1234.50", 123450)]
    [InlineData("----9.99", "   -5.25", -525)]
    public void DeEdit_FloatingSignString_ReadsTheLandedSign(string mask, string image, long expected) =>
        Assert.Equal((Int128)expected, CobolEdit.DeEdit(image, mask));

    // §13.18.40.5 rule 6: a PICTURE EDITING character-1 used as a simple insertion symbol EMBEDDED in a floating
    // string is part of the string, so the landed sign can sit on it (PIC --T--9 holding -555 is "  -555"). The
    // de-edit reads that sign too (PB4514 review round 1, R1-1); the round trip pins Format against DeEdit.
    [Theory]
    [InlineData("--T--9", -555)]
    [InlineData("--T--9", 555)]
    [InlineData("++T++9", -555)]
    [InlineData("++T++9", 555)]
    [InlineData("--T--9", -1555)]
    [InlineData("++T++9", 0)]
    [InlineData("--T--9", -555, "-")]
    [InlineData("--T--9", 555, "-")]
    [InlineData("++T++9", -555, "-")]
    [InlineData("++T++9", 555, "-")]
    [InlineData("--T--9", -1555, "-")]
    [InlineData("--T--9", 1555, "-")]
    [InlineData("++T++9", -1555, "-")]
    [InlineData("++T++9", 1555, "-")]
    public void DeEdit_SignLandedOnEmbeddedInsertionCharacter1_RoundTrips(string mask, long value, string literal = ":")
    {
        // A literal that is itself '-' (PB4514 round 2, R2-1) is the landed sign while no digit of the string
        // precedes it, and the literal once one does (rule 6 a: spaces and the sign before the first nonzero digit).
        CobolEdit.EditRule[] rules = [new CobolEdit.EditRule('T', literal, literal, SimpleInsertion: true, Floating: false)];
        string image = CobolEdit.Format(value, 0, mask, edits: rules);
        Assert.Equal((Int128)value, CobolEdit.DeEdit(image, mask, edits: rules));
    }

    [Fact]
    public void Format_FixedExtendedSign_FollowsTheStoredValue()
    {
        // Table 8 for a PICTURE EDITING character-1: FOR NEGATIVE "DEBIT " (positive side defaults to spaces, SR12c).
        CobolEdit.EditRule[] rules = [new CobolEdit.EditRule('L', "DEBIT ", "      ", SimpleInsertion: false, Floating: false)];
        Assert.Equal("      00", CobolEdit.Format(-100, 0, "L99", edits: rules));
        Assert.Equal("      00", CobolEdit.Format(-1, 3, "L99", edits: rules));
        Assert.Equal("DEBIT 01", CobolEdit.Format(-1, 0, "L99", edits: rules));
    }

    [Fact]
    public void Format_FloatingExtendedSign_FollowsTheStoredValue()
    {
        // Table 9: the floating literal lands once (GR14 'es': literal plus one character per repetition).
        CobolEdit.EditRule[] rules = [new CobolEdit.EditRule('L', "DEBIT ", "      ", SimpleInsertion: false, Floating: true)];
        Assert.Equal("        0", CobolEdit.Format(-1, 3, "LLL9", edits: rules));
        Assert.Equal("  DEBIT 1", CobolEdit.Format(-1, 0, "LLL9", edits: rules));
    }

    [Fact]
    public void FormatLocale_PlusSign_FollowsTheStoredValue()
    {
        // GR18 '+' under the invariant locale: a negative layout is the parenthesized one (CobolLocaleEditTests),
        // and a sending -0.0001 into two fraction digits stores zero, which takes the nonnegative layout.
        Assert.Equal(CobolLocaleEdit.Format(0, 2, "+$Z9.99", "", 10), CobolLocaleEdit.Format(-1, 4, "+$Z9.99", "", 10));
        // The same at the HIGH end: -100 into two integer digits holds 00.
        Assert.Equal(CobolLocaleEdit.Format(0, 0, "+$Z9", "", 10), CobolLocaleEdit.Format(-100, 0, "+$Z9", "", 10));
        Assert.DoesNotContain("(", CobolLocaleEdit.Format(-1, 4, "+$Z9.99", "", 10));
        Assert.Contains("(", CobolLocaleEdit.Format(-1, 2, "+$Z9.99", "", 10));
    }
}
