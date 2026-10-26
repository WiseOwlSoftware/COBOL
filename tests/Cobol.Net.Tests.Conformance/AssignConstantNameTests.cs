// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A WORD THAT IS BOTH A DECLARED CONSTANT-NAME AND AN IMPLEMENTOR SYSTEM-NAME, WRITTEN WHERE THE ASSIGN CLAUSE ADMITS
/// EITHER (kb/Work PB2500). ISO §12.4.5.1 prints <c>ASSIGN [TO] {device-name-1 | literal-1}</c>; §13.10.3 SR2 puts a
/// constant-name wherever a format specifies a literal; §8.3.2.1 4) lets one word be a system-name and a user-defined
/// word; §12.4.5.2 SR5 leaves "the meaning and rules for the allowable specification of device-name-1 and the value of
/// literal-1" to the implementor. The determination (docs/CONFORMANCE.md §7 DOC-A.1-71): the program's own declaration of
/// the word wins, exactly as it does for the LOCALE clause and the ALPHABET code-name. The corpus golden
/// <c>pb2500_assign_constant_name</c> carries the success and run-time abnormal paths (I-O statuses 00, 05, 10, 35, a USE
/// declarative); these facts carry what a corpus entry cannot — the EXACT wording of each compile-time refusal, the
/// silent recovery from a refused constant entry, and the reach into scopes (a GLOBAL constant of a container, a
/// hexadecimal-alphanumeric constant, a constant that spells a device class).
/// </summary>
public sealed class AssignConstantNameTests
{
    private static string Source(string pid, string constantEntry) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO K.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(3).
        WORKING-STORAGE SECTION.
        01 K CONSTANT AS {constantEntry}.
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    /// <summary>§12.4.5.2 SR4 — "Literal-1 shall be an alphanumeric literal and shall be neither a figurative constant
    /// nor a zero-length literal" — holds the constant, which is "as if literal-1 … were written" (§13.10.4 GR1), to the
    /// same rule: a numeric, national or boolean constant, and a zero-length one, each refused with COBOL2645 naming
    /// the constant and what it stands for, at every edition that has constant entries (2002 introduced them).</summary>
    [Theory]
    [InlineData("PB2500N1", "5", "stands for a numeric literal")]
    [InlineData("PB2500N2", "1.5", "stands for a numeric literal")]
    [InlineData("PB2500N3", "N\"abc\"", "stands for a national literal")]
    [InlineData("PB2500N4", "B\"1\"", "stands for a boolean literal")]
    [InlineData("PB2500N5", "\"\"", "stands for a zero-length literal")]
    public void ConstantOfTheWrongShape_IsRefusedUnderSR4(string pid, string value, string why)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Source(pid + edition, value), edition);
            Assert.False(ok, $"[{pid}] must be REJECTED at --std {edition}");
            EditionHarness.AssertHasDiagnostic(errors, "COBOL2645");
            EditionHarness.AssertHasDiagnostic(errors, "the constant-name 'K'");
            EditionHarness.AssertHasDiagnostic(errors, why);
            EditionHarness.AssertHasDiagnostic(errors, "ISO §12.4.5.2 SR4");
        }
    }

    /// <summary>A constant entry whose own operand was refused (§13.10.3 SR6: a figurative constant is no literal-1 of a
    /// constant entry) reports at the ENTRY; the ASSIGN operand that names it recovers silently — one fault, one report,
    /// never a second COBOL2645 that blames the ASSIGN clause for the entry's error.</summary>
    [Fact]
    public void RefusedConstantEntry_ReportsOnce_AtTheEntry()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Source("PB2500R1", "SPACE"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1547");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL2645");
    }

    /// <summary>A word that names NO constant stays device-name-1 — its own spelling is the physical file's name
    /// (docs/CONFORMANCE.md §7 DOC-A.1-71) — so the substitution is the program's declaration winning, never every word
    /// being looked up. The program below declares a constant NEXT TO the word, not under it.</summary>
    [Fact]
    public void WordNamingNoConstant_IsStillDeviceName1()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2500D1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT W ASSIGN TO PB2500DEV
                    ORGANIZATION IS SEQUENTIAL.
                SELECT R ASSIGN TO "pb2500dev.txt"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD W.
            01 W-REC PIC X(4).
            FD R.
            01 R-REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 OTHER-K CONSTANT AS "other.dat".
            PROCEDURE DIVISION.
                OPEN OUTPUT W
                MOVE "DEV1" TO W-REC
                WRITE W-REC
                CLOSE W
                OPEN INPUT R
                READ R
                DISPLAY R-REC
                CLOSE R
                STOP RUN.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal("DEV1", stdout.Trim());
    }

    /// <summary>The constant is "as if literal-1 … were written" (§13.10.4 GR1), so a hexadecimal-alphanumeric constant
    /// (an alphanumeric literal, §8.3.3.2.2) names the file its characters spell, and a GLOBAL constant of a containing
    /// program (§13.18.27.4 GR1-GR2) is the one a contained program's ASSIGN reads. ONE file, written through the
    /// contained program's constant and read back through a literal in the container.</summary>
    [Fact]
    public void HexadecimalAndGlobalConstants_NameTheirFile()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2500G1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT R ASSIGN TO "k.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD R.
            01 R-REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 GK CONSTANT IS GLOBAL AS X"6B2E646174".
            PROCEDURE DIVISION.
                CALL "PB2500G2"
                OPEN INPUT R
                READ R
                DISPLAY R-REC
                CLOSE R
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2500G2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT W ASSIGN TO GK
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD W.
            01 W-REC PIC X(4).
            PROCEDURE DIVISION.
                OPEN OUTPUT W
                MOVE "GLOB" TO W-REC
                WRITE W-REC
                CLOSE W
                EXIT PROGRAM.
            END PROGRAM PB2500G2.
            END PROGRAM PB2500G1.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal("GLOB", stdout.Trim());
    }

    /// <summary>⛔ THE THIRD EC-I-O ARM OF A CONSTANT-NAMED FILE (kb/Work PB3438): NEITHER a FILE STATUS item NOR a USE
    /// declarative. The corpus golden <c>pb2500_assign_constant_name</c> covers FILE STATUS (MS, status 35) and USE
    /// without FILE STATUS (DC) but cannot hold this arm, because the harness requires exit 0 and this arm ends the run
    /// unit abnormally. §14.6.13.1.3 3): "If the exception condition is a fatal EC-I-O exception condition, then the
    /// rules for 9.1.13, I-O status apply"; §9.1.13.1: a status beginning 3 or 4 "indicate[s] fatal exception
    /// conditions" and the implementor "may either continue or terminate the execution of the run unit"; the
    /// implementor's determination (docs/CONFORMANCE.md §7, DOC-A.1-103) continues only when the program is equipped
    /// to see the failure, and with nothing equipped the run unit terminates (§14.6.12). That is the implementor's
    /// determination, not a rule the standard mandates: no EC-I-O checking is enabled here, so §14.6.13.1.3 8) leaves
    /// whether execution continues to the implementor, and 7) (termination, with checking enabled) does not apply. The
    /// constant must not change that: its file is an ordinary file once named, so each fatal status of the abnormal
    /// paths the golden answers through a status item ends the run, with nothing after the failing statement run.
    /// <list type="bullet">
    /// <item>OPEN INPUT of a non-optional file whose physical file is absent: '35' (§9.1.13.6 5)), first digit 3,
    /// EC-I-O-PERMANENT-ERROR.</item>
    /// <item>WRITE to a file that is not open: '48' (§9.1.13.7 8)), first digit 4, EC-I-O-LOGIC-ERROR.</item>
    /// </list>
    /// Every leg can fail: "AFTER" is displayed only if the run unit continued, and the termination must name the
    /// exception, the statement and the status the rule gives. The constant is spelled as a word that is ALSO a
    /// system-name (PB2500KN, the golden's spelling); the golden's RR and SP legs, not this test, pin which file it names.</summary>
    [Theory]
    [InlineData("PB3438A", "OPEN INPUT NF", "EC-I-O-PERMANENT-ERROR", "(OPEN)", "35")]
    [InlineData("PB3438B", "WRITE NF-REC", "EC-I-O-LOGIC-ERROR", "(WRITE)", "48")]
    public void ConstantNamedFile_FatalStatus_NeitherFileStatusNorUse_TerminatesTheRunUnit(
        string pid, string statement, string ecName, string stmtTag, string status)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT NF ASSIGN TO PB2500KN
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD NF.
            01 NF-REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 PB2500KN CONSTANT AS "pb3438-absent.dat".
            PROCEDURE DIVISION.
            MAIN-P.
                DISPLAY "BEFORE".
                {statement}.
                DISPLAY "AFTER".
                STOP RUN.
            """, 2023);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean with stdout:\n{stdout}");
        Assert.Equal("BEFORE", stdout.Trim());
        Assert.Contains("abnormal run-unit termination", detail);
        Assert.Contains(ecName, detail);
        Assert.Contains(stmtTag, detail);
        Assert.Contains($"I-O status {status}", detail);
    }

    /// <summary>The leading word of a TWO-operand list is a device class when it spells one — the only allowable
    /// two-operand list is class + name (docs/CONFORMANCE.md §7 DOC-A.1-71) — even when the program also declares a
    /// constant of that spelling; the constant reading would be a list of two names, which is refused. The SECOND
    /// operand is read as any single operand is: a constant names the file.</summary>
    [Fact]
    public void ConstantSpelledLikeADeviceClass_LeadingATwoOperandList_IsTheClass()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB2500C1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT W ASSIGN TO DISK NM
                    ORGANIZATION IS SEQUENTIAL.
                SELECT R ASSIGN TO "pb2500c.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD W.
            01 W-REC PIC X(4).
            FD R.
            01 R-REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 DISK CONSTANT AS "unused.dat".
            01 NM CONSTANT AS "pb2500c.dat".
            PROCEDURE DIVISION.
                OPEN OUTPUT W
                MOVE "CLAS" TO W-REC
                WRITE W-REC
                CLOSE W
                OPEN INPUT R
                READ R
                DISPLAY R-REC
                CLOSE R
                STOP RUN.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal("CLAS", stdout.Trim());
    }
}
