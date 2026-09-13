// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// USAGE POINTER data (ISO §8.5.2.6 / §13.18.60 / §14.9.39 Format 7 / §8.8.4.2.16) — Phase-4b increment 1
/// (the ManagedPointer carrier; DEVLOG 613). Increment 1 holds only NULL: declaration, SET TO NULL /
/// pointer, and [NOT] EQUAL comparison against NULL and another pointer. ADDRESS OF / BASED / ALLOCATE are
/// increment 2+. The end-to-end behavior rides the pointer_data conformance golden; these lock the edition
/// gate and the diagnostic band.
/// </summary>
public sealed class PointerDataTests
{
    private static readonly string PtrProg = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PTRT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 P USAGE POINTER.
        01 Q USAGE POINTER.
        01 N PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
        {0}
            STOP RUN.
        """;

    private static string Prog(string body) => PtrProg.Replace("{0}", body);

    /// <summary>The introduction gate: USAGE POINTER is COBOL-2002+ (§13.18.60) — at 85 the W1.5 mapping
    /// names the edition (0900); at 2002 it binds and runs.</summary>
    [Fact]
    public void UsagePointer_IntroducedAt2002()
    {
        var (ok85, e85, _) = EditionHarness.CompileFull(Prog("    DISPLAY \"X\"."), 85);
        Assert.False(ok85, "USAGE POINTER is 2002+; 85 must reject");
        EditionHarness.AssertHasDiagnostic(e85, "COBOLNET0900");
        var (ok02, e02, _) = EditionHarness.CompileFull(Prog("    SET P TO NULL.\n    DISPLAY \"X\"."), 2002);
        Assert.True(ok02, "USAGE POINTER + SET TO NULL must bind at 2002: " + string.Join("\n", e02));
    }

    /// <summary>§14.9.39 Format 7 — a data-pointer SET target shall be USAGE POINTER; a non-pointer target
    /// (or a non-pointer/non-NULL sender) is COBOLNET0869.</summary>
    [Theory]
    [InlineData("    SET N TO P.")]                 // pointer sender into a non-pointer target (routes to F4)
    [InlineData("    SET P TO N.")]                 // non-pointer sender into a pointer
    public void SetPointer_Violations_0869(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(body), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>⛔ AN INDEX-NAME RECEIVER IS REFUSED FOR THE REASON IT IS REFUSED (kb/Work PB388).
    /// <c>SET IX TO P</c> re-routes to the data-pointer format on the SENDER's category, and the receiver
    /// resolution then answered for a DATA ITEM only — so the FIRST thing the user read was COBOLNET1639,
    /// "'IX' is not defined — no declaration in this source element gives the name 'IX'", about a name
    /// INDEXED BY declared four lines up. The refusal is right (§14.9.39.3 SR17: identifier-5 shall reference
    /// a data item of category data-pointer); the statement about the program was not.</summary>
    /// <remarks>BOTH arms of the dispatch: the carrier re-route is selected by whichever operand HAS a carrier
    /// category, so an index-name reaches the receiving position AND the sending one (feedback_two_arm_dispatch
    /// — the sender arm was still printing COBOLNET1639 after the receiver arm was repaired).</remarks>
    [Theory]
    [InlineData("    SET IX TO P.")]
    [InlineData("    SET P TO IX.")]
    public void SetPointer_IndexNameOperand_IsACategoryError_NotAnUndefinedName(string body)
    {
        string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PTRTIXR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 E PIC X OCCURS 3 INDEXED BY IX.
            01 P USAGE POINTER.
            PROCEDURE DIVISION.
            MAIN.
            {0}
                STOP RUN.
            """.Replace("{0}", body);
        var (ok, errors, _) = EditionHarness.CompileFull(prog, 2002);
        Assert.False(ok, "an index-name is not of category data-pointer");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET1639");
        EditionHarness.AssertNoDiagnostic(errors, "is not defined");
    }

    /// <summary>§8.8.4.2.16 — a data pointer is not ORDERED: only [NOT] EQUAL; an ordering operator or an
    /// object/numeric mix is COBOLNET0869.</summary>
    [Theory]
    [InlineData("    IF P < Q DISPLAY \"X\" END-IF.")]   // ordering
    [InlineData("    IF P = N DISPLAY \"X\" END-IF.")]   // pointer vs numeric
    public void PointerRelation_Violations_0869(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(body), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }
}
