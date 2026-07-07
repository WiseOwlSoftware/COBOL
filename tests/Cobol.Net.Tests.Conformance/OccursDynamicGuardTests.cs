// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The OCCURS DYNAMIC staged-loud guards (Phase 6, data-model D9, increment 5): a dynamic-capacity table has
/// construction/placement rules whose violation must be a LOUD bind-time rejection, never a silent mis-compile
/// (COBOLNET_DESIGN §1.4). Each guard cites its ISO §13.18.38 / §13.18.44 / §13.18.63 rule. The positive facts
/// (a well-formed FROM/TO, and a VALUE on a GROUP dynamic table's subordinate = the element seed) must NOT trip a
/// guard — the run-success corpus (<c>dyn_*</c>) covers behavior; these assert the negative gating.
/// </summary>
public sealed class OccursDynamicGuardTests
{
    private static string Prog(string tableEntry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DYNG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + tableEntry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>§13.18.38 SR28 — integer-5 (TO) shall be greater than integer-4 (FROM); <c>FROM 8 TO 3</c> is a
    /// declaration error, COBOLNET1522, not a table that opens above its own expected capacity.</summary>
    [Fact]
    public void FromGreaterThanTo_Rejected1522()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 8 TO 3."), 2014);
        Assert.False(ok, "FROM 8 TO 3 must be rejected (ISO §13.18.38 SR28)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1522");
    }

    /// <summary>§13.18.44 SR5 — a dynamic-capacity table is out-of-line storage that can neither be the subject nor
    /// the object of a REDEFINES (it does not share a fixed area). COBOLNET1525.</summary>
    [Fact]
    public void RedefinesOverDynamicTable_Rejected1525()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 3.\n   05 WS-R REDEFINES WS-E PIC X(9)."), 2014);
        Assert.False(ok, "a REDEFINES over a dynamic-capacity table must be rejected (ISO §13.18.44 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1525");
    }

    /// <summary>§13.18.38 GR16 / §13.18.63 GR6 — a VALUE clause on an ELEMENTARY dynamic-capacity entry derives the
    /// initial capacity (the VALUE-derived-capacity subrules), a construct staged loud (COBOLNET1528) rather than
    /// silently mis-seeded.</summary>
    [Fact]
    public void ValueOnElementaryDynamicEntry_Rejected1528()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 3 VALUE 7."), 2014);
        Assert.False(ok, "a VALUE clause on an elementary dynamic entry (VALUE-derived capacity) is staged loud");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1528");
    }

    /// <summary>§13.18.63 GR16 (review #4) — a VALUE clause in an entry SUPERORDINATE to a DYNAMIC entry derives the
    /// initial capacity (GR16b: no-TO-in-VALUE → the OCCURS expected capacity). When the OCCURS carries a TO, that
    /// derivation applies to a GROUP dynamic table's subordinate VALUE too → staged loud COBOLNET1528 (not the
    /// silently-wrong capacity = FROM).</summary>
    [Fact]
    public void GroupSubordinateValueWithTo_Rejected1528()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("""
            01 WS-TABLE.
               05 ROW OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
                  10 ELEM PIC 99 VALUE 07.
            """), 2014);
        Assert.False(ok, "a group dynamic table with a subordinate VALUE and a TO derives capacity (GR16) — staged loud");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1528");
    }

    /// <summary>§8.5.1.9.1 item 3 (review #6) — a dynamic-capacity table "may be defined in any place, OTHER THAN the
    /// file section" (its out-of-line storage has no place in a record image). COBOLNET1526.</summary>
    [Fact]
    public void FileSectionDynamicTable_Rejected1526()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FDYN.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "fdyn.dat" ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  F.
            01  REC.
                05  DT OCCURS DYNAMIC FROM 2 CAPACITY IN CP PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2014);
        Assert.False(ok, "OCCURS DYNAMIC in the FILE SECTION must be rejected (ISO §8.5.1.9.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1526");
    }

    /// <summary>The positive companions: a well-formed FROM/TO, and a VALUE on the SUBORDINATE of a GROUP dynamic
    /// table (the element's per-occurrence seed, capacity = FROM — supported, NOT a VALUE-derived capacity), both
    /// compile cleanly. Guards must not over-restrict the supported surface.</summary>
    [Fact]
    public void WellFormedAndGroupSubordinateValue_CompileClean()
    {
        var (ok1, diag1) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 2 TO 5."), 2014);
        Assert.True(ok1, $"a well-formed FROM 2 TO 5 must compile: {string.Join("; ", diag1)}");

        var (ok2, diag2) = EditionHarness.Compile(Prog("""
            01 WS-TABLE.
               05 WS-ROW OCCURS DYNAMIC FROM 3 INITIALIZED.
                  10 WS-NAME PIC X(4) VALUE "----".
                  10 WS-QTY  PIC 9(2) VALUE 7.
            """), 2014);
        Assert.True(ok2, $"a VALUE on a GROUP dynamic table's subordinate is the element seed, not 1528: {string.Join("; ", diag2)}");
    }
}
