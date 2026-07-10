// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.4 gates — the file-control SHARING (§12.4.5.15) and LOCK MODE (§12.4.5.9)
/// clauses, the SPECIAL-NAMES FOR ALPHANUMERIC/NATIONAL phrase (§12.3.7), and the PROCEDURE DIVISION RETURNING (§14.2)
/// and RAISING (§14.2.2) phrases — all COBOL-2002 introductions relocated from their bind-time Checks
/// (DataBinder.BindFileControl / SwitchBindSpecialNames / CallBindLinkage) to the post-bind <c>VersionConformancePass</c>
/// parse-arm on RECOGNITION. The version matrix + the contains-based conformance suite verify PRESENCE; these pin the
/// FIRING COUNT. The load-bearing case is <see cref="MethodReturning_At85_NotGated"/>: the procedureDivision rule is
/// SHARED by program and method PDs, but CallBindLinkage gated program units only — so a method's RETURNING must NOT
/// gate (the InMethodDefinition guard; the 14g.3 shared-rule lesson).
/// </summary>
public sealed class ConfigPdEditionTests
{
    private static int Count0900(string source, int edition, string whereFragment)
    {
        var (_, errors, _) = EditionHarness.CompileFull(source, edition);
        return errors.Count(e => e.Contains("COBOLNET0900", StringComparison.OrdinalIgnoreCase)
            && e.Contains(whereFragment, StringComparison.OrdinalIgnoreCase));
    }

    // A SELECT carrying BOTH the SHARING and LOCK MODE clauses (independent §12.4.5 introductions).
    private const string ShareLock = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SHL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "f.dat"
                SHARING WITH NO OTHER
                LOCK MODE IS MANUAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(10).
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    private const string AlphabetFor = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ALF.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET A IS STANDARD-1 FOR ALPHANUMERIC.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    private const string ProgramReturning = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RET.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC X.
        PROCEDURE DIVISION RETURNING L.
        MAIN.
            STOP RUN.
        """;

    private const string ProgramRaising = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RAI.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC X.
        PROCEDURE DIVISION RAISING SOME-EXC.
        MAIN.
            STOP RUN.
        """;

    // A METHOD whose PROCEDURE DIVISION carries RETURNING — the same procedureDivision rule as a program PD, but the
    // former gate (CallBindLinkage) ran for program units only, so this must NOT fire ProcedureReturning2002.
    private const string MethodReturning = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DRV.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        END PROGRAM DRV.

        IDENTIFICATION DIVISION.
        CLASS-ID. MC.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. M.
        DATA DIVISION.
        LINKAGE SECTION.
        01 RV PIC X.
        PROCEDURE DIVISION RETURNING RV.
        MPARA.
            GOBACK.
        END METHOD M.
        END OBJECT.
        END CLASS MC.
        """;

    /// <summary>The SELECT … SHARING clause gates EXACTLY ONCE at 85, never at 2002.</summary>
    [Fact]
    public void SharingClause_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(ShareLock, 85, "the SHARING clause"));

    [Fact]
    public void SharingClause_At2002_NoGate()
        => Assert.Equal(0, Count0900(ShareLock, 2002, "the SHARING clause"));

    /// <summary>The SELECT … LOCK MODE clause gates EXACTLY ONCE at 85 (independently of the sibling SHARING clause).</summary>
    [Fact]
    public void LockModeClause_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(ShareLock, 85, "the LOCK MODE clause"));

    /// <summary>An ALPHABET … FOR ALPHANUMERIC phrase (one of the three SPECIAL-NAMES FOR sites) gates EXACTLY ONCE
    /// at 85, never at 2002.</summary>
    [Fact]
    public void AlphabetForPhrase_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(AlphabetFor, 85, "the FOR ALPHANUMERIC/NATIONAL phrase"));

    [Fact]
    public void AlphabetForPhrase_At2002_NoGate()
        => Assert.Equal(0, Count0900(AlphabetFor, 2002, "the FOR ALPHANUMERIC/NATIONAL phrase"));

    /// <summary>A PROGRAM-unit PROCEDURE DIVISION RETURNING gates EXACTLY ONCE at 85.</summary>
    [Fact]
    public void ProgramReturning_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(ProgramReturning, 85, "the PROCEDURE DIVISION RETURNING phrase"));

    /// <summary>A PROGRAM-unit PROCEDURE DIVISION RAISING gates EXACTLY ONCE at 85.</summary>
    [Fact]
    public void ProgramRaising_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(ProgramRaising, 85, "the PROCEDURE DIVISION RAISING phrase"));

    /// <summary>The load-bearing shared-rule witness: a METHOD's PROCEDURE DIVISION RETURNING does NOT gate at 85 —
    /// CallBindLinkage (the former gate site) ran for program units only, and the InMethodDefinition guard reproduces
    /// that. (Other 0900s fire — the class definition, etc. — but the RETURNING phrase must not.)</summary>
    [Fact]
    public void MethodReturning_At85_NotGated()
        => Assert.Equal(0, Count0900(MethodReturning, 85, "the PROCEDURE DIVISION RETURNING phrase"));

    // ── DEVLOG-738 (14g.4 review): class-level env-division clauses gate the SPEC-CORRECT ONCE ──────────────────
    // A class's CLASS-LEVEL environment division is adopted by BOTH the object and factory reparent binders
    // (OoReparent{Class,Factory}Data), and the binder's singular environmentDivision() accessor means it was bound
    // twice (no object/factory env) or zero times (both shadowed) — so the former bind-time gate fired 2× or 0×. The
    // parse-arm fires ONCE per clause node (the spec-correct count). These pin that; the underlying OO-env
    // double/zero-BIND (which also mis-registers class-level CURRENCY/ALPHABET/SELECT) is a flagged latent bug.

    private const string ClassLevelSpecialNames = """
        IDENTIFICATION DIVISION.
        CLASS-ID. CN.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET AL IS STANDARD-1 FOR ALPHANUMERIC.
        OBJECT.
        PROCEDURE DIVISION.
        END OBJECT.
        END CLASS CN.
        """;

    // The same class-level SPECIAL-NAMES, but with an OBJECT env AND a FACTORY env that both shadow it — the former
    // gate fired ZERO times here; the parse-arm still fires exactly once (a spec-correct improvement).
    private const string ClassLevelSpecialNamesShadowed = """
        IDENTIFICATION DIVISION.
        CLASS-ID. CN.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET AL IS STANDARD-1 FOR ALPHANUMERIC.
        FACTORY.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. FOO.
        END FACTORY.
        OBJECT.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. BAR.
        PROCEDURE DIVISION.
        END OBJECT.
        END CLASS CN.
        """;

    /// <summary>A class-level SPECIAL-NAMES FOR phrase gates EXACTLY ONCE (the parse node), not twice — even though
    /// both the object and factory reparent binders adopt the class-level env (the former gate fired 2×).</summary>
    [Fact]
    public void ClassLevelSpecialNamesFor_At85_GatesExactlyOnce()
        => Assert.Equal(1, Count0900(ClassLevelSpecialNames, 85, "the FOR ALPHANUMERIC/NATIONAL phrase"));

    /// <summary>The same, when the class-level env is SHADOWED by both an object and a factory env — the parse-arm
    /// still fires once (the former gate fired zero times, silently missing the below-edition construct).</summary>
    [Fact]
    public void ClassLevelSpecialNamesFor_Shadowed_At85_GatesExactlyOnce()
        => Assert.Equal(1, Count0900(ClassLevelSpecialNamesShadowed, 85, "the FOR ALPHANUMERIC/NATIONAL phrase"));
}
