// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB598 — WHICH SUBJECT the §13.18.63.3 SR4 / SR5 / SR10 SIZE sentences bound, at every edition.
///
/// <para>Each of those three rules is a sentence PAIR (SR4 alphanumeric, SR5 national, SR10 boolean): sentence 1
/// is a CLASS rule over "the item" / "the subject of the entry", and sentences 2–3 are a SIZE rule that names
/// exactly two subjects — "an elementary item" (bounded by "the size indicated by an <b>explicit</b> PICTURE
/// clause") and a group item (bounded by "the size of the group item"). A CONDITION-NAME is neither: a Format-3
/// entry is <c>88 condition-name-1 value-clause .</c> (§13.16.2), which admits no PICTURE clause, and §13.18.63.3
/// SR33 makes that level-88 entry the subject; §8.5.1.3.2 item 3 gives a condition-name entry "no true concept of
/// level", so it is neither of §8.5.1.3.1's record subdivisions; and §13.18.63.4 GR19 gives it its conditional
/// variable's characteristics only IMPLICITLY — which is what "explicit" excludes. The three level-88 call sites
/// in <c>DataBinder.BindCondition</c> nevertheless handed the screen the CONDITIONAL VARIABLE's size, so all three
/// arms rejected legal source.</para>
///
/// <para>⛔ THIS CLASS HOLDS BOTH ARMS OF THE DISPATCH IN ONE PLACE — the withheld size for a condition-name
/// subject AND the size that must STILL be measured for an elementary and a group subject (kb/Work PB206). A fix
/// that over-withheld would pass the first half and fail the second, which is the [[two_arm_dispatch]] proof this
/// file exists to keep standing. The runtime BEHAVIOUR the standard defines for an oversize condition-name literal
/// (permanently false; <c>SET … TO TRUE</c> truncates to the right) is pinned by the corpus goldens
/// <c>85/pb598_condition_name_value_size</c> and <c>2002/pb598_condition_name_value_size_national_bit</c>; this
/// class pins the EDITION axis those two single-edition goldens cannot.</para>
/// </summary>
public sealed class ConditionNameValueSizeTests
{
    private static string Program(string decls, string body) =>
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB598T.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
        + decls + "\nPROCEDURE DIVISION.\n" + body + "\n    STOP RUN.\n";

    private const string Alnum = "01 XV PIC X.\n   88 XC VALUE \"cd\".\n   88 XR VALUE \"aa\" THRU \"zz\".";
    private const string National = "01 NV PIC N(2).\n   88 NC VALUE N\"ABC\".";
    private const string Boolean = "01 BV PIC 1(2) USAGE BIT.\n   88 BC VALUE B\"101\".";

