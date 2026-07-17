// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The SAME AS clause syntax rules (ISO §13.18.49 / §13.16.3 SR12; P10 Step 16). The run behavior — elementary
/// copy with VALUE, group copy + subordinate renumbering, SAME AS + OCCURS, a copied STRONG identity — is the
/// byte-verified <c>typedef_same_as</c> golden; these tests pin each rejection band: COBOLNET1555 (the
/// subject-entry rules), COBOLNET1556 (the referenced-entry rules), COBOLNET1557 (cycles), plus the
/// qualified-reference positive. Every guard cites its ISO rule.
/// </summary>
public sealed class SameAsTests
{
    private static (bool Ok, System.Collections.Generic.IReadOnlyList<string> Diags) C(string src) =>
        EditionHarness.Compile(src, 2002);

    /// <summary>§13.16.3 SR12 — SAME AS composes only with CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL,
    /// level-number, and OCCURS; a sibling PICTURE clause is COBOLNET1555.</summary>
    [Fact]
    public void SameAsWithPicture_Rejected1555()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA55A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PROTO PIC 9(3).
            01 W SAME AS PROTO PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS with a sibling PICTURE clause must be rejected (ISO §13.16.3 SR12)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1555");
    }

    /// <summary>§13.18.49.3 SR2 — a SAME AS entry shall not be immediately followed by a subordinate entry or a
    /// level-88 entry (COBOLNET1555).</summary>
    [Fact]
    public void SameAsWithSubordinate_Rejected1555()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA55B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PROTO.
               05 F PIC X.
            01 W SAME AS PROTO.
               05 EXTRA PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS followed by a subordinate entry must be rejected (ISO §13.18.49.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1555");
    }

    /// <summary>§13.18.49.3 SR9 — no group containing the subject may carry a USAGE (or SIGN / GROUP-USAGE)
    /// clause (COBOLNET1555).</summary>
    [Fact]
    public void SameAsUnderUsageGroup_Rejected1555()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA55C.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PROTO PIC 9(4).
            01 G USAGE COMP.
               05 W SAME AS PROTO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS under a USAGE-carrying group must be rejected (ISO §13.18.49.3 SR9)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1555");
    }

    /// <summary>§13.18.49.3 SR8 — a level-77 subject requires an elementary data-name-1 (COBOLNET1555).</summary>
    [Fact]
    public void SameAs77OfGroup_Rejected1555()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA55D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PROTO.
               05 F PIC X.
            77 W SAME AS PROTO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "a level-77 SAME AS of a group must be rejected (ISO §13.18.49.3 SR8)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1555");
    }

    /// <summary>§13.18.49.3 SR7 — data-name-1 shall reference an elementary item or a LEVEL-1 group item; a
    /// mid-level subordinate GROUP target is COBOLNET1556.</summary>
    [Fact]
    public void SameAsOfMidLevelGroup_Rejected1556()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA56A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OUTER-G.
               05 INNER-G.
                  10 F PIC X.
            01 W SAME AS INNER-G.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS of a mid-level subordinate group must be rejected (ISO §13.18.49.3 SR7)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1556");
    }

    /// <summary>§13.18.49.3 SR5 — data-name-1's own entry shall not contain an OCCURS clause (COBOLNET1556).
    /// (Subordinates of data-name-1 may — the positive rides the <c>typedef_same_as</c> golden's targets.)</summary>
    [Fact]
    public void SameAsOfOccursEntry_Rejected1556()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA56B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 ROW-E PIC X OCCURS 3.
            01 W SAME AS ROW-E.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS of an OCCURS-carrying entry must be rejected (ISO §13.18.49.3 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1556");
    }

    /// <summary>§13.18.49.3 SR1 — data-name-1 shall not be SUBJECT TO any OCCURS clause (an element of a table's
    /// subtree; the reference is a bare data-name, never subscripted) — COBOLNET1556.</summary>
    [Fact]
    public void SameAsOfTableSubordinate_Rejected1556()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA56C.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 ROW-E OCCURS 3.
                  10 F PIC X(2).
            01 W SAME AS F.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS of an item subject to OCCURS must be rejected (ISO §13.18.49.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1556");
    }

    /// <summary>§13.18.49.3 SR10 — data-name-1's description shall not contain a CONSTANT RECORD clause
    /// (COBOLNET1556).</summary>
    [Fact]
    public void SameAsOfConstantRecord_Rejected1556()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA56D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CREC CONSTANT RECORD.
               05 F PIC X VALUE "A".
            01 W SAME AS CREC.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS of a CONSTANT RECORD must be rejected (ISO §13.18.49.3 SR10)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1556");
    }

    /// <summary>An unresolved data-name-1 is COBOLNET1556 (§13.18.49.2 — data-name-1 shall reference a data
    /// item; TYPEDEF template members are OFF the referenceable index by §13.18.58.4 GR1, so a type's insides
    /// are equally unreachable).</summary>
    [Fact]
    public void SameAsUnresolved_Rejected1556()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA56E.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W SAME AS NO-SUCH-ITEM.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "an unresolved SAME AS target must be rejected (ISO §13.18.49.2/SR7)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1556");
    }

    /// <summary>§13.18.49.3 SR3 — a direct SAME AS cycle (mutual references) is COBOLNET1557.</summary>
    [Fact]
    public void SameAsMutualCycle_Rejected1557()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA57A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A SAME AS B.
            01 B SAME AS A.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "a mutual SAME AS cycle must be rejected (ISO §13.18.49.3 SR3)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1557");
    }

    /// <summary>§13.18.49.3 SR3 (containment leg) — SAME AS referencing a group the subject is subordinate to
    /// is COBOLNET1557.</summary>
    [Fact]
    public void SameAsOfOwnAncestor_Rejected1557()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA57B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G.
               05 F PIC X.
               05 W SAME AS G.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """);
        Assert.False(ok, "SAME AS of an enclosing group must be rejected (ISO §13.18.49.3 SR3)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1557");
    }

    /// <summary>The positives beyond the <c>typedef_same_as</c> golden: a QUALIFIED target reference
    /// (<c>SAME AS F OF G2</c> — data-name-1 is an ordinary data-name reference, §8.4.3.2), a CHAINED
    /// SAME AS (B SAME AS A where A itself is a SAME AS copy — GR1 copies the complete expanded description),
    /// and SAME AS + EXTERNAL/GLOBAL siblings (the §13.16.3 SR12 allowed set) must all compile clean.</summary>
    [Fact]
    public void QualifiedChainedAndComposedSameAs_CompileClean()
    {
        var (ok, diag) = C("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA-OK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 F PIC 9(2).
            01 G2.
               05 F PIC X(4).
            01 W1 SAME AS F OF G2.
            01 W2 SAME AS W1.
            01 W3 SAME AS G1 EXTERNAL.
            01 W4 SAME AS G1 GLOBAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO W1.
                MOVE W1 TO W2.
                STOP RUN.
            """);
        Assert.True(ok, $"qualified / chained / EXTERNAL-GLOBAL-composed SAME AS must compile clean: "
            + string.Join("; ", diag));
    }
}
