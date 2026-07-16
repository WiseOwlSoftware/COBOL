// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// User-defined function invocation (ISO §9.4 / §8.4.3.2 / §12.3.8; Phase 4c M2-UDF-1 — DEVLOG 615).
/// End-to-end behavior rides the three udf_* conformance goldens (COMPUTE/MOVE receiving forms, inline
/// sub-expression, literal + arithmetic-expression arguments — all byte-exact); these lock the edition
/// gate, the §12.3.8.2 GR12 repository semantics, and the COBOLNET1501/1505–1509 diagnostic band.
/// </summary>
public sealed class UdfInvocationTests
{
    /// <summary>A whole-source caller + FUNCTION-ID unit. <paramref name="pid"/> keeps PROGRAM-IDs unique
    /// per fact (the stale same-named-assembly hazard); {body} is the caller's MAIN body.</summary>
    private static string Group(string pid, string body, string repository = "    FUNCTION UDFDBL.") => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
        {{repository}}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 4.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
            STOP RUN.
        END PROGRAM {{pid}}.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. UDFDBL.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        P.
            COMPUTE L-R = L-X * 2.
            GOBACK.
        END FUNCTION UDFDBL.
        """;

    /// <summary>The introduction gate: user-defined functions are COBOL-2002+ (§9.4 / §12.3.8) — 0900 at 85
    /// (the binder gate and the PD-header RETURNING parse hint both name the edition); binds at 2002.</summary>
    [Fact]
    public void Invocation_IntroducedAt2002()
    {
        string src = Group("UDFT1", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).");
        var (ok85, e85, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok85, "user-defined functions are 2002+; 85 must reject");
        EditionHarness.AssertHasDiagnostic(e85, "COBOLNET0900");
        var (ok02, e02, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok02, "must bind at 2002: " + string.Join("\n", e02));
    }

    /// <summary>§12.3.8.2 GR12 is a PRECONDITION: without the repository FUNCTION specifier the name is not
    /// a user-function reference — COBOLNET1501, with the hint naming the in-group FUNCTION-ID.</summary>
    [Fact]
    public void MissingRepositoryEntry_1501_WithGr12Hint()
    {
        string src = Group("UDFT2", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).",
            repository: "    FUNCTION ALL INTRINSIC.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1501");
        Assert.Contains(errors, e => e.Contains("REPOSITORY", StringComparison.Ordinal));
    }

    /// <summary>A REPOSITORY-declared function with NO in-group FUNCTION-ID definition is the
    /// separate-compilation prototype surface (M2-UDF-3) — staged loud as COBOLNET1505.</summary>
    [Fact]
    public void DeclaredButUndefined_PrototypeGap_1505()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION NOWHERE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION NOWHERE(1).
                STOP RUN.
            END PROGRAM UDFT3.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1505");
    }

    /// <summary>§14.8.2 positional correspondence: an argument-count mismatch is COBOLNET1506. (The
    /// zero-argument spelling is the bare name — empty parentheses are a §8.4.3.2 format violation and
    /// already fail at parse.)</summary>
    [Theory]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL.")]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(1, 2).")]
    public void ArityMismatch_1506(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT4", body), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1506");
    }

    /// <summary>§14.2 procedure-division header (:23666): "The RETURNING phrase shall be specified in a
    /// function definition" — checked once per unit, even for an uncalled function (COBOLNET1507).</summary>
    [Fact]
    public void FunctionWithoutReturning_1507()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT5.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT5.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFNORET.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION.
            P.
                GOBACK.
            END FUNCTION UDFNORET.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1507");
    }

    /// <summary>The evaluation-cardinality guard (§14.9.28 / §14.9.37 / §14.9.13 / §8.8.4.13): a function
    /// reference in a re-evaluated or conditionally-evaluated position cannot ride the once-hoisted
    /// activation — loud COBOLNET1509, never a stale-temp loop or an over-evaluated side effect. Legs:
    /// PERFORM UNTIL, PERFORM VARYING, SEARCH WHEN, EVALUATE selection, and a non-first AND operand
    /// (§8.8.4.13 r1 short-circuit / r2 function timing).</summary>
    [Theory]
    [InlineData("    PERFORM UNTIL FUNCTION UDFDBL(WS-A) > 9000\n        ADD 1 TO WS-A\n    END-PERFORM.")]
    [InlineData("    PERFORM VARYING WS-A FROM 1 BY 1 UNTIL WS-A > FUNCTION UDFDBL(2)\n        DISPLAY \"X\"\n    END-PERFORM.")]
    [InlineData("    EVALUATE WS-A\n        WHEN FUNCTION UDFDBL(2) DISPLAY \"E\"\n    END-EVALUATE.")]
    [InlineData("    IF WS-A = 4 AND FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    [InlineData("    IF WS-A = 9 OR FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    public void ConditionallyEvaluatedPositions_1509(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT6", body), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1509");
    }

    /// <summary>SEARCH WHEN leg of the 1509 guard (per-pass re-evaluation, §14.9.37).</summary>
    [Fact]
    public void SearchWhenCondition_1509()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT6S.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFDBL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TAB.
               05 ROW PIC 9(2) OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION.
            MAIN.
                SET IX TO 1.
                SEARCH ROW
                    WHEN ROW(IX) = FUNCTION UDFDBL(2) DISPLAY "F"
                END-SEARCH.
                STOP RUN.
            END PROGRAM UDFT6S.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                GOBACK.
            END FUNCTION UDFDBL.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1509");
    }

    /// <summary>The 0900 introduction gate on the BINDER path: a caller-only source (no FUNCTION-ID unit,
    /// so the {is2002()}? PD-header RETURNING parse hint cannot be the source of the diagnostic) still names
    /// the edition at 85 via ConstructRegistry.Check in UdfBindCall.</summary>
    [Fact]
    public void BinderGate_0900_At85_CallerOnly()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT10.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION NOWHERE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION NOWHERE(1).
                STOP RUN.
            END PROGRAM UDFT10.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
    }

    /// <summary>Two FUNCTION-ID definitions with one name — COBOLNET1508 (first-wins signature kept).</summary>
    [Fact]
    public void DuplicateFunctionId_1508()
    {
        string src = Group("UDFT11", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).") + """

            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                GOBACK.
            END FUNCTION UDFDBL.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1508");
    }

    /// <summary>§8.4.6.6: within a function definition its OWN user-function-name is referable with NO
    /// repository declaration — self-recursion binds (a present self-entry would be ignored, §12.3.8 GR11).
    /// The runtime behavior (5! = 120 through five nested activations) is the udf_recursion golden.</summary>
    [Fact]
    public void SelfRecursion_WithoutRepositoryEntry_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT12.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UFCT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(8).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UFCT(5).
                STOP RUN.
            END PROGRAM UDFT12.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UFCT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-N PIC 9(4).
            01 L-R PIC 9(8).
            PROCEDURE DIVISION USING L-N RETURNING L-R.
            P.
                IF L-N < 2
                    MOVE 1 TO L-R
                ELSE
                    COMPUTE L-R = L-N * FUNCTION UFCT(L-N - 1)
                END-IF.
                GOBACK.
            END FUNCTION UFCT.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.4 GR1: the container's configuration-section entries apply to each contained source
    /// unit (§12.3.3 SR1 — a contained program cannot declare its own), so a contained program's function
    /// reference resolves through the OUTER program's REPOSITORY.</summary>
    [Fact]
    public void ContainedProgram_InheritsRepositoryFunctions()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT13.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFDBL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                CALL "UDFT13IN".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT13IN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-Q PIC 9(4).
            PROCEDURE DIVISION.
            M2.
                COMPUTE WS-Q = FUNCTION UDFDBL(3).
                EXIT PROGRAM.
            END PROGRAM UDFT13IN.
            END PROGRAM UDFT13.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                GOBACK.
            END FUNCTION UDFDBL.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>EXIT FUNCTION placement (the 0827 EXIT-family band): the pre-2023 function-return synonym
    /// may appear only in a function definition — in a program procedure division it is rejected.</summary>
    [Fact]
    public void ExitFunction_OutsideFunction_0827()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT15", "    EXIT FUNCTION."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0827");
    }

    /// <summary>The exit-function-window removal edge (Annex E.2 :49036): valid at 2002/2014, 0902 at 2023
    /// strict (the matrix row's RemovedMatrix theory proves the permissive migration contract). The runtime
    /// early-return behavior is the udf_exit_function golden (X=0014).</summary>
    [Fact]
    public void ExitFunction_Window()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT16.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFT16F.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UDFT16F(3).
                STOP RUN.
            END PROGRAM UDFT16.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFT16F.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                EXIT FUNCTION.
            END FUNCTION UDFT16F.
            """;
        var (ok02, e02, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok02, string.Join("\n", e02));
        var (ok23, e23, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok23, "EXIT FUNCTION was removed by 2023 (Annex E.2) — strict must reject");
        EditionHarness.AssertHasDiagnostic(e23, "COBOLNET0902");
    }

    /// <summary>The RETURNING category channel (§8.4.3.2.4 GR1 / §14.2.2 SR5 — NO category restriction on a
    /// function's RETURNING item; P10 Step 9): the CARRIED categories — elementary fixed-point numeric,
    /// alphanumeric, numeric-edited, national, and character-form groups — compile end-to-end (the runtime
    /// behavior is the udf_returning_categories golden); the STAGED residues — FLOAT (no CALL-boundary float
    /// write half), BOOLEAN (no §8.8.2 boolean-expression function-result arm), and a group with a
    /// non-character (binary) leaf (the Tier-C image island) — stay loud COBOLNET1510 by name.</summary>
    [Theory]
    [InlineData("ALN", "01 L-R PIC X(4).", true)]
    [InlineData("GRP", "01 L-R.\n               05 L-R-A PIC 9(2).\n               05 L-R-B PIC 9(2).", true)]
    [InlineData("EDT", "01 L-R PIC ZZ9.", true)]
    [InlineData("NAT", "01 L-R PIC N(3).", true)]
    [InlineData("FLT", "01 L-R USAGE FLOAT-LONG.", false)]
    [InlineData("BOL", "01 L-R PIC 1(4).", false)]
    [InlineData("BIN", "01 L-R.\n               05 L-R-A PIC X(2).\n               05 L-R-B PIC 9(4) USAGE BINARY.", false)]
    public void ReturningCategories_CarriedVsStaged1510(string tag, string returningDecl, bool carried)
    {
        // DISPLAY is the category-neutral reference site (a MOVE receiver would entangle Table-16 legality —
        // e.g. national→alphanumeric is a "No" cell); unique PROGRAM-IDs per fact (P10UR wave).
        string pid = $"UDFT14{tag}P10UR";
        string src = $$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {{pid}}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFX{{tag}}P10UR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-D PIC X(1).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FUNCTION UDFX{{tag}}P10UR(1).
                STOP RUN.
            END PROGRAM {{pid}}.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFX{{tag}}P10UR.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            {{returningDecl}}
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                GOBACK.
            END FUNCTION UDFX{{tag}}P10UR.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        if (carried)
            Assert.True(ok, string.Join("\n", errors));
        else
        {
            Assert.False(ok);
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1510");
        }
    }

    /// <summary>Shapes the goldens do not cover, locked at bind level: a no-argument function, a nested
    /// user-function argument (activation order = registration order, §8.4.3.2.4 GR2), a user-function
    /// argument inside an INTRINSIC call, and an IF-condition reference (once-per-execution — exact).</summary>
    [Theory]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(FUNCTION UDFDBL(WS-A)).")]
    [InlineData("    COMPUTE WS-R = FUNCTION MOD(FUNCTION UDFDBL(3), 4).")]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(FUNCTION MOD(7, 4)).")]
    [InlineData("    IF FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    [InlineData("    IF FUNCTION UDFDBL(WS-A) = 8 AND WS-A = 4 DISPLAY \"Y\" END-IF.")]
    [InlineData("    MOVE FUNCTION UDFDBL(WS-A) TO WS-R.")]
    public void SupportedShapes_Bind(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT7", body), 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>No-argument function: <c>FUNCTION name</c> with no subscript part, zero USING formals.</summary>
    [Fact]
    public void NoArgFunction_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT8.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UDFK.
                STOP RUN.
            END PROGRAM UDFT8.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFK.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                MOVE 7 TO L-R.
                GOBACK.
            END FUNCTION UDFK.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.8.2 GR12 (:14885): a REPOSITORY-declared function-prototype-name refers to the
    /// USER-DEFINED function "and not to an intrinsic function of the same name" — the spec's own
    /// factorial-override pattern (:43651). A user function named SQRT must bind against the FUNCTION-ID
    /// unit's ONE formal, not the intrinsic catalog's signature.</summary>
    [Fact]
    public void UserFunctionShadowsIntrinsic_Gr12()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT9.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION SQRT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION SQRT(4, 5).
                STOP RUN.
            END PROGRAM UDFT9.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. SQRT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-B PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
            P.
                COMPUTE L-R = L-A + L-B.
                GOBACK.
            END FUNCTION SQRT.
            """;
        // Two arguments: the INTRINSIC SQRT takes exactly one — binding proves the user function won.
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    // ── M2-UDF-3: function prototypes (ISO §11.5 Format 2 / §10.6 / §12.3.8; DEVLOG 624) ─────────────────────

    /// <summary>UDF-3: a FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) preceding the caller (§10.6.2 SR1) supplies
    /// the signature; the same-name in-group DEFINITION that follows is authoritative (§12.3.8 GR11a) — the pair
    /// is NOT a COBOLNET1508 duplicate. Binds clean at 2002.</summary>
    [Fact]
    public void Prototype_WithDefinition_NotDuplicate_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPSQ IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPSQ.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT17.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UPSQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4) VALUE 6.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UPSQ(WS-A).
                STOP RUN.
            END PROGRAM UDFT17.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPSQ.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * L-X.
                GOBACK.
            END FUNCTION UPSQ.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>UDF-3: a prototype-declared function with NO in-group definition BINDS — the prototype supplies
    /// the §8.4.3.2.4 GR1 signature and the definition is a separately-compiled target resolved at run time
    /// (§12.3.8 GR11c / §8.4.3.2.4 GR6b). The former COBOLNET1505 "not implemented" gap is closed for the
    /// prototyped case (the cross-assembly run is proven in InterProgramFileDifferentialTests).</summary>
    [Fact]
    public void PrototypeOnly_NoInGroupDefinition_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPXTERN IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPXTERN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT18.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UPXTERN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4) VALUE 6.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UPXTERN(WS-A).
                STOP RUN.
            END PROGRAM UDFT18.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, "a prototype-declared function must bind with no in-group definition: " + string.Join("\n", errors));
    }

    /// <summary>UDF-3: §10.6.2 SR3 — an in-group prototype and same-name definition shall have the same
    /// signature; an argument-count mismatch is COBOLNET1513.</summary>
    [Fact]
    public void PrototypeDefinitionSignatureMismatch_1513()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPMIS IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPMIS.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT19.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT19.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPMIS.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-B PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
            P.
                COMPUTE L-R = L-A + L-B.
                GOBACK.
            END FUNCTION UPMIS.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1513");
    }

    /// <summary>UDF-3: a function prototype without a RETURNING phrase is ill-formed — COBOLNET1507 (a function
    /// cannot deliver a result without it, §14.2).</summary>
    [Fact]
    public void PrototypeWithoutReturning_1507()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPNORET IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            PROCEDURE DIVISION USING L-X.
            END FUNCTION UPNORET.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT20.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT20.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1507");
    }

    /// <summary>UDF-3: FUNCTION-ID … IS PROTOTYPE is a 2002 introduction — a prototype at 85 is rejected with the
    /// edition-naming COBOLNET0900 (the W1.5 ReservedWordEditionHints mapping for the {is2002()}?-gated tail).</summary>
    [Fact]
    public void PrototypeAt85_0900()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UP85 IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UP85.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT21.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT21.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok, "a function prototype is 2002+; 85 must reject");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
    }

    // ── M2-UDF-4: REPOSITORY specifiers + the §8.4.3.2 SR2 keyword-omitted reference form (DEVLOG 626) ────────

    /// <summary>A minimal program that references <paramref name="expr"/> as its COMPUTE/MOVE body under the
    /// given <paramref name="repository"/> and optional extra <paramref name="ws"/> — for the keyword-omission
    /// facts (byte-exact behaviour rides the udf_keyword_omitted golden).</summary>
    private static string KoGroup(string pid, string repository, string body, string ws = "") => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
        {{repository}}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 12.
        01 WS-B PIC 9(4) VALUE 34.
        01 WS-R PIC 9(4).
        {{ws}}
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
            STOP RUN.
        END PROGRAM {{pid}}.
        """;

    /// <summary>§8.4.3.2 SR2 + §12.3.8 GR13/GR14 — with FUNCTION ALL INTRINSIC in the REPOSITORY the word
    /// FUNCTION may be omitted for an intrinsic reference (SR6: the '(' is the argument list). Binds at 2002.</summary>
    [Fact]
    public void KeywordOmitted_AllIntrinsic_Binds()
    {
        string src = KoGroup("UDFT22", "    FUNCTION ALL INTRINSIC.",
            "    COMPUTE WS-R = MAX(WS-A, WS-B).\n    MOVE MIN(WS-A, WS-B) TO WS-R.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.8 — a specifically-named intrinsic (FUNCTION MAX INTRINSIC, no ALL) also enables keyword
    /// omission for THAT name.</summary>
    [Fact]
    public void KeywordOmitted_NamedIntrinsic_Binds()
    {
        string src = KoGroup("UDFT23", "    FUNCTION MAX INTRINSIC.", "    COMPUTE WS-R = MAX(WS-A, WS-B).");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>The D2 data-item-wins safety: a declared data item whose NAME matches an intrinsic stays a
    /// subscripted reference — never mis-routed to the intrinsic — even under FUNCTION ALL INTRINSIC (SR13 would
    /// prohibit the declaration; enforcing it is staged, so the reference must remain the data item, not silently
    /// become a function). Binds as the table element.</summary>
    [Fact]
    public void KeywordOmitted_DataItemWins()
    {
        string src = KoGroup("UDFT24", "    FUNCTION ALL INTRINSIC.", "    MOVE MOD(2) TO WS-R.",
            ws: "01 GRP.\n           05 MOD PIC 9(4) OCCURS 3 VALUE 7.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, "a data item named like an intrinsic must stay a subscript: " + string.Join("\n", errors));
    }

    /// <summary>§8.4.3.2 SR2 is a PRECONDITION (a RUNTIME-observable property): WITHOUT a REPOSITORY intrinsic/ALL
    /// specifier the word FUNCTION is REQUIRED — a bare MAX(a, b) is NOT the intrinsic. It is an unresolved data
    /// reference, which under the §1.4 "references then fail loud" contract compiles but loud-fails at run time
    /// (a compile-time assertion cannot distinguish it from the intrinsic-routed case — both compile). Contrast:
    /// with FUNCTION ALL INTRINSIC the same source runs and computes MAX=34.</summary>
    [Fact]
    public void KeywordOmitted_RequiresRepositoryDeclaration_RuntimeLoudFail()
    {
        string dir = CutRunner.NewTempDir("udfko");
        try
        {
            string src = KoGroup("UDFKON", "    FUNCTION UDFDBL.",
                "    COMPUTE WS-R = MAX(WS-A, WS-B).\n    DISPLAY \"R=\" WS-R.");
            string cob = Path.Combine(dir, "UDFKON.cob");
            File.WriteAllText(cob, src);
            var r = CobolNet.CompilerDriver.Compile(
                new CobolNet.CompilerDriver.Options(cob, Path.Combine(dir, "UDFKON.dll"), DialectLevel: 2002));
            Assert.True(r.Success, "the undefined-reference program compiles (the §1.4 loud-fail is deferred to run "
                + "time): " + string.Join("\n", r.Errors));
            var (ok, _, detail) = CutRunner.Run(Path.Combine(dir, "UDFKON.dll"), dir);
            Assert.False(ok, "MAX without FUNCTION and without an ALL/named-intrinsic specifier must not bind as "
                + "the intrinsic — it is an unresolved reference that loud-fails");
            Assert.Contains("MAX", detail, StringComparison.Ordinal);   // the loud-fail names the unresolved reference
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
