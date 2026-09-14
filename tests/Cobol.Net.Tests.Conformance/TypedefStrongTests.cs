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

    /// <summary>ISO §8.5.3.1 alternative 2 — two subordinate items in equivalent type declarations are of the same
    /// type only when they start "at the same relative byte or bit position and [have] the same length in bytes or
    /// bits". GA (bit 0) and GB (bit 24) are two subgroups of ONE declaration at DIFFERENT positions, so a
    /// cross-position MOVE is COBOLNET1533.
    /// <para>⚠ This test stated the criterion correctly while the code it pinned matched member-NAME PATHS, and it
    /// passed only because this fixture's paths and positions happen to agree (kb/Work PB427). Its companions
    /// <see cref="MoveSameOffsetDifferentlyNamedSubgroups_Accepted"/> (paths differ, positions agree → legal) and
    /// <see cref="MoveSamePathDifferentPosition_Rejected1533"/> (paths agree, positions differ → refused) are the
    /// pair that tells the two criteria apart, and neither existed before.</para></summary>
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

    // ── ISO §8.5.3.1, THE SAME-TYPE TEST ITSELF (kb/Work PB427) ──────────────────────────────────────────────
    // TypedefSameTypeTests-PB427. The clause has TWO alternatives over typed items and ONE relation under them:
    //   "Two typed items are of the same type when:
    //    — The items are described with TYPE clauses that reference equivalent type declarations; or
    //    — The items are described as subordinate items in equivalent type declarations, starting at the same
    //      relative byte pr bit position and having the same length in bytes or bits."
    //   "Two type declarations are considered equivalent when they have the same type-name, both have the same
    //    presence or absence of the EXTERNAL clause and the STRONG phrase, and for each elementary item in one
    //    type declaration there is a corresponding elementary item in the other type declaration, starting at
    //    the same relative byte or bit position and having the same length in bytes or bits. Each pair of
    //    corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED,
    //    PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses …"
    // The four tests below are the four axes a type-NAME + member-NAME-PATH stand-in got wrong.

    /// <summary>§8.5.3.1 alternative 2, the POSITIVE direction: OUTERG and INNERG are DIFFERENTLY NAMED subgroups
    /// of one declaration, both starting at relative bit 0 and both 32 bits long, so they are of the same type and
    /// the MOVE is legal. A member-name-path criterion refused this — legal source, rejected.</summary>
    [Fact]
    public void MoveSameOffsetDifferentlyNamedSubgroups_Accepted()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSPOSOK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NEST-T TYPEDEF STRONG.
               05 OUTERG.
                  10 INNERG.
                     15 LEAFX PIC X(4).
            01 N1 TYPE NEST-T.
            01 N2 TYPE NEST-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE OUTERG OF N1 TO INNERG OF N2.
                STOP RUN.
            """, 2002);
        Assert.True(ok, "two subordinate items of equivalent declarations at the same relative position and length "
            + "ARE of the same type (ISO §8.5.3.1): " + string.Join("; ", diag));
    }

    /// <summary>§8.5.3.1 alternative 2, the axis the path criterion could not see: the two declarations ARE
    /// equivalent — identical elementary layout (two PIC X(2) leaves at bits 0 and 16), identical clauses, same
    /// type-name, same STRONG, no EXTERNAL — but grouping differs, so <c>G</c> spans bits 0–31 in the container and
    /// bits 16–31 in the contained program. The member-name PATHS agree (<c>G</c> in both); the POSITIONS and
    /// LENGTHS do not, so the operands are NOT of the same type. §8.5.3.1's first paragraph is why the declarations
    /// still count as equivalent: the essential characteristics are the elementary items' positions, lengths and
    /// clauses — an intermediate group level is none of them.</summary>
    [Fact]
    public void MoveSamePathDifferentPosition_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSPATHPOS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SPLIT-T TYPEDEF STRONG.
               05 G.
                  10 GX PIC X(2).
                  10 GY PIC X(2).
            01 R1 TYPE SPLIT-T IS GLOBAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "TSPATHPOSIN" AS NESTED.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSPATHPOSIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SPLIT-T TYPEDEF STRONG.
               05 GX PIC X(2).
               05 G.
                  10 GY PIC X(2).
            01 R2 TYPE SPLIT-T.
            PROCEDURE DIVISION.
            CMAIN.
                MOVE G OF R1 TO G OF R2.
                GOBACK.
            END PROGRAM TSPATHPOSIN.
            END PROGRAM TSPATHPOS.
            """, 2002);
        Assert.False(ok, "equivalent declarations do not make two subordinate items the same type unless they also "
            + "start at the same relative position and have the same length (ISO §8.5.3.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>§8.5.3.1's equivalence relation: a matching type-NAME is ONE conjunct. Two source elements each
    /// declare SHAPE-T; the names match and nothing else does (one 6-byte elementary item against two 3-byte ones),
    /// so the declarations are not equivalent and the whole-record MOVE is refused. Under the name-string criterion
    /// this program COMPILED AND RAN, depositing "ABC"/"DEF" into two PIC 9(3) items (kb/Work PB427).</summary>
    [Fact]
    public void MoveNonEquivalentSameNamedDeclarations_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSNEQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SA PIC X(6).
            01 OUTER-X TYPE SHAPE-T IS GLOBAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "TSNEQIN" AS NESTED.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSNEQIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SB PIC 9(3).
               05 SC PIC 9(3).
            01 INNER-Y TYPE SHAPE-T.
            PROCEDURE DIVISION.
            CMAIN.
                MOVE OUTER-X TO INNER-Y.
                GOBACK.
            END PROGRAM TSNEQIN.
            END PROGRAM TSNEQ.
            """, 2002);
        Assert.False(ok, "two same-named but structurally different type declarations are NOT equivalent, so items "
            + "described with them are not of the same type (ISO §8.5.3.1 / §14.9.25.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>§8.5.3.1's equivalence relation, the POSITIVE direction across a source-element boundary: the same
    /// two declarations made ACTUALLY equivalent — same type-name, same STRONG phrase, no EXTERNAL clause on either,
    /// and corresponding elementary items at the same relative positions with the same lengths and clauses — are
    /// equivalent, so the cross-element whole-record MOVE conforms. This is the over-rejection guard on the test
    /// above: the fix must refuse the non-equivalent pair WITHOUT refusing this one.</summary>
    [Fact]
    public void MoveEquivalentSameNamedDeclarationsAcrossElements_Accepted()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSEQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SA PIC X(3).
               05 SB PIC 9(3).
            01 OUTER-X TYPE SHAPE-T IS GLOBAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "TSEQIN" AS NESTED.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSEQIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SA PIC X(3).
               05 SB PIC 9(3).
            01 INNER-Y TYPE SHAPE-T.
            PROCEDURE DIVISION.
            CMAIN.
                MOVE OUTER-X TO INNER-Y.
                GOBACK.
            END PROGRAM TSEQIN.
            END PROGRAM TSEQ.
            """, 2002);
        Assert.True(ok, "two source elements declaring EQUIVALENT same-named types describe items of the same type "
            + "(ISO §8.5.3.1): " + string.Join("; ", diag));
    }

    /// <summary>§8.5.3.1's equivalence relation names the ESSENTIAL CHARACTERISTICS clause by clause — "Each pair of
    /// corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED,
    /// PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses". Here the two declarations agree on name, strength, layout
    /// and every position and length — the leaves are both 3 bytes — and differ ONLY in the PICTURE clause
    /// (<c>X(3)</c> against <c>9(3)</c>). That alone makes them non-equivalent.</summary>
    [Fact]
    public void MoveSameLayoutDifferentPicture_Rejected1533()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSPIC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SA PIC X(3).
            01 OUTER-X TYPE SHAPE-T IS GLOBAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "TSPICIN" AS NESTED.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSPICIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SHAPE-T TYPEDEF STRONG.
               05 SA PIC 9(3).
            01 INNER-Y TYPE SHAPE-T.
            PROCEDURE DIVISION.
            CMAIN.
                MOVE OUTER-X TO INNER-Y.
                GOBACK.
            END PROGRAM TSPICIN.
            END PROGRAM TSPIC.
            """, 2002);
        Assert.False(ok, "corresponding elementary items with different PICTURE clauses make the two type "
            + "declarations non-equivalent (ISO §8.5.3.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1533");
    }

    /// <summary>ISO §14.8.2.2 — "If either the formal parameter or the corresponding argument is a strongly-typed
    /// group item, both shall be of the same type." PLAING is an ordinary 4-byte group and the formal is a
    /// strongly-typed 4-byte group, so every LENGTH test the activation boundary applies is satisfied and only this
    /// sentence refuses the crossing. It was implemented NOWHERE before kb/Work PB427 — the program ran.</summary>
    [Fact]
    public void CallArgumentNotSameTypeAsStrongFormal_Rejected()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSARG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PLAING.
               05 PA PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "TSARGIN" AS NESTED USING PLAING.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSARGIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CT-T TYPEDEF STRONG.
               05 CA PIC X(4).
            LINKAGE SECTION.
            01 LF TYPE CT-T.
            PROCEDURE DIVISION USING LF.
            CMAIN.
                GOBACK.
            END PROGRAM TSARGIN.
            END PROGRAM TSARG.
            """, 2002);
        Assert.False(ok, "an argument that is not of the formal's type may not cross into a strongly-typed group "
            + "formal (ISO §14.8.2.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1688");
    }
}
