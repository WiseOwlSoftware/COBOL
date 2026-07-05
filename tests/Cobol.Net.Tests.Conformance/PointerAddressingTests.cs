// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE / F10 arithmetic (Phase-4b increment 2; the
/// PHASE4_RECONCILIATION design + its adversarial-review coverage findings). End-to-end behavior rides the
/// three pointer goldens; these lock the bind gates, the review-caught legs, and the compile-validity of the
/// emitted cell/bridge code across statement shapes.
/// </summary>
public sealed class PointerAddressingTests
{
    private static string Prog(string pid, string decls, string body) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{decls}}
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
            STOP RUN.
        """;

    /// <summary>ADDRESS OF an EXTERNAL record (the design test plan's named case): the item is ALREADY
    /// cell-backed by the run-unit ExternalStore — the increment-2 unification means the emitter's
    /// <c>ManagedPointer.At(ExternalStore.Cell(...), offset)</c> leg compiles with no special case.</summary>
    [Fact]
    public void AddressOf_ExternalRecord_Compiles()
    {
        string src = Prog("PTAT1",
            "01 X PIC X(5) EXTERNAL.\n        01 P USAGE POINTER.\n        01 B PIC X(5) BASED.",
            "    SET P TO ADDRESS OF X.\n            SET ADDRESS OF B TO P.\n            DISPLAY B.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>F10 multi-target (<c>SET P Q UP BY 2</c> — every target moves) compiles; a pointer/index
    /// MIX in one statement is the SR23 0869 rejection.</summary>
    [Fact]
    public void UpDownBy_MultiTarget_AndSr23Mix()
    {
        string decls = "01 X PIC X(6) VALUE \"ABCDEF\".\n        01 P USAGE POINTER.\n        01 Q USAGE POINTER.\n"
            + "        01 TAB.\n           05 ROW PIC 9 OCCURS 3 INDEXED BY IX.";
        var (ok, e, _) = EditionHarness.CompileFull(
            Prog("PTAT2", decls, "    SET P TO ADDRESS OF X.\n            SET Q TO ADDRESS OF X.\n            SET P Q UP BY 2."), 2002);
        Assert.True(ok, string.Join("\n", e));
        var (okMix, eMix, _) = EditionHarness.CompileFull(
            Prog("PTAT3", decls, "    SET P TO ADDRESS OF X.\n            SET P IX UP BY 1."), 2002);
        Assert.False(okMix);
        EditionHarness.AssertHasDiagnostic(eMix, "COBOLNET0869");
    }

    /// <summary>The two NEW binder gates fire with THEIR OWN diagnostics at 85 (the review's gate-masking
    /// finding: the matrix witnesses' 85 legs trip the BASED/POINTER parse hints first, so a bare-code 0900
    /// assertion could not tell whether these gates exist at all). The where-texts are gate-specific.</summary>
    [Fact]
    public void BinderGates_FireUnmasked_At85()
    {
        string decls = "01 X PIC X(5).\n        01 P USAGE POINTER.";
        var (_, e1, _) = EditionHarness.CompileFull(
            Prog("PTAT4", decls, "    SET P TO ADDRESS OF X."), 85);
        EditionHarness.AssertHasDiagnostic(e1, "SET ADDRESS OF (ISO §14.9.39 Format 7)");
        var (_, e2, _) = EditionHarness.CompileFull(
            Prog("PTAT5", decls, "    SET P UP BY 2."), 85);
        EditionHarness.AssertHasDiagnostic(e2, "SET pointer UP/DOWN BY (ISO §14.9.39 Format 10)");
    }

    /// <summary>The ALLOCATE legs the goldens do not reach, locked at compile level: a fractional request
    /// (GR1 rounds UP via the AwayFromZero rescale), INITIALIZED with CHARACTERS (GR6 zero-fill), and the
    /// form-2 RETURNING delivery (GR4b).</summary>
    [Theory]
    [InlineData("    ALLOCATE 2.5 CHARACTERS RETURNING P.")]
    [InlineData("    ALLOCATE 8 CHARACTERS INITIALIZED RETURNING P.")]
    [InlineData("    ALLOCATE B RETURNING P.")]
    [InlineData("    ALLOCATE 0 CHARACTERS RETURNING P.\n            IF P = NULL DISPLAY \"N\" END-IF.")]
    public void Allocate_Legs_Compile(string body)
    {
        string decls = "01 P USAGE POINTER.\n        01 B PIC X(5) BASED.";
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("PTAT6", decls, body), 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>FREE under enabled EC-STORAGE-NOT-ALLOC checking (>>TURN): the EcWrap BoundFree arm selects
    /// the family and the emitter renders the §14.6.13.1.3 #5 sequence (status set + the F3 selection) —
    /// the checked leg must produce compilable code both with and without an F3 declarative.</summary>
    [Fact]
    public void Free_UnderTurnedChecking_Compiles()
    {
        string src = """
            >>TURN EC-STORAGE-NOT-ALLOC CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PTAT7.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 P USAGE POINTER.
            01 X PIC X(3).
            PROCEDURE DIVISION.
            MAIN.
                SET P TO ADDRESS OF X.
                FREE P.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>ADDRESS OF a SUBORDINATE of a BASED record carries the subordinate's offset (the review's
    /// ClassOffset-drop finding — the base address would alias the wrong bytes). Compile-level lock; the
    /// runtime displacement is CLI-probed (B2 window reads "CD" of "ABCDEF" at offset 2).</summary>
    [Fact]
    public void AddressOf_BasedSubordinate_Compiles()
    {
        string decls = "01 P USAGE POINTER.\n        01 Q USAGE POINTER.\n"
            + "        01 B BASED.\n           05 B1 PIC X(4).\n           05 B2 PIC X(4).";
        string body = "    ALLOCATE B RETURNING P.\n            SET Q TO ADDRESS OF B2.\n"
            + "            IF Q = P DISPLAY \"BAD\" ELSE DISPLAY \"OK\" END-IF.";
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("PTAT8", decls, body), 2002);
        Assert.True(ok, string.Join("\n", errors));
    }
}
