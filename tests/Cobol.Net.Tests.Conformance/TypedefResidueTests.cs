// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The TYPEDEF residue rules (Phase 6, data-model D17, increment 4; P10 Step 16 reworked the EXTERNAL leg):
/// COBOLNET1558 — the EXTERNAL type-declaration conformance rules (§13.18.22 GR2 level-1 reference; SR5
/// strong-external pairing — the former COBOLNET1534 stage is LIFTED, the accepted form is the
/// <c>typedef_external</c> golden); COBOLNET1535 — a RENAMES inside a TYPEDEF (not cloned into references,
/// §13.18.58.4 GR1, staged) and a strong group with a boolean/object/pointer element compared with an ordering
/// operator (§8.8.4.2.3 SR4 — the complete spec rule: equality/inequality only); COBOLNET0899
/// <c>strong-group-ordering-signed-leaf</c> — an ordering over same-type strong groups with a SIGNED numeric
/// leaf (the §8.8.4.2.12 element-by-element algebraic ordering, staged); COBOLNET1531 — a type whose OCCURS has
/// an INDEXED BY phrase referenced ≥2× (the global index-name would collide, §13.18.38). The positive companions
/// (a single INDEXED-type reference — also the <c>typedef_indexed</c> golden — and a strong boolean-group
/// EQUALITY compare) must NOT trip a guard.
/// </summary>
public sealed class TypedefResidueTests
{
    /// <summary>§13.18.22 GR2 — a data description containing an EXTERNAL type declaration shall be at
    /// level-number 1; a NESTED (level > 1) TYPE reference to an external type is COBOLNET1558. (The level-1
    /// form is ACCEPTED and run-verified by the <c>typedef_external</c> golden — the 1534 stage is lifted.)</summary>
    [Fact]
    public void ExternalTypeReferenceBelowLevel1_Rejected1558()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR58A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF EXTERNAL.
               05 F PIC X.
            01 G.
               05 R TYPE REC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a below-level-1 reference to an EXTERNAL type must be rejected (ISO §13.18.22 GR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1558");
    }

    /// <summary>§13.18.22 SR5 — an EXTERNAL record described with a STRONG type requires that type declaration
    /// to be external too; a plain STRONG typedef on an EXTERNAL record is COBOLNET1558.</summary>
    [Fact]
    public void ExternalRecordWithNonExternalStrongType_Rejected1558()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR58B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF STRONG.
               05 F PIC X.
            01 R TYPE REC-T EXTERNAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an EXTERNAL record of a non-external STRONG type must be rejected (ISO §13.18.22 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1558");
    }

    /// <summary>§13.18.58.4 GR1 — a level-66 RENAMES inside a TYPEDEF (part of the type, but not cloned into a TYPE
    /// reference) is staged loud (COBOLNET1535).</summary>
    [Fact]
    public void RenamesInsideTypedef_Rejected1535()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR35R.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF.
               05 A PIC X.
               05 B PIC X.
            66 RN RENAMES A THRU B.
            01 R TYPE REC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a RENAMES inside a TYPEDEF must be staged loud (ISO §13.18.58.4 GR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1535");
    }

    /// <summary>§8.8.4.2.3 SR4 — a strongly-typed group whose elements include class boolean / object / pointer may be
    /// compared only for equality; an ordering relation on such a group is staged loud (COBOLNET1535).</summary>
    [Fact]
    public void StrongBooleanGroupOrderingCompare_Rejected1535()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR35B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BREC-T TYPEDEF STRONG.
               05 FLAG PIC 1.
            01 R1 TYPE BREC-T.
            01 R2 TYPE BREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 < R2
                    DISPLAY "X"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an ordering compare of a boolean-bearing strong group must be rejected (ISO §8.8.4.2.3 SR4)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1535");
    }

    /// <summary>§8.8.4.2.12 (P10 Step 16, staged loud): an ORDERING relation between same-type strong groups
    /// containing a SIGNED numeric leaf needs the element-by-element ALGEBRAIC comparison (§8.8.4.2.4), which
    /// the whole-group image comparison cannot honor — COBOLNET0899 <c>strong-group-ordering-signed-leaf</c>.
    /// The EQUALITY compare of the same shape stays legal (image-equal ⟺ element-equal).</summary>
    [Fact]
    public void StrongSignedGroupOrderingCompare_Staged0899()
    {
        const string decl = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {0}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SREC-T TYPEDEF STRONG.
               05 AMT PIC S9(3).
            01 R1 TYPE SREC-T.
            01 R2 TYPE SREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 {1} R2
                    DISPLAY "X"
                END-IF.
                STOP RUN.
            """;
        var (okLt, diagLt) = EditionHarness.Compile(decl.Replace("{0}", "TR12A").Replace("{1}", "<"), 2002);
        Assert.False(okLt, "an ordering compare of a signed-leaf strong group must be staged loud (ISO §8.8.4.2.12)");
        EditionHarness.AssertHasDiagnostic(diagLt, "COBOLNET0899");

        var (okEq, diagEq) = EditionHarness.Compile(decl.Replace("{0}", "TR12B").Replace("{1}", "="), 2002);
        Assert.True(okEq, $"an EQUALITY compare of the same signed-leaf strong groups must compile clean: "
            + string.Join("; ", diagEq));
    }

    /// <summary>§13.18.38 — a type whose OCCURS carries an INDEXED BY phrase, referenced twice, clones the same global
    /// index-name onto two tables; staged loud (COBOLNET1531).</summary>
    [Fact]
    public void IndexedTypeReferencedTwice_Rejected1531()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR31.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL-T TYPEDEF.
               05 ROW OCCURS 3 INDEXED BY IX PIC X.
            01 A TYPE TBL-T.
            01 B TYPE TBL-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an INDEXED-BY type referenced twice must be staged loud (ISO §13.18.38)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1531");
    }

    /// <summary>The positive companions must NOT trip a guard: a SINGLE INDEXED-type reference (the guard is ≥2×, not
    /// ≥1× — also the <c>typedef_indexed</c> golden) and a strong boolean-group EQUALITY compare (SR4 bans only the
    /// ordering relation).</summary>
    [Fact]
    public void SingleIndexedTypeAndBooleanEquality_CompileClean()
    {
        var (ok1, diag1) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR1IX.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL-T TYPEDEF.
               05 ROW OCCURS 3 INDEXED BY IX PIC X.
            01 A TYPE TBL-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET IX TO 1.
                MOVE "Z" TO ROW OF A (IX).
                STOP RUN.
            """, 2002);
        Assert.True(ok1, $"a single INDEXED-type reference must compile clean: {string.Join("; ", diag1)}");

        var (ok2, diag2) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR1EQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BREC-T TYPEDEF STRONG.
               05 FLAG PIC 1.
            01 R1 TYPE BREC-T.
            01 R2 TYPE BREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 = R2
                    DISPLAY "EQ"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok2, $"an EQUALITY compare of a boolean strong group must compile clean: {string.Join("; ", diag2)}");
    }
}