    /// <summary>§13.18.63.3 SR4 sentence 2 does not reach a Format-3 subject, at ANY edition — the rule is
    /// edition-invariant (no Annex E change touches it), so all four compile and produce the same answer:
    /// §8.8.4.5.3 item 2 sends the comparison to the relation-condition rules, which space-extend the shorter
    /// operand, so a literal longer than the conditional variable never compares equal.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnOversizeAlphanumericConditionNameLiteral_IsLegalAndPermanentlyFalse(int edition)
    {
        string src = Program(Alnum, "    MOVE \"c\" TO XV\n    IF XC DISPLAY \"Y\" ELSE DISPLAY \"N\" END-IF");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, edition);
        Assert.True(ok, detail);
        Assert.Equal("N", stdout.Trim());
    }

    /// <summary>§14.9.39.4 GR6 → §13.18.63.4 GR7 → §14.6.8.5: <c>SET condition-name TO TRUE</c> places the literal
    /// "aligned at the leftmost character position in the data item with space fill or truncation to the right, as
    /// required". The standard DEFINES the oversize store, which a size rule at level 88 would make dead text.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void SetConditionNameToTrue_TruncatesTheOversizeLiteralToTheRight(int edition)
    {
        string src = Program(Alnum, "    SET XC TO TRUE\n    DISPLAY \"[\" XV \"]\"");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, edition);
        Assert.True(ok, detail);
        Assert.Equal("[c]", stdout.Trim());
    }

    /// <summary>SR5's and SR10's size sentences are the same sentence over national and boolean positions, and
    /// they do not reach a condition-name either. National and boolean data are COBOL-2002 introductions, so the
    /// legal band starts at 2002.</summary>
    [Theory]
    [InlineData(2002, National, "NV")]
    [InlineData(2014, National, "NV")]
    [InlineData(2023, National, "NV")]
    [InlineData(2002, Boolean, "BV")]
    [InlineData(2014, Boolean, "BV")]
    [InlineData(2023, Boolean, "BV")]
    public void AnOversizeNationalOrBooleanConditionNameLiteral_IsLegalFrom2002(int edition, string decls, string v)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(decls, "    DISPLAY " + v), edition);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>⛔ THE GATING DIAGNOSTIC, not a size diagnostic. Below 2002 the same source is rejected for the
    /// national / boolean CATEGORY itself (COBOLNET0900), and it must never be rejected for a size rule that does
    /// not apply — an edition gate reported as a syntax-rule violation is the wrong answer twice over.</summary>
    [Theory]
    [InlineData(National)]
    [InlineData(Boolean)]
    public void Below2002_TheSameSourceIsRejectedForTheCategoryGate_NotForASizeRule(string decls)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(decls, "    CONTINUE"), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0898");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET1740");
    }

    /// <summary>⛔ THE OTHER ARM (kb/Work PB206). The SIZE sentences still bind the two subjects they DO name — an
    /// elementary item ("the size indicated by an explicit PICTURE clause", sentence 2) and a group item ("the size
    /// of the group item", sentence 3) — at every edition. Withholding the size from a condition-name must not
    /// withhold it from these; the corpus negatives <c>pb206-value-oversize-elementary</c> and
    /// <c>pb206-group-value-oversize</c> are the same proof on disk, and this is it on the edition axis.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheSizeSentencesStillBindAnElementaryAndAGroupSubject(int edition)
    {
        var (eok, eerrors, _) = EditionHarness.CompileFull(
            Program("01 E1 PIC X(2) VALUE \"ABCD\".", "    DISPLAY E1"), edition);
        Assert.False(eok);
        EditionHarness.AssertHasDiagnostic(eerrors, "COBOLNET1740");

        var (gok, gerrors, _) = EditionHarness.CompileFull(
            Program("01 GZ VALUE \"ABCDEF\".\n   05 O1 PIC X(2).\n   05 O2 PIC X(2).", "    DISPLAY O1"), edition);
        Assert.False(gok);
        EditionHarness.AssertHasDiagnostic(gerrors, "COBOLNET1740");
    }

    /// <summary>And the national / boolean twins of that same other arm, over their own positions (SR5 / SR10
    /// sentence 2), from the edition their category exists.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheNationalAndBooleanSizeSentencesStillBindAnElementarySubject(int edition)
    {
        var (nok, nerrors, _) = EditionHarness.CompileFull(
            Program("01 N1 PIC N(2) VALUE N\"ABC\".", "    DISPLAY N1"), edition);
        Assert.False(nok);
        EditionHarness.AssertHasDiagnostic(nerrors, "COBOLNET0898");

        var (bok, berrors, _) = EditionHarness.CompileFull(
            Program("01 B1 PIC 1(2) USAGE BIT VALUE B\"101\".", "    DISPLAY B1"), edition);
        Assert.False(bok);
        EditionHarness.AssertHasDiagnostic(berrors, "COBOLNET0898");
    }

    /// <summary>The CLASS half of SR4 / SR5 / SR10 is untouched and still binds a condition-name: SR4 and SR5 are
    /// ALL FORMATS rules and SR24 ("Syntax rules 10 and 17 above apply") carries SR10 into Format 3. Only the SIZE
    /// half was withheld — a fix that dropped the whole subject would pass every test above and this one is what
    /// catches it.</summary>
    [Theory]
    [InlineData(2023, "01 XV PIC X.\n   88 XC VALUE N\"AB\".")]
    [InlineData(2023, "01 NV PIC N(2).\n   88 NC VALUE \"ab\".")]
    [InlineData(2023, "01 BV PIC 1(2) USAGE BIT.\n   88 BC VALUE \"ab\".")]
    [InlineData(2002, "01 NV PIC N(2).\n   88 NC VALUE B\"10\".")]
    public void TheClassHalfStillBindsAConditionName(int edition, string decls)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(decls, "    CONTINUE"), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0898");
    }
}
