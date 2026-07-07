// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The STRONG TYPEDEF use + declaration restrictions (Phase 6, data-model D17, increment 2): a strongly-typed group
/// item (ISO §8.5.3.3) protects its data integrity with COMPILE-TIME checks, whose violation must be a LOUD bind-time
/// rejection (COBOLNET_DESIGN §1.4). Each guard cites its ISO rule — the USE gates (COBOLNET1533): MOVE §14.9.25.3
/// SR2, comparison §8.8.4.2.3 SR1, class condition §8.8.4.4.3 SR1; the DECLARATION gates (COBOLNET1532): §13.18.57.3
/// SR3 (no RENAMES), SR4 (no REDEFINES), SR6 (level-1 or subordinate to a strong group). The positive companions must
/// NOT trip a guard — the run-success corpus (<c>typedef_strong_ok</c>) covers same-type MOVE/compare behavior; these
/// assert the negative gating and that a same-type / same-position operation stays clean.
/// </summary>
public sealed class TypedefStrongTests
{
    // Two DISTINCT strong types + a record of each — the different-type USE cases.
    private const string TwoTypes = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. TS.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A-T TYPEDEF STRONG.
           05 AX PIC 9(3).
        01 B-T TYPEDEF STRONG.
           05 BX PIC 9(3).
        01 RA TYPE A-T.
        01 RB TYPE B-T.
        PROCEDURE DIVISION.
        MAIN-PARA.
        """;

    /// <summary>§14.9.25.3 SR2 — a MOVE whose RECEIVER is a strongly-typed group requires a SENDER of the same type;
    /// a different-type whole-record MOVE is COBOLNET1533.</summary>
    [Fact]
    public void MoveDifferentType_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile(TwoTypes + "\n    MOVE RA TO RB.\n    STOP RUN.", 2002);
        Assert.False(ok, "MOVE between two different strong types must be rejected (ISO §14.9.25.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>§8.8.4.2.3 SR1 — if either comparison operand is a strongly-typed group, both shall be of the same
    /// type; a different-type relation is COBOLNET1533. (The check rides the ONE CheckedRelational chokepoint, so it
    /// also covers EVALUATE / PERFORM UNTIL / SEARCH WHEN.)</summary>
    [Fact]
    public void CompareDifferentType_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile(
            TwoTypes + "\n    IF RA = RB\n        DISPLAY \"X\"\n    END-IF.\n    STOP RUN.", 2002);
        Assert.False(ok, "comparing two different strong types must be rejected (ISO §8.8.4.2.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>§8.8.4.4.3 SR1 — a strongly-typed group has its own unique class and category (the type-name) and may
    /// not appear in a class condition. COBOLNET1533.</summary>
    [Fact]
    public void ClassConditionOnStrongGroup_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile(
            TwoTypes + "\n    IF RA IS NUMERIC\n        DISPLAY \"X\"\n    END-IF.\n    STOP RUN.", 2002);
        Assert.False(ok, "a class condition on a strongly-typed group must be rejected (ISO §8.8.4.4.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>§13.18.57.3 SR6 — a STRONG type may be referenced only at level 1 or subordinate to a strongly-typed
    /// group; a strong TYPE reference as a lone field of an ORDINARY group is COBOLNET1532.</summary>
    [Fact]
    public void StrongRefInOrdinaryGroup_Rejected1532()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TS6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A-T TYPEDEF STRONG.
               05 AX PIC 9(3).
            01 OUTER.
               05 INNER TYPE A-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a strong TYPE reference inside an ordinary group must be rejected (ISO §13.18.57.3 SR6)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1532");
    }

    /// <summary>§13.18.57.3 SR4 — a strongly-typed item shall not be redefined in whole or in part; a REDEFINES over a
    /// strong record is COBOLNET1532.</summary>
    [Fact]
    public void RedefinesOverStrongItem_Rejected1532()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TS4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A-T TYPEDEF STRONG.
               05 AX PIC 9(3).
            01 RA TYPE A-T.
            01 RB REDEFINES RA PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a REDEFINES over a strongly-typed item must be rejected (ISO §13.18.57.3 SR4)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1532");
    }

    /// <summary>§13.18.57.3 SR3 — a strongly-typed item shall not be renamed in whole or in part; a level-66 RENAMES
    /// spanning a strong record's fields is COBOLNET1532.</summary>
    [Fact]
    public void RenamesOverStrongItem_Rejected1532()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TS3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A-T TYPEDEF STRONG.
               05 AX PIC 9(3).
               05 AY PIC 9(3).
            01 REC TYPE A-T.
            66 RN RENAMES AX THRU AY.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a RENAMES over a strongly-typed item must be rejected (ISO §13.18.57.3 SR3)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1532");
    }

    /// <summary>The §8.5.3 same-type rule is by relative POSITION, not just type-name: two DIFFERENT subgroups of the
    /// SAME strong type (GA vs GB) are NOT the same type, so a cross-position MOVE is COBOLNET1533 — the
    /// relative-member-path half of <c>SameStrongType</c>.</summary>
    [Fact]
    public void MoveDifferentSubgroupPosition_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSREL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PAIR-T TYPEDEF STRONG.
               05 GA.
                  10 GAX PIC 9(3).
               05 GB.
                  10 GBX PIC 9(3).
            01 R1 TYPE PAIR-T.
            01 R2 TYPE PAIR-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE GA OF R1 TO GB OF R2.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a MOVE between different-position subgroups of one strong type is not same-type (ISO §8.5.3)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>The positive companions must compile CLEAN — the gates must not over-restrict: an individual field
    /// MOVE into a strong record (a strong record is built up field by field), a same-position subgroup MOVE/compare
    /// (matching relative path), and a whole-record same-type MOVE. (The <c>typedef_strong_ok</c> corpus golden
    /// byte-verifies the run behavior; here we assert no false COBOLNET1532/1533.)</summary>
    [Fact]
    public void SameTypeAndFieldOps_CompileClean()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSOK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PAIR-T TYPEDEF STRONG.
               05 GA.
                  10 GAX PIC 9(3).
               05 GB.
                  10 GBX PIC 9(3).
            01 R1 TYPE PAIR-T.
            01 R2 TYPE PAIR-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 5 TO GAX OF R1.
                MOVE GA OF R1 TO GA OF R2.
                MOVE R1 TO R2.
                IF GA OF R1 = GA OF R2
                    DISPLAY "OK"
                END-IF.
                IF R1 = R2
                    DISPLAY "OK2"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok, $"same-type / same-position / individual-field strong operations must compile clean: "
            + string.Join("; ", diag));
    }
}
