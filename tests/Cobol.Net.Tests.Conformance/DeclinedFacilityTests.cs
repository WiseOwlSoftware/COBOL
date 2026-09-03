// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The WITNESSES for the two DECLINED facilities' STATEMENT halves — and the first assertions in this
/// repository that the §4.2.6 recognize-and-warn band fires at all.
///
/// <para>⛔ WHY THESE HAD TO BE WRITTEN AS xUnit FACTS. The negative corpus can only assert a FAILING compile
/// (<c>CorpusRunnerTests.EnabledNegativeCase_RejectsWithItsDiagnostic</c> asserts <c>ok == false</c> and matches
/// the <c>.err</c> code), and the positive corpus compares STDOUT only — the fixture header of
/// <c>2023/wave_h_facilities_inert.cob</c> says in so many words that the warnings "go to the non-failing
/// compile channel (not stdout)". So COBOLNET1560 / 1578 / 1579 / 1580 had **no assertion mechanism of any
/// kind**: a repository-wide search found the four codes only inside SOURCE COMMENTS. Every one of them could
/// have stopped firing and nothing would have gone red (feedback_green_gates_arent_evidence;
/// feedback_reachability_is_measured_not_deduced). <c>EditionHarness.CompileFull</c> returns the warning channel,
/// which is the seam these facts use.</para>
///
/// <para>The A.4 DATA-DIVISION halves need none of this: COBOLNET1708/1709/1710 are ERRORS (Annex A.4.1 — an
/// optional element's syntax is accepted only when support is claimed), so they are witnessed by the ordinary
/// <c>tests/conformance/negative/declined-*</c> corpus.</para>
/// </summary>
public sealed class DeclinedFacilityTests
{
    private const string Prologue = """
                                           IDENTIFICATION DIVISION.
                                           PROGRAM-ID. DCLSTMT.
                                           DATA DIVISION.
                                           WORKING-STORAGE SECTION.
                                           01 WS-REC PIC X(4) VALUE "AB".
                                           PROCEDURE DIVISION.
                                       """;

