// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The TYPEDEF staged-loud residue (Phase 6, data-model D17, increment 4): TYPEDEF sub-features that are recognized
/// but not yet fully modeled must be a LOUD bind-time rejection, never a silent mis-compile (COBOLNET_DESIGN §1.4).
/// COBOLNET1534 — an EXTERNAL type declaration (run-unit-shared, §13.18.57.4 GR5); COBOLNET1535 — a RENAMES inside a
/// TYPEDEF (not cloned into references, §13.18.58.4 GR1) and a strong group with a boolean/object/pointer element
/// compared with an ordering operator (§8.8.4.2.3 SR4, equality-only); COBOLNET1531 — a type whose OCCURS has an
/// INDEXED BY phrase referenced ≥2× (the global index-name would collide, §13.18.38). The positive companions
/// (a single INDEXED-type reference — also the <c>typedef_indexed</c> golden — and a strong boolean-group EQUALITY
/// compare) must NOT trip a guard.
/// </summary>
public sealed class TypedefResidueTests
{
    /// <summary>§13.18.57.4 GR5 / §13.18.22 — an EXTERNAL type declaration is staged loud (COBOLNET1534).</summary>
    [Fact]
    public void ExternalTypeDeclaration_Rejected1534()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR34.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF EXTERNAL.
               05 F PIC X.
            01 R TYPE REC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an EXTERNAL type declaration must be staged loud (ISO §13.18.57.4 GR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1534");
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
        Assert.False(ok, "an ordering compare of a boolean-bearing strong group must be staged loud (ISO §8.8.4.2.3 SR4)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1535");
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
