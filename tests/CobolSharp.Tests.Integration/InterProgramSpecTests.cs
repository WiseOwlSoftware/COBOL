// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Spec-conformance integration tests for Inter-Program Communication features that the baselined
/// NIST IC suite under-tests (WS-SPEC workstream; docs/SPEC_GAP_INVENTORY.md "## Inter-Program
/// Communication"). Every [Fact] asserts output observed from the compiled+run program.
/// </summary>
public sealed class InterProgramSpecTests : EndToEndTestBase
{
    // ─────────────────────────────────────────────────────────────────────────────
    // PROGRAM-ID ... INITIAL clause (ISO_COBOL.md §8810; commonProgramAttribute : INITIAL_)
    //
    // An INITIAL program's internal data is re-initialized to its VALUE state on EVERY call
    // (§8810). The runtime effect is otherwise only reached via CANCEL (IC203A); no baselined
    // A-test exercises the clause directly. Here a non-INITIAL subprogram retains its counter
    // across two calls (1 then 2) while the INITIAL subprogram resets to its VALUE each entry
    // (1 then 1), isolating the INITIAL attribute on a passing path.
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public void InitialClause_ReinitializesStateEachCall_WhereasNormalProgramRetains()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INITDRV.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "NORMSUB"
                CALL "NORMSUB"
                CALL "INITSUB"
                CALL "INITSUB"
                STOP RUN.
            END PROGRAM INITDRV.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NORMSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CTR PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            SUB-PARA.
                ADD 1 TO WS-CTR
                DISPLAY "NORM " WS-CTR
                EXIT PROGRAM.
            END PROGRAM NORMSUB.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INITSUB INITIAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CTR PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            SUB-PARA.
                ADD 1 TO WS-CTR
                DISPLAY "INIT " WS-CTR
                EXIT PROGRAM.
            END PROGRAM INITSUB.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        // Non-INITIAL program retains WORKING-STORAGE state across calls: 1, then 2.
        Assert.Equal("NORM 1", lines[0]);
        Assert.Equal("NORM 2", lines[1]);
        // INITIAL program re-initializes to its VALUE clause on each entry: 1, then 1.
        Assert.Equal("INIT 1", lines[2]);
        Assert.Equal("INIT 1", lines[3]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // PROGRAM-ID ... COMMON clause (ISO_COBOL.md D.6.5; commonProgramAttribute : COMMON)
    //
    // A COMMON contained program is visible to (callable by) the sibling programs contained in the
    // same outer program, not only by the directly-containing program. Here outermost COMOUT calls
    // sibling SIBB; SIBB calls the COMMON sibling SIBA, passing a value BY REFERENCE; SIBA triples
    // it (7 -> 21) and SIBB prints the result, proving the COMMON sibling is reachable and operates
    // correctly on data handed to it by another sibling.
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public void CommonClause_CommonSiblingIsCallableByOtherSibling_AndTransformsData()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMOUT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "SIBB"
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIBA COMMON.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LS-N PIC 9(4).
            PROCEDURE DIVISION USING LS-N.
            A-PARA.
                COMPUTE LS-N = LS-N * 3
                EXIT PROGRAM.
            END PROGRAM SIBA.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIBB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-V PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
            B-PARA.
                CALL "SIBA" USING WS-V
                DISPLAY "RESULT " WS-V
                EXIT PROGRAM.
            END PROGRAM SIBB.
            END PROGRAM COMOUT.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // SIBA (declared COMMON) was reachable from sibling SIBB and tripled 7 -> 21 by reference.
        Assert.Equal("RESULT 0021", stdout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CALL ... USING BY VALUE identifier (ISO_COBOL.md §14.9.4 Format 2; callByValue {is2002()}?)
    //
    // BY VALUE is a COBOL-2002+ phrase (grammar-gated behind is2002()), so this compiles under the
    // Cobol2002 dialect. The argument is passed by value: the callee receives a COPY (10), modifies
    // its own local copy (10 + 100 = 110), and the caller's WS-A is left UNCHANGED (still 0010) —
    // the defining property of pass-by-value vs. the default BY REFERENCE.
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public void CallUsingByValue_CalleeReceivesCopy_CallerOperandUnchanged()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BVMAIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4) VALUE 10.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "BVSUB" USING BY VALUE WS-A
                DISPLAY "CALLER A " WS-A
                STOP RUN.
            END PROGRAM BVMAIN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BVSUB.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LS-V PIC 9(4).
            PROCEDURE DIVISION USING LS-V.
            SUB-PARA.
                ADD 100 TO LS-V
                DISPLAY "CALLEE V " LS-V
                EXIT PROGRAM.
            END PROGRAM BVSUB.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        // Callee mutated its local copy.
        Assert.Equal("CALLEE V 0110", lines[0]);
        // Caller's operand is unchanged — proves BY VALUE (a copy), not BY REFERENCE.
        Assert.Equal("CALLER A 0010", lines[1]);
    }
}

