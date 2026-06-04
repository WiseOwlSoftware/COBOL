// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Item 8 (DEVLOG 306): PICTURE / level-number validity.
/// - CBL0815 (level number) is unconditional: an out-of-range or unparseable level is diagnosed instead
///   of crashing a level-number int.Parse (ISO §8.5.1.2). No valid program has a bad level.
/// - CBL0814 (illegal PICTURE symbol) is dialect-gated to named-strict modes (staged like CBL3128): the
///   runtime PIC parser silently swallows unrecognized symbols, so this surfaces them in strict mode
///   while Default / --nist stay permissive — the NIST corpus (with its unusual-but-valid pictures) is
///   unaffected by construction.
/// </summary>
public class PicValidityTests : DiagnosticTestBase
{
    private static string Program(string dataEntries) => $@"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
{dataEntries}
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";

    // ── CBL0815: level number (unconditional) ──

    [Fact]
    public void OutOfRangeLevel_ReportsCBL0815()
        => AssertHasDiagnostic(GetDiagnostics(Program("       50 WS-X PIC X(3).")), "CBL0815");

    [Fact]
    public void HugeLevelNumber_DoesNotCrash_ReportsCBL0815()
        => AssertHasDiagnostic(GetDiagnostics(Program("       999999999999 WS-X PIC X(3).")), "CBL0815");

    [Fact]
    public void ValidLevels_NoCBL0815()
    {
        var src = Program(
            "       01 WS-REC.\n" +
            "          05 WS-A PIC X(3).\n" +
            "          88 WS-A-OK VALUE \"YES\".\n" +
            "       77 WS-STANDALONE PIC 9(4).");
        AssertNoDiagnostic(GetDiagnostics(src), "CBL0815");
    }

    // ── CBL0814: illegal PICTURE symbol (strict-gated) ──

    [Fact]
    public void Strict_IllegalPicSymbol_ReportsCBL0814()
        => AssertHasDiagnostic(GetDiagnostics(Program("       01 WS-X PIC 9Q9."), DialectMode.StrictCobol85), "CBL0814");

    [Fact]
    public void Default_IllegalPicSymbol_NoCBL0814()
        => AssertNoDiagnostic(GetDiagnostics(Program("       01 WS-X PIC 9Q9.")), "CBL0814");

    [Fact]
    public void Strict_UnusualButValidPics_NoCBL0814()
    {
        // CR/DB editing, floating-Z editing with insertion comma/period, leading+trailing P-scaling
        // with S, and a long V picture — all valid COBOL-85 pictures the NIST suite exercises.
        var src = Program(
            "       01 WS-A PIC 999DB.\n" +
            "       01 WS-B PIC ZZ,ZZZ.9.\n" +
            "       01 WS-C PIC SP(8)9.\n" +
            "       01 WS-D PIC 999999999V999999999.");
        AssertNoDiagnostic(GetDiagnostics(src, DialectMode.StrictCobol85), "CBL0814");
    }
}
