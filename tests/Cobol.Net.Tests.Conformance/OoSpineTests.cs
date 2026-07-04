// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Phase-3 OO SPINE facts (docs/COBOLNET_OO_DESIGN.md — spine part 2: ClassUnit collection + the pass-1
/// class symbol table, the emit-into-a-type parameterization, method PC-dispatch + BoundMethodReturn (D8),
/// INVOKE NEW / no-arg instance binding, typed object references): the deep-dive's ADVERSARIAL REGRESSION
/// TRAPS reproduced as day-one tests (each was a REAL caught legacy bug — traps #1/#4/#10 run here or in the
/// enabled 2002 corpus), the D8 control-flow contract (method GOBACK vs STOP RUN vs EXIT METHOD), and the OO
/// diagnostic band (COBOLNET0813/0820–0827). Two-object independence (trap #1) runs as the ENABLED corpus
/// programs oo_instance_data / oo_object_group (CorpusRunnerTests byte-compares them).
/// </summary>
public sealed class OoSpineTests
{
    /// <summary>A driver + one class with the given OBJECT-paragraph procedure division body.</summary>
    private static string DriverAndClass(string pid, string cls, string driverBody, string classBody) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS {{cls}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T USAGE OBJECT REFERENCE {{cls}}.
        PROCEDURE DIVISION.
        MAIN.
        {{driverBody}}
            STOP RUN.
        END PROGRAM {{pid}}.

        IDENTIFICATION DIVISION.
        CLASS-ID. {{cls}}.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        {{classBody}}
        END OBJECT.
        END CLASS {{cls}}.
        """;

    private static (bool Ok, string Stdout, string Detail) CompileAndRun(string source, int edition = 2002)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Oo_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CobolNet.CompilerDriver.Compile(new CobolNet.CompilerDriver.Options(
                src, dll, DialectLevel: edition));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            return CutRunner.Run(dll, dir);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    // ── The adversarial traps (deep-dive "Adversarial regression traps" — each a REAL legacy bug) ──────────

    /// <summary>Trap #4 — method fall-through: falling off a method's LAST paragraph is the implicit method
    /// return (the exit-bounded <c>__Dispatch(entry, last)</c> range), NEVER a run into the next method's
    /// paragraphs (the legacy <c>Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond</c> guard).</summary>
    [Fact]
    public void Trap4_MethodFallThrough_DoesNotEnterSiblingMethod()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP1", "OSPC1", """
                INVOKE OSPC1 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "AFTER-M1".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "M1-A".
            PARA-B.
                DISPLAY "M1-B".
            END METHOD M1.
            METHOD-ID. M2.
            PROCEDURE DIVISION.
            PARA-C.
                DISPLAY "M2-MUST-NOT-PRINT".
            END METHOD M2.
            """));
        Assert.True(ok, detail);
        Assert.Equal("M1-A\nM1-B\nAFTER-M1", CutRunner.Normalize(stdout));
    }

    /// <summary>Trap #10 — cross-method PERFORM: procedure names resolve METHOD-LOCALLY (§11.7), so a PERFORM
    /// of a sibling method's paragraph binds to the loud unknown-procedure guard (with the method-scope hint),
    /// never a silent cross-method transfer.</summary>
    [Fact]
    public void Trap10_CrossMethodPerform_FailsLoud()
    {
        var (ok, _, detail) = CompileAndRun(DriverAndClass("OOSP2", "OSPC2", """
                INVOKE OSPC2 "NEW" RETURNING T.
                INVOKE T "M1".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                PERFORM PARA-C.
            END METHOD M1.
            METHOD-ID. M2.
            PROCEDURE DIVISION.
            PARA-C.
                DISPLAY "M2-PARA".
            END METHOD M2.
            """));
        Assert.False(ok, "a cross-method PERFORM must fail loud, never transfer");
        Assert.Contains("unknown procedure 'PARA-C'", detail);
        Assert.Contains("method-local resolution", detail);
    }

    // ── D8: the method-return / run-unit-stop / program-return split (§14.9.18.4 GR4 / §14.9.43) ───────────

    /// <summary>GOBACK in a method terminates the METHOD only (§14.9.18.4 GR4; D8): the INVOKE site continues.
    /// The GOBACK sits inside an out-of-line PERFORM, so the MethodReturn signal must unwind the NESTED
    /// bounded __Dispatch frame too (the reason D8's realization is catch-at-entry, not a plain return) —
    /// and the paragraphs after the PERFORM and after the GOBACK must both be skipped.</summary>
    [Fact]
    public void MethodGoback_ReturnsFromMethodOnly_UnwindingNestedPerformFrames()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP3", "OSPC3", """
                INVOKE OSPC3 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "DRIVER-CONTINUES".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                PERFORM DEEP.
                DISPLAY "MUST-NOT-PRINT-1".
            PARA-B.
                DISPLAY "MUST-NOT-PRINT-2".
            DEEP.
                DISPLAY "IN-DEEP".
                GOBACK.
            END METHOD M1.
            """));
        Assert.True(ok, detail);
        Assert.Equal("IN-DEEP\nDRIVER-CONTINUES", CutRunner.Normalize(stdout));
    }

    /// <summary>STOP RUN inside a method terminates the RUN UNIT (§14.9.43) — it must NOT be caught at the
    /// method boundary as if it were a method return (the D8 anti-conflation fact).</summary>
    [Fact]
    public void StopRun_InsideMethod_TerminatesRunUnit()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP4", "OSPC4", """
                INVOKE OSPC4 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "MUST-NOT-PRINT".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "IN-METHOD".
                STOP RUN.
            END METHOD M1.
            """));
        Assert.True(ok, detail);
        Assert.Equal("IN-METHOD", CutRunner.Normalize(stdout));
    }

    /// <summary>EXIT METHOD is the method-return synonym in the editions that HAVE it (2002/2014; the
    /// exit-method-window registry row already 0902s it at 2023 — Annex E.2 removal, deep-dive correction #2):
    /// statements after it in the method are skipped; the INVOKE site continues.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    public void ExitMethod_Pre2023_IsMethodReturn(int edition)
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP5", "OSPC5", """
                INVOKE OSPC5 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "AFTER".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "BEFORE-EXIT".
                EXIT METHOD.
            PARA-B.
                DISPLAY "MUST-NOT-PRINT".
            END METHOD M1.
            """), edition);
        Assert.True(ok, detail);
        Assert.Equal("BEFORE-EXIT\nAFTER", CutRunner.Normalize(stdout));
    }

    // ── The OO diagnostic band (bind-time; the four-compilers rule's complete-behavior half) ────────────────

    private static IReadOnlyList<string> ErrorsOf(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "must be rejected");
        return errors;
    }

    /// <summary>§13.18.60.4 — the declared class of a typed object reference must be a class of the group.</summary>
    [Fact]
    public void TypedObjectReference_UnknownClass_0813()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE NOSUCH.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP6.
            """), "COBOLNET0813");

    /// <summary>§10.7 — END CLASS names its class.</summary>
    [Fact]
    public void EndClassNameMismatch_0820()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC7.
            END CLASS OTHER7.
            """), "COBOLNET0820");

    /// <summary>D9 (v1) — method names unique per class; §12063 overloading is optional and deferred.</summary>
    [Fact]
    public void DuplicateMethodName_0822()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC8.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            END METHOD M1.
            METHOD-ID. M1.
            END METHOD M1.
            END OBJECT.
            END CLASS OSPC8.
            """), "COBOLNET0822");

    /// <summary>Trap #8 — INHERITS of an unknown base is LOUD (0821), never silently a root class; a KNOWN
    /// base stages loud (0899 — port slice 3a) until inheritance emission lands.</summary>
    [Fact]
    public void InheritsUnknownBase_0821_KnownBaseStaged0899()
    {
        var unknown = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC9 INHERITS FROM NOBASE.
            END CLASS OSPC9.
            """);
        EditionHarness.AssertHasDiagnostic(unknown, "COBOLNET0821");
        var known = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPB10.
            END CLASS OSPB10.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC10 INHERITS FROM OSPB10.
            END CLASS OSPC10.
            """);
        EditionHarness.AssertHasDiagnostic(known, "COBOLNET0899");
        EditionHarness.AssertHasDiagnostic(known, "slice 3a");
    }

    /// <summary>§14.9.23.3 SR4d — for a TYPED receiver an unknown method is a COMPILE-time diagnostic (the
    /// static analog of EC-OO-METHOD, GR7b).</summary>
    [Fact]
    public void InvokeUnknownMethod_0825()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP11", "OSPC11", """
                INVOKE OSPC11 "NEW" RETURNING T.
                INVOKE T "NOSUCHM".
            """, """
            METHOD-ID. M1.
            END METHOD M1.
            """)), "COBOLNET0825");

    /// <summary>§16.2.1 — NEW's only result is the reference: RETURNING is required and USING is meaningless;
    /// the receiver must conform (§14.8 — a typed receiver of an UNRELATED class rejects).</summary>
    [Fact]
    public void InvokeNew_ShapeAndConformance_0826()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP12", "OSPC12", """
                INVOKE OSPC12 "NEW".
            """, "")), "COBOLNET0826");
        EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP13.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W USAGE OBJECT REFERENCE OSPD13.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC13 "NEW" RETURNING W.
                STOP RUN.
            END PROGRAM OOSP13.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC13.
            END CLASS OSPC13.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPD13.
            END CLASS OSPD13.
            """), "COBOLNET0826");
    }

    /// <summary>§14.9.14.3 SR7 — EXIT PROGRAM only in a program procedure division; the method form only in a
    /// method (both COBOLNET0827).</summary>
    [Fact]
    public void ExitPlacement_0827()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP14", "OSPC14", """
                INVOKE OSPC14 "NEW" RETURNING T.
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                EXIT PROGRAM.
            END METHOD M1.
            """)), "COBOLNET0827");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP15.
            PROCEDURE DIVISION.
            MAIN.
                EXIT METHOD.
                STOP RUN.
            END PROGRAM OOSP15.
            """), "COBOLNET0827");
    }

    /// <summary>The staged part-2 boundaries stay LOUD (never a silent drop): INVOKE USING/RETURNING on an
    /// instance method (slice 2) and INVOKE SELF (slice 3b) reach the runtime not-implemented guard.</summary>
    [Fact]
    public void StagedInvokeForms_FailLoud()
    {
        var (ok, _, detail) = CompileAndRun(DriverAndClass("OOSP16", "OSPC16", """
                INVOKE OSPC16 "NEW" RETURNING T.
                INVOKE T "M1" USING T.
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "X".
            END METHOD M1.
            """));
        Assert.False(ok, "INVOKE USING (slice 2) must stay loud");
        Assert.Contains("slice 2", detail);
    }
}
