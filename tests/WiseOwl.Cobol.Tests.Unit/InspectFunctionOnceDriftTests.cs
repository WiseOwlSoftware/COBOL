// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN INSPECT EVALUATES A FUNCTION-IDENTIFIER ONCE, HOWEVER ITS COUNTERS OVERLAP WHAT THE STATEMENT READS (kb/Work
/// PB4115). A function-identifier "references a temporary data item whose value is determined when the function is
/// referenced at runtime" (ISO §2023:8.4.3.2.4 GR1), and INSPECT references each identifier once (§2023:14.9.22.4 GR6), so
/// no counter can occupy that item and the D-INS1 re-run of the tallying cycle (docs/CONFORMANCE.md) re-reads only
/// DATA ITEMS. The golden <c>conformance:cases/14.9.22-INSPECT/pb4115_inspect_function_evaluated_once</c> pins the
/// observable results; this class pins what no golden can observe deterministically: a clock function such as
/// CURRENT-DATE as identifier-1 of a multi-counter TALLYING is called exactly once (it was called once per operand),
/// and a function operand is evaluated into one local ahead of the cycle and never rendered into the re-run.
/// </summary>
public sealed class InspectFunctionOnceDriftTests
{
    private static string EmitCSharp(string data, string procedure)
    {
        string dir = ScratchDirectory.Create("pb4115drift");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, $$"""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. PB4115DRIFT.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                {{data}}
                PROCEDURE DIVISION.
                MAIN.
                    {{procedure}}
                    STOP RUN.
                """);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static int Count(string cs, string pattern) => Regex.Matches(cs, pattern).Count;

    [Fact]
    public void FunctionIdentifier1_IsEvaluatedOnce_ThoughEveryCounterMayChangeAnOperand()
    {
        // C1 is the pattern of the second operand, so the cycle IS re-run after C1 is stored (D-INS1); the clock is not read again.
        string cs = EmitCSharp("""
            01 C1 PIC 9(4) VALUE 0.
            01 C2 PIC 9(4) VALUE 0.
            """, """INSPECT FUNCTION CURRENT-DATE TALLYING C1 FOR ALL "0" C2 FOR ALL C1""");
        Assert.Equal(1, Count(cs, @"CobolDate\.CurrentDate\("));
        Assert.Equal(2, Count(cs, @"CobolInspect\.Tally\("));
    }

    [Fact]
    public void FunctionOperandOverACounter_IsEvaluatedOnceIntoALocal_AndTheCycleIsNotReRun()
    {
        string cs = EmitCSharp("""
            01 Q.
               05 QT PIC 9 VALUE 0.
               05 QP PIC X VALUE "X".
            01 S PIC X(3) VALUE "B1X".
            """, """INSPECT S TALLYING QT FOR ALL "B" QT FOR ALL FUNCTION UPPER-CASE(Q)""");
        Assert.Equal(1, Count(cs, @"CobolIntrinsics\.UpperCase\("));
        Assert.Matches(@"var __insOp\d+_0 = CobolIntrinsics\.UpperCase\(", cs);
        Assert.Equal(1, Count(cs, @"CobolInspect\.Tally\("));
    }
}
