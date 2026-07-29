// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// PERFORM Format 3 (exception-checking, ISO §14.9.28 Format 3, COBOL-2023) conformance tests: the §14.9.28.3
/// syntax rules + cross-statement bans (COBOLNET1597-1617), the COBOLNET0900 introduction gate, and the LANDED
/// runtime interceptor (the pc-range interceptor — at 2023 a well-formed F3 PERFORM compiles clean and runs, in a
/// program AND inside an OO method [design §9.10]; the open-mode WHEN operand + F3-in-a-method sub-GAPs are LANDED).
/// The runtime BEHAVIOUR is verified in PerformFormat3BehaviorTests (program) + PerformFormat3MethodBehaviorTests
/// (method); the frame-stack mechanics in PerformFrameStackTests.
/// See docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md §9.
/// </summary>
public sealed class PerformFormat3Tests
{
    // A working-storage-only program with two single-digit items.
    private static string Prog(string body) =>
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n" +
        "01 N PIC 9 VALUE 0.\n01 M PIC 9 VALUE 0.\nPROCEDURE DIVISION.\nMAIN.\n" + body + "\n";

    // A program with two LINE SEQUENTIAL files F1/F2 (for the file-operand and imp-1 file-statement bans).
    private static string ProgFiles(string body) =>
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3F.\nENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
        "  SELECT F1 ASSIGN \"f1.dat\" ORGANIZATION LINE SEQUENTIAL.\n" +
        "  SELECT F2 ASSIGN \"f2.dat\" ORGANIZATION LINE SEQUENTIAL.\n" +
        "DATA DIVISION.\nFILE SECTION.\nFD F1.\n01 R1 PIC X(4).\nFD F2.\n01 R2 PIC X(4).\n" +
        "WORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\nPROCEDURE DIVISION.\nMAIN.\n" + body + "\n";

    private static void AssertRejects(string source, string code)
    {
        var (ok, diag) = EditionHarness.Compile(source, 2023);
        Assert.False(ok, "expected a compile error " + code + " but the program compiled");
        EditionHarness.AssertHasDiagnostic(diag, code);
    }

    // ── §14.9.28.3 structural + operand syntax rules ──

