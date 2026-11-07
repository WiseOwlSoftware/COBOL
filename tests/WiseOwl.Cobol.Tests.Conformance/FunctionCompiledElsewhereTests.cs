// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB2103 (DESIGN-external-repository §4.2) — the FUNCTION arm of "a site whose callee is compiled elsewhere
/// describes its operands", end to end on the arm the manifest goldens cannot observe: EC-PROGRAM-ARG-MISMATCH is fatal and
/// a function reference has no ON EXCEPTION phrase, so the run unit ends abnormally (the golden harness requires exit 0).
/// The CALL arm is the golden <c>conformance:wiseowl/cases/14.9.4-CALL/pb2103_prototype_callee_compiled_elsewhere</c>.
/// <para>ISO §2023:12.3.8.4 GR11 b): with a function prototype definition in the group, "the details are taken from that
/// function prototype definition", so §2023:8.4.3.2.3 SR13's conformance is checked at bind time against the PROTOTYPE. The
/// definition is compiled in another group and may differ; §2023:14.9.4.4 GR3 d) — "the rules for conformance specified in
/// 14.8.2, Parameters and 14.8.3, Returning items apply. If a violation of these rules is detected, the
/// EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if checking for it is enabled in both the activated
/// program and activating runtime element" — is therefore asked again at the activation, and §2023:14.8.2.3.2 rule 2 ("a
/// function") compares the clauses. Before the fix a function reference never described its arguments, so the stale
/// definition read a PIC 9(4) argument as PIC X(4) with no diagnostic.</para>
/// </summary>
public sealed class FunctionCompiledElsewhereTests
{
    private const string Library = """
        >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
        IDENTIFICATION DIVISION.
        FUNCTION-ID. F2103X.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC X(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING L RETURNING R.
            DISPLAY "F2103X GOT " L
            MOVE 7 TO R
            GOBACK.
        END FUNCTION F2103X.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. F2103N.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING L RETURNING R.
            DISPLAY "F2103N GOT " L
            MOVE 8 TO R
            GOBACK.
        END FUNCTION F2103N.
        """;

    /// <summary>The caller: a prototype of each function saying PIC 9(4), and a reference to <paramref name="function"/>.</summary>
    private static string Caller(string function) => $"""
        >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
        IDENTIFICATION DIVISION.
        FUNCTION-ID. F2103X IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING L RETURNING R.
        END FUNCTION F2103X.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. F2103N IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING L RETURNING R.
        END FUNCTION F2103N.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P2103F.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION F2103X
            FUNCTION F2103N.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N4 PIC 9(4) VALUE 1234.
        01 R4 PIC 9(4) VALUE 0.
        PROCEDURE DIVISION.
            DISPLAY "BEFORE"
            MOVE FUNCTION {function}(N4) TO R4
            DISPLAY "AFTER " R4
            STOP RUN.
        END PROGRAM P2103F.
        """;

    private static void CompileTo(string source, string dir, string name)
    {
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, name + ".cob"), source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"),
            DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>The definition compiled elsewhere describes its formal PIC X(4) where the prototype the reference was
    /// checked against says PIC 9(4): the activation is not successful, the function never runs, and the unhandled fatal
    /// condition ends the run unit (§2023:14.6.12).</summary>
    [Fact]
    public void AFunctionDefinedElsewhere_WithAnotherFormal_IsEcProgramArgMismatch_AtTheActivation()
    {
        string dir = ScratchDirectory.Create("pb2103fx");
        try
        {
            CompileTo(Library, dir, "F2103LIB");
            CompileTo(Caller("F2103X"), dir, "P2103F");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "P2103F.dll"), dir);
            Assert.Equal("BEFORE", stdout);
            Assert.Equal(1, exit);
            Assert.Contains("EC-PROGRAM-ARG-MISMATCH", stderr, StringComparison.Ordinal);
            Assert.Contains("ISO §2023:14.8.2.3.2 rule 2", stderr, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The control: the definition compiled elsewhere is described as its prototype is, so the described
    /// argument conforms and the function runs.</summary>
    [Fact]
    public void AFunctionDefinedElsewhere_AsItsPrototypeSays_Runs()
    {
        string dir = ScratchDirectory.Create("pb2103fn");
        try
        {
            CompileTo(Library, dir, "F2103LIB");
            CompileTo(Caller("F2103N"), dir, "P2103F");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "P2103F.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BEFORE\nF2103N GOT 1234\nAFTER 0008", stdout);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