    /// <summary>ISO §14.9.50, Annex A.4.14 item 9 — the VALIDATE STATEMENT is recognized and NAMED. It is
    /// accepted-inert with a Warning rather than refused, which is the posture PHASE-13 Wave H landed and
    /// <c>docs/CONFORMANCE.md</c> §4 item 3 documents; the assertion here is that the WARNING IS ACTUALLY
    /// EMITTED, once per site, and that the compile still SUCCEEDS.
    /// <para>This is the covering witness for the seventeen §14.9.50 rows (SR-14.9.50.3-1..-6,
    /// GR-14.9.50.4-1..-10, FMT-14.9.50.2) and for the rules that describe what a SUPPORTED clause does inside a
    /// VALIDATE statement and therefore cannot fire at all — GR-13.18.40.4-15/-19 (PICTURE "takes effect during
    /// the format validation stage"), GR-13.18.44.4-3 (REDEFINES under a VALIDATE operand), GR-13.18.60.4-3
    /// (USAGE compatibility checking) and DOC-A.1-86 (the Annex A.1 "Format validation" documentation item,
    /// which A.1's own preamble makes not-required when the feature is not implemented).</para></summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ValidateStatement_DrawsTheNamedNonSupportWarning_AndStillCompiles(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(
            Prologue + "\n           VALIDATE WS-REC.\n           STOP RUN.\n", edition);
        Assert.True(ok, "the VALIDATE statement is accepted-inert (§4.2.6 ¶3), never a compile error: "
            + string.Join("\n", errors));
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1580");
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL. At <c>--std 85</c> VALIDATE is a USER-DEFINED WORD
    /// (§8.9 reserves it "added 2002"), so the statement arm must NOT fire and no COBOLNET1580 may appear — the
    /// same fact <c>conformance:negative/user-word-validate</c> pins from the other side. Without this leg the
    /// theory above would pass just as well against a compiler that warned unconditionally.</summary>
    [Fact]
    public void ValidateAt85_IsAUserWord_AndDrawsNoFacilityWarning()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DCLV85.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 VALIDATE PIC X(4) VALUE "AB".
                   PROCEDURE DIVISION.
                       DISPLAY VALIDATE.
                       STOP RUN.

               """, 85);
        Assert.True(ok, "VALIDATE is a legal user-defined word at COBOL-85 (ISO §8.9)");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET1580");
    }

    /// <summary>ISO §14.9.7 / §14.9.36, Annex A.4.3 items 4 and 5 — the COMMIT and ROLLBACK STATEMENTS are
    /// recognized and NAMED, and behave as CONTINUE (§14.9.7.4 GR1 / §14.9.36.4 GR1: "If this statement is
    /// executed when there is no active APPLY COMMIT clause, then it has the same effect as a CONTINUE
    /// statement"). With COBOLNET1709 refusing every APPLY COMMIT clause, "no active APPLY COMMIT clause" is
    /// the ONLY state this implementation has, so that CONTINUE behaviour is the facility's COMPLETE behaviour.
    /// <para>Covering witness for GR-14.9.7.4-3/-4/-5, GR-14.9.36.4-3/-4/-5/-6 and GR-14.6.11-1 (the implicit
    /// COMMIT at normal run-unit termination, which quantifies over the necessarily-empty set of active APPLY
    /// COMMIT clauses). The runtime half — a bare COMMIT and ROLLBACK actually running as CONTINUE — is
    /// <c>conformance:2023/pb137_commit_inert</c>.</para>
    /// <para>⚠ The count is asserted per SITE, not per program: an emit that moved to a once-per-program guard
    /// would still satisfy "contains COBOLNET1579" and would stop naming the second statement.</para></summary>
    [Fact]
    public void CommitAndRollback_DrawTheNamedNonSupportWarning_OncePerSite()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(
            Prologue + "\n           COMMIT.\n           ROLLBACK.\n           STOP RUN.\n", 2023);
        Assert.True(ok, "COMMIT/ROLLBACK are accepted-inert (§4.2.6 ¶3), never a compile error: "
            + string.Join("\n", errors));
        Assert.Equal(2, warnings.Count(w => w.Contains("COBOLNET1579", StringComparison.Ordinal)));
    }

    /// <summary>The edition arm for commit and rollback: the facility is a COBOL-2023 addition (Annex E.3.2
    /// item 2) and §8.9 reserves COMMIT and ROLLBACK at 2023 only, so at <c>--std 2014</c> the words are
    /// user-defined and no facility warning may appear. The failure this catches is a <c>facilityWord</c>
    /// predicate that stopped consulting the per-edition reservation table.</summary>
    [Fact]
    public void CommitAt2014_IsAUserWord_AndDrawsNoFacilityWarning()
    {
        var (_, _, warnings) = EditionHarness.CompileFull("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DCLC14.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 COMMIT PIC X(4) VALUE "AB".
                   PROCEDURE DIVISION.
                       DISPLAY COMMIT.
                       STOP RUN.

               """, 2014);
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET1579");
    }

    /// <summary>The <c>--permissive</c> arm of the declined-optional-element band. All three codes route
    /// through the ONE <c>EditionContext.Removed</c> severity seam, so the migration mode downgrades them to
    /// warnings and the program compiles with the declined element simply absent — the same seam every removed
    /// construct uses. Asserted on the APPLY COMMIT clause because it is the one whose strict refusal is a
    /// whole I-O-CONTROL clause rather than a data item.</summary>
    [Fact]
    public void DeclinedBand_IsAWarningUnderPermissive()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DCLPERM.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-REC.
                      05 WS-A PIC X(4) DEFAULT IS "AB".
                   PROCEDURE DIVISION.
                       DISPLAY WS-A.
                       STOP RUN.

               """;
        var (okStrict, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(okStrict, "strict: a declined A.4.14 clause is an ERROR (Annex A.4.1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1708");

        var (okPermissive, _, warnings) = EditionHarness.CompileFull(src, 2023, permissive: true);
        Assert.True(okPermissive, "--permissive: the declined clause downgrades through the Removed() seam");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1708");
    }
}