    [Fact] // PF3-STRUCT-WHEN-REQUIRED
    public void NoWhenPhrase_Rejected1597() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    FINALLY DISPLAY \"f\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1597");

    [Fact] // PF3-SR15 — a duplicate exception-name without a distinct file-name
    public void DuplicateExceptionName_Rejected1600() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"a\"\n    WHEN EC-SIZE DISPLAY \"b\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1600");

    [Fact] // PF3-SR15 — a BARE exception-name coexisting with a FILE-paired instance of the SAME name (a bare
           //           occurrence is not "in conjunction with a file-name", so the repeat is illegal).
    public void BareAndFilePairedSameName_Rejected1600() =>
        AssertRejects(ProgFiles("    PERFORM\n        ADD 1 TO N\n    WHEN EC-I-O-PERMANENT-ERROR DISPLAY \"a\"\n    WHEN EC-I-O-PERMANENT-ERROR FILE F1 DISPLAY \"b\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1600");

    [Fact] // PF3-SR16 — a FILE-paired exception-name must begin EC-I-O
    public void FilePairedNonIoName_Rejected1601() =>
        AssertRejects(ProgFiles("    PERFORM\n        ADD 1 TO N\n    WHEN EC-BOUND-SUBSCRIPT FILE F1 DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1601");

    [Fact] // PF3-SR14 — a bare file-name in more than one WHEN
    public void DuplicateBareFileName_Rejected1599() =>
        AssertRejects(ProgFiles("    PERFORM\n        ADD 1 TO N\n    WHEN EXCEPTION F1 DISPLAY \"a\"\n    WHEN EXCEPTION F1 DISPLAY \"b\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1599");

    [Fact] // an unknown exception-name operand → the reused COBOLNET0711
    public void UnknownExceptionName_Rejected0711() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-NO-SUCH-NAME DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET0711");

    // ── Cross-statement bans ──

    [Fact] // XS-EXIT-PERFORM-CYCLE (region B) — plain EXIT PERFORM is legal; CYCLE is not
    public void ExitPerformCycle_Rejected1604() =>
        AssertRejects(Prog("    PERFORM\n        EXIT PERFORM CYCLE\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1604");

    [Fact] // XS-GOTO (region C — a WHEN phrase)
    public void GoToInWhen_Rejected1608() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE GO TO SKIP\n    END-PERFORM.\n    STOP RUN.\nSKIP.\n    CONTINUE."), "COBOLNET1608");

    [Fact] // XS-RESUME-OPERAND — RESUME in a WHEN shall specify NEXT STATEMENT, not AT procedure-name
    public void ResumeAtProcInWhen_Rejected1610() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE RESUME AT SKIP\n    END-PERFORM.\n    STOP RUN.\nSKIP.\n    CONTINUE."), "COBOLNET1610");

    [Fact] // XS-RAISE — RAISE only in imperative-statement-1 (region D bans imp-2..5)
    public void RaiseInWhen_Rejected1611() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE RAISE EXCEPTION EC-BOUND-SUBSCRIPT\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1611");

    [Fact] // XS-CLOSE-MULTI (region A — imperative-statement-1 only)
    public void MultiFileCloseInImp1_Rejected1612() =>
        AssertRejects(ProgFiles("    PERFORM\n        CLOSE F1 F2\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1612");

    [Fact] // XS-INITIALIZE-DUP (region A)
    public void InitializeDupInImp1_Rejected1614() =>
        AssertRejects(Prog("    PERFORM\n        INITIALIZE N N\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1614");

    [Fact] // XS-OPEN-DUP (region A)
    public void OpenDupInImp1_Rejected1616() =>
        AssertRejects(ProgFiles("    PERFORM\n        OPEN INPUT F1 F1\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1616");

    [Fact] // XS-SORT (region A) — any SORT in imperative-statement-1
    public void SortInImp1_Rejected1617()
    {
        string src =
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3SORT.\n" +
            "ENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
            "  SELECT F1 ASSIGN \"f1\" ORGANIZATION LINE SEQUENTIAL.\n" +
            "  SELECT F2 ASSIGN \"f2\" ORGANIZATION LINE SEQUENTIAL.\n" +
            "  SELECT SF ASSIGN \"sf\".\n" +
            "DATA DIVISION.\nFILE SECTION.\nFD F1.\n01 R1 PIC X(4).\nFD F2.\n01 R2 PIC X(4).\n" +
            "SD SF.\n01 SR.\n   03 SK PIC X(4).\n" +
            "WORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\n" +
            "PROCEDURE DIVISION.\nMAIN.\n" +
            "    PERFORM\n        SORT SF ON ASCENDING KEY SK USING F1 GIVING F2\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN.\n";
        AssertRejects(src, "COBOLNET1617");
    }

    [Fact] // XS-VALIDATE-MULTI (region B) — VALIDATE naming >1 identifier
    public void MultiIdentifierValidate_Rejected1607() =>
        AssertRejects(Prog("    PERFORM\n        VALIDATE N M\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET1607");

    [Fact] // XS-RESUME-PLACEMENT — RESUME NEXT STATEMENT in imperative-statement-1 (not a WHEN phrase) → 0712
    public void ResumeNextInImp1_Rejected0712() =>
        AssertRejects(Prog("    PERFORM\n        RESUME NEXT STATEMENT\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), "COBOLNET0712");

    [Fact] // XS-GOTO — region C includes WHEN OTHER (not just the ordinary WHEN)
    public void GoToInWhenOther_Rejected1608() =>
        AssertRejects(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    WHEN OTHER GO TO SKIP\n    END-PERFORM.\n    STOP RUN.\nSKIP.\n    CONTINUE."), "COBOLNET1608");

    // ── Region discrimination: the imp-1-only bans do NOT fire in a WHEN body; the imp-2..5 bans do NOT fire in imp-1. ──

    [Fact]
    public void MultiFileCloseInWhenBody_Accepted_NoBan()
    {
        var (_, diag) = EditionHarness.Compile(
            ProgFiles("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE CLOSE F1 F2\n    END-PERFORM.\n    STOP RUN."), 2023);
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET1612");   // CLOSE-multi is region A (imp-1) only
    }

    [Fact]
    public void RaiseInImp1_Accepted_NoBan()
    {
        var (_, diag) = EditionHarness.Compile(
            Prog("    PERFORM\n        RAISE EXCEPTION EC-BOUND-SUBSCRIPT\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), 2023);
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET1611");   // RAISE is legal in imperative-statement-1
    }

    // ── Version gate (COBOLNET0900) + the staged-runtime disposition (COBOLNET0899). ──

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void BelowIntroduction_Rejected0900(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
        // Below 2023 the introduction gate (0900) fires; the staged-runtime 0899 is suppressed (mutual exclusivity).
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET0899");
    }

    // ── Landed runtime: at 2023 a well-formed F3 PERFORM compiles clean (the pc-range interceptor, §9 — the 0899
    //    program-path staging is lifted; behaviour is verified in PerformFormat3BehaviorTests). ──

    [Fact]
    public void At2023_ExceptionCheckingPerform_CompilesClean()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), 2023);
        Assert.True(ok, "the exception-checking PERFORM runtime has landed — it compiles at 2023: " + string.Join("\n", diag));
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET0899");
    }

    [Fact]
    public void WellFormedF3_At2023_CompilesClean_NoSyntaxRuleFires()
    {
        // A fully well-formed F3 PERFORM (no SR / cross-statement-ban violation) compiles clean at 2023 — none of the
        // §14.9.28.3 rules fire on legal source, and the runtime interceptor has landed (no 0899).
        var (ok, diag) = EditionHarness.Compile(
            Prog("    PERFORM WITH LOCATION\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    WHEN OTHER EXCEPTION DISPLAY \"o\"\n    FINALLY DISPLAY \"f\"\n    END-PERFORM.\n    STOP RUN."), 2023);
        Assert.True(ok, string.Join("\n", diag));
        foreach (var code in new[] { "0899", "1597", "1598", "1599", "1600", "1601", "1604", "1608", "1610", "1611" })
            EditionHarness.AssertNoDiagnostic(diag, "COBOLNET" + code);
    }

    // ── Continuity: a paragraph named LOCATION + `PERFORM LOCATION` (out-of-line) compiles below 2023. ──

    [Theory]
    [InlineData(85)]
    [InlineData(2014)]
    public void PerformLocationParagraph_CompilesBelow2023(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3LOC.\nPROCEDURE DIVISION.\nMAIN.\n    PERFORM LOCATION.\n    STOP RUN.\nLOCATION.\n    CONTINUE.\n", edition);
        Assert.True(ok, string.Join("\n", diag));
    }
}
