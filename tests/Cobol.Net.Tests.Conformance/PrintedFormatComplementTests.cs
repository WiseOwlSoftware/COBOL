// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE COMPLEMENT OF A CLAUSE'S PRINTED GENERAL FORMATS IS REFUSED, AND REFUSED AT COMPILE TIME
/// (kb/Work PB412, PB421).
///
/// <para>Two grammar rules were written as the UNION of their clause's general formats with every element
/// optional, and nothing subtracted the complement. What made each a defect was not the over-acceptance but the
/// STAGE at which the shape was finally answered: <c>GO TO A B.</c> reached the Format-1 bind arm, which read
/// the first procedure-name and DISCARDED the rest without a diagnostic, and both
/// <c>GO TO DEPENDING ON X.</c> and <c>MOVE A CORRESPONDING G1 TO G2.</c> compiled clean and aborted at run
/// time on a loud stage that told the user a COBOL FEATURE was unimplemented — about source the standard has no
/// format for. §4.2.2's first paragraph fixes what may be accepted ("An implementation shall accept the syntax
/// and provide the functionality for all standard language elements required by this Working Draft
/// International Standard and the optional or processor-dependent language elements for which support is
/// claimed"), and a construct no general format prints is neither.</para>
///
/// <para><b>The formats, read from the printed figures</b> (canonical PDF page 660 / printed folio 630 for
/// GO TO, page 694 for MOVE, because a general-format diagram is load-bearing — CLAUDE.md rule 1):
/// <list type="bullet">
///   <item>§14.9.17.2 Format 1 <c>GO TO procedure-name-1</c>; Format 2
///         <c>GO TO { procedure-name-1 } … DEPENDING ON identifier-1</c>. GO and DEPENDING are underlined; TO
///         and ON are not, so §5.2.3 makes them optional words.</item>
///   <item>§14.9.25.2 Format 1 <c>MOVE { identifier-1 | literal-1 } TO { identifier-2 } …</c>; Format 2
///         <c>MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4</c>. Both put the whole sending
///         specification directly after the verb.</item>
/// </list></para>
///
/// <para>⚠ EVERY ROW RUNS AT ALL FOUR EDITIONS. Neither clause's formats changed shape across
/// 1985/2002/2014/2023, so an edition-gated rejection would be wrong in both directions, and the old defects
/// were measured accepted at every edition probed. The one arm that IS edition-dependent — the ANSI-85
/// target-less <c>GO TO.</c>, which §14.9.17.2 prints no format for — keeps its named removal diagnostic rather
/// than becoming a parse error, and is pinned here too.</para>
/// </summary>
public sealed class PrintedFormatComplementTests
{
    private const string GoToShape = "COBOLNET2172";
    private const string MoveCorrPosition = "COBOLNET2173";

