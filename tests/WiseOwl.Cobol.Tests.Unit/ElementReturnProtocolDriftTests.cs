// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB6140 — ⛔ A SOURCE ELEMENT RETURNS TO ITS ACTIVATOR DIRECTLY WHERE EVERY FRAME ABOVE IT TESTS FOR THE RETURN, AND
/// THROWS ITS RETURN SIGNAL EVERYWHERE ELSE; ONE PLACE SPELLS BOTH.
/// <para>GOBACK, a called program's EXIT PROGRAM, a method's GOBACK and the §2023:14.6.3 implicit GOBACK (ISO §2023:14.9.18.4
/// GR2/GR4, §2023:14.9.14.4 GR3) used to throw <c>ProgramReturn</c> / <c>MethodReturn</c> from every frame, about 2 us a CALL.
/// Now a dispatch frame whose <c>__direct</c> argument is true returns <c>ElementReturn.Returned</c>, and the frames that pass
/// <c>__direct</c> on are exactly the ones that test for it (<c>DispatchState.DispatchCall</c>) or ignore a finished run (the
/// activation entry). A frame entered any other way (<c>__RunUse</c>, <c>__RunDebug</c>) gets false, so a return below it is
/// thrown, through the exception filter <c>__RunUse</c> keeps for §2023:14.9.40.4 GR17. A dispatch call that passed
/// <c>__direct</c> without testing the result would resume a PERFORM the program had left: these facts pin the pairing.</para>
/// </summary>
public sealed class ElementReturnProtocolDriftTests
{
    private const string Src = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RETDRIFT.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "retdrift.dat" FILE STATUS IS FS.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 FR PIC X(10).
        WORKING-STORAGE SECTION.
        01 FS PIC XX.
        01 J PIC 9 VALUE 0.
        LINKAGE SECTION.
        01 CNT PIC 9(4).
        PROCEDURE DIVISION USING CNT.
        DECLARATIVES.
        D-ERR SECTION.
            USE AFTER STANDARD ERROR PROCEDURE ON F.
        D1.
            PERFORM D2.
        D2.
            ADD 100 TO CNT
            GOBACK.
        END DECLARATIVES.
        M SECTION.
        M0.
            OPEN INPUT F
            PERFORM M1.
        M1.
            PERFORM VARYING J FROM 1 BY 1 UNTIL J > 3
                PERFORM M2
            END-PERFORM.
        M2.
            IF J > 1
                EXIT PROGRAM
            END-IF
            GOBACK.
        """;

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"retdrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = TestSourceFormat.Inline, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            return new CSharpEmitter().EmitBound(new BinderDriver().Bind(tree!, new EditionContext(2002)), "RETDRIFT");
        }
        finally { FileHarness.TryDelete(path); }
    }

    private const string Direct = "if (__direct) return ElementReturn.Returned; throw new ProgramReturn();";

    /// <summary>A dispatch call: the method, its argument list (no nested parentheses in either shape the emitter writes).</summary>
    private static readonly Regex DispatchCall = new(@"__M?Dispatch\((?<args>[^()]*)\)", RegexOptions.Compiled);

    /// <summary>The emitted C# as one line of statements: the listing's <c>#line</c> directives dropped and every run of white
    /// space one blank, because the writer gives an <c>if</c> and its controlled statement two sequence points.</summary>
    private static string Flat(string cs) => Regex.Replace(
        string.Join(" ", cs.Split('\n').Where(l => !l.TrimStart().StartsWith("#line", StringComparison.Ordinal))), @"\s+", " ");

    [Fact]
    public void EveryDispatchCallThatPassesDirectTestsItsResult()
    {
        string cs = Flat(Emit(Src));
        int passed = 0;
        foreach (Match m in DispatchCall.Matches(cs))
        {
            string args = m.Groups["args"].Value;
            if (args.Contains("int __startPc", StringComparison.Ordinal)) continue;   // the method's own signature
            if (args.EndsWith("__direct: true", StringComparison.Ordinal))
            {
                // The activation entry: the top frame's result is not read, so a direct return simply ends the run.
                Assert.StartsWith($"{m.Value}; }} catch (ProgramReturn)", cs[m.Index..], StringComparison.Ordinal);
                continue;
            }
            if (!args.Contains("__direct", StringComparison.Ordinal)) continue;   // __RunUse, __RunDebug: the thrown form below
            passed++;
            string call = cs[Math.Max(0, m.Index - 4)..];
            Assert.True(call.StartsWith($"if ({m.Value} == ElementReturn.Returned) return ElementReturn.Returned;", StringComparison.Ordinal),
                "a dispatch call passes __direct but does not return ElementReturn.Returned when its range did:\n  "
                + call[..Math.Min(call.Length, 120)]);
        }
        Assert.True(passed >= 2, $"the fixture's out-of-line PERFORMs rendered {passed} tested dispatch calls");
    }

    [Fact]
    public void ReturnsAreRenderedInBothFormsAndTheDeclarativeRunsBelowADirectFalseFrame()
    {
        string cs = Flat(Emit(Src));
        // GOBACK in M2 and in the declarative's D2, EXIT PROGRAM in M2, and the dispatcher's implicit GOBACK.
        int returns = Regex.Matches(cs, Regex.Escape(Direct)).Count;
        Assert.True(returns >= 4, $"expected the GOBACKs, the EXIT PROGRAM and the implicit GOBACK in the one form, found {returns}");
        Assert.Equal(returns, Regex.Matches(cs, Regex.Escape("throw new ProgramReturn();")).Count);   // no bare throw left behind
        // The declarative's bounded run passes no __direct, so its GOBACK throws through the GR17 filter.
        Assert.Matches(new Regex(@"try \{ __Dispatch\(__startPc, __endPc\); \}"), cs);
        Assert.Contains("catch (ProgramReturn) when (", cs, StringComparison.Ordinal);
    }

    /// <summary>The thrown signal is written by <c>DispatchState.ReturnToActivator</c> and, for the automatic propagation of an
    /// unhandled fatal condition (§2023:14.6.13.1.3 GR6), by <c>EcEmitter</c>, whose text is also emitted into the generated
    /// <c>__IoCheckEc</c> helper, a method with no dispatch frame and so no <c>__direct</c>. No other emitter writes either, and
    /// only <c>DispatchState</c> writes the direct value.</summary>
    [Fact]
    public void OnlyTheProtocolOwnersWriteTheReturn()
    {
        var throwSite = new Regex(@"throw new (ProgramReturn|MethodReturn|\{[A-Za-z.]*ReturnSignal\([a-zA-Z]*\)\})\(\)");
        // POSITIVE CONTROL: the site the change replaced in CallEmitter.EmitGoback must still be recognized.
        Assert.Matches(throwSite, "        w.Line(\"throw new ProgramReturn();   // return to the activator\");");
        Assert.Matches(throwSite, "w.Line($\"if (__pc == {toPc + 1}) throw new {ReturnSignal(inMethod)}();\");");

        var allowedThrow = new HashSet<string>(StringComparer.Ordinal) { "EmitterState.cs", "EcEmitter.cs" };
        var bad = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string name = Path.GetFileName(file);
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (throwSite.IsMatch(line) && !allowedThrow.Contains(name)) bad.Add($"{name}: {t}");
                if (line.Contains("ElementReturn.Returned", StringComparison.Ordinal) && name != "EmitterState.cs") bad.Add($"{name}: {t}");
            }
        }
        Assert.True(bad.Count == 0,
            "an element's return is written outside DispatchState.ReturnToActivator / DispatchCall (kb/Work PB6140):\n  "
            + string.Join("\n  ", bad));
    }
}