    /// <summary>A main program whose PROCEDURE DIVISION is <paramref name="body"/>, with the two groups and the
    /// scalars the MOVE rows need. Paragraph P-A / P-B exist so a GO TO row is about SYNTAX alone and never
    /// about an unresolvable procedure-name.</summary>
    private static string Prog(string pid, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9 VALUE 1.
        01 A PIC X(4) VALUE "ABCD".
        01 B PIC X(4).
        01 G1.
           05 F1 PIC X(3) VALUE "XYZ".
        01 G2.
           05 F1 PIC X(3).
        PROCEDURE DIVISION.
        MAIN-PARA.
            {body}.
        P-A.
            DISPLAY "A".
            STOP RUN.
        P-B.
            DISPLAY "B".
            STOP RUN.

        """;

    // ── GO TO (§14.9.17.2) ──

    /// <summary>Every spelling the two printed formats admit — the over-rejection guard for the narrowing.
    /// TO omitted (§5.2.3), ON omitted, Format 2 with one procedure-name and with three.</summary>
    [Theory]
    [InlineData("PBGT01", "GO TO P-A")]
    [InlineData("PBGT02", "GO P-A")]
    [InlineData("PBGT03", "GO TO P-A DEPENDING ON W-N")]
    [InlineData("PBGT04", "GO TO P-A DEPENDING W-N")]
    [InlineData("PBGT05", "GO TO P-A P-B DEPENDING ON W-N")]
    [InlineData("PBGT06", "GO P-A P-B DEPENDING ON W-N")]
    public void EverySpellingTheGoToFormatsAdmit_CompilesAtEveryEdition(string pid, string body)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Prog(pid + edition, body), edition);
            Assert.True(ok, $"--std {edition}: {string.Join("\n", diagnostics)}");
        }
    }

    /// <summary>The complement: more than one procedure-name with no DEPENDING phrase (only Format 2 prints the
    /// list, and its DEPENDING is underlined — §5.2.2), and a DEPENDING phrase with no procedure-name (Format
    /// 2's <c>{ procedure-name-1 } …</c> is a brace group, so §5.2.6.3 requires one). Refused at all four
    /// editions, by NAME — a bare COBOL0001 would leave a reader unable to tell a compiler limitation from a
    /// syntax mistake.</summary>
    [Theory]
    [InlineData("PBGT10", "GO TO P-A P-B")]
    [InlineData("PBGT11", "GO P-A P-B")]
    [InlineData("PBGT12", "GO TO DEPENDING ON W-N")]
    [InlineData("PBGT13", "GO TO DEPENDING W-N")]
    public void AGoToShapeNeitherFormatPrints_IsRefusedAtEveryEdition(string pid, string body)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Prog(pid + edition, body), edition);
            Assert.False(ok, $"--std {edition}: accepted a shape §14.9.17.2 prints no format for");
            EditionHarness.AssertHasDiagnostic(diagnostics, GoToShape);
        }
    }

    /// <summary>⛔ THE EDITION-GATED ARM IS NOT A PARSE ERROR. The target-less <c>GO TO.</c> is ANSI
    /// X3.23-1985's alterable form, deleted by ISO/IEC 1989:2002 — so at 85 it COMPILES, and at 2002+ it draws
    /// the named removal diagnostic (COBOLNET0811) instead of the no-viable-alternative the narrowing would
    /// otherwise have produced. The four-compilers rule wants the EDITION named, which is why that third
    /// alternative stayed in the grammar rather than being deleted with the rest of the complement.</summary>
    [Fact]
    public void TheAnsi85TargetLessGoTo_KeepsItsNamedEditionGate()
    {
        const string body = "GO TO";
        var (ok85, d85) = EditionHarness.Compile(Prog("PBGT20", body), 85);
        Assert.True(ok85, string.Join("\n", d85));

        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, diagnostics) = EditionHarness.Compile(Prog("PBGT21" + edition, body), edition);
            Assert.False(ok, $"--std {edition}: the target-less GO TO was removed by ISO/IEC 1989:2002");
            EditionHarness.AssertHasDiagnostic(diagnostics, "COBOLNET0811");
            EditionHarness.AssertNoDiagnostic(diagnostics, GoToShape);   // gated by name, not by shape
        }
    }

    // ── MOVE (§14.9.25.2) ──

    /// <summary>Both printed formats, in every spelling — the over-rejection guard for deleting
    /// <c>moveReceivingPhrase</c>'s second alternative.</summary>
    [Theory]
    [InlineData("PBMV01", "MOVE A TO B")]
    [InlineData("PBMV02", "MOVE A TO B, B")]
    [InlineData("PBMV03", "MOVE \"Q\" TO B")]
    [InlineData("PBMV04", "MOVE FUNCTION UPPER-CASE(\"ab\") TO B")]
    [InlineData("PBMV05", "MOVE CORRESPONDING G1 TO G2")]
    [InlineData("PBMV06", "MOVE CORR G1 TO G2")]
    public void EverySpellingTheMoveFormatsAdmit_CompilesAtEveryEdition(string pid, string body)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Prog(pid + edition, body), edition);
            Assert.True(ok, $"--std {edition}: {string.Join("\n", diagnostics)}");
        }
    }

    /// <summary>⛔ ALL FOUR SPELLINGS OF THE ONE OVER-ACCEPTANCE (feedback_two_arm_dispatch). The deleted
    /// alternative spelled its keyword <c>(CORRESPONDING | CORR)</c> and the sending position admits a literal
    /// as well as an identifier, so the shape had four spellings; a repair that reached only the first would
    /// leave three compiling and dying at run time.</summary>
    [Theory]
    [InlineData("PBMV10", "MOVE A CORRESPONDING G1 TO G2")]
    [InlineData("PBMV11", "MOVE A CORR G1 TO G2")]
    [InlineData("PBMV12", "MOVE \"Q\" CORRESPONDING G1 TO G2")]
    [InlineData("PBMV13", "MOVE \"Q\" CORR G1 TO G2")]
    public void ACorrespondingPhraseAfterASendingOperand_IsRefusedAtEveryEdition(string pid, string body)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Prog(pid + edition, body), edition);
            Assert.False(ok, $"--std {edition}: accepted a shape §14.9.25.2 prints no format for");
            EditionHarness.AssertHasDiagnostic(diagnostics, MoveCorrPosition);
        }
    }

    /// <summary>The rest of the MOVE format surface, which localises the divergence to the one deleted
    /// alternative: a literal in a RECEIVING position and a second identifier-4 after Format 2's receiver are
    /// both refused, and were before this change too.</summary>
    [Theory]
    [InlineData("PBMV20", "MOVE A TO \"Q\"")]
    [InlineData("PBMV21", "MOVE CORRESPONDING G1 TO G2 G1")]
    public void TheRestOfTheMoveSurface_StaysRefused(string pid, string body)
        => Assert.False(EditionHarness.Compile(Prog(pid, body), 2023).Ok);
}
