// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NESTED FUNCTION CALLS IN AN EVALUATE OPERAND PARSE IN WORK LINEAR IN THE NESTING (kb/Work PB3504). Legal source
/// <c>EVALUATE FUNCTION ABS(FUNCTION ABS(FUNCTION ABS(FUNCTION ABS(N)))) WHEN 1 …</c> never finished: the EVALUATE
/// subject's adaptive prediction looks through the whole operand, and ANTLR's SLL conflict test
/// (<c>PredictionMode.HasSLLConflictTerminatingPrediction</c>) copied the predicated configurations WITHOUT a merge
/// cache, re-merging every shared prediction-context subgraph once per path. The fix is in the ANTLR runtime itself, the
/// fork's build in <c>nuget-feed/</c> (README there); this test is what holds a later runtime pin to it.
/// <para>⛔ The witness is ALLOCATION, never the clock (kb/Work PB1590): the bytes the compiling thread allocates while
/// it checks the program, at depths 1, 2 and 3, after one discarded warm-up compile. Lookahead depth, ATN transitions and
/// the configuration count stay linear even under the defect (ANTLR merges configurations per state); what multiplied
/// was the prediction contexts the merges built, and every one is an allocation. Measured (identical to a few hundred
/// bytes run to run): the fixed runtime 169 KB · 48.2 MB · 30.6 MB, the defective 4.13.2 169 KB · 192 MB · 2,335 MB.
/// The bound — the third level costs at most three times the second — holds at 0.6 fixed and fails at 11 defective, at
/// depths that finish in seconds either way. The parser's DFA cache is shared by every parse in the process, so the
/// numbers assume no other Unit test predicts an EVALUATE operand holding two or three nested calls first; one that did
/// would shrink the second level's cost and could only make this test stricter, never mask the defect.</para>
/// </summary>
public sealed class EvaluateNestedOperandPredictionTests
{
    [Fact]
    public void NestedFunctionCallsInAnEvaluateSubject_PredictionWorkIsLinearInDepth()
    {
        CheckAllocation(1);   // warm-up, discarded: the first compile in a process allocates its one-time state
        long one = CheckAllocation(1), two = CheckAllocation(2), three = CheckAllocation(3);
        long first = two - one, second = three - two;
        Assert.True(second <= 3 * Math.Max(first, 1),
            $"checking the EVALUATE with 1, 2 and 3 nested calls allocated {one:N0}, {two:N0} and {three:N0} bytes: the "
            + $"third level cost {second:N0} against {first:N0} for the second — prediction multiplies per level, the "
            + "ANTLR merge-cache defect of kb/Work PB3504 is back (is the Antlr4.Runtime.Standard pin the fork's build?)");
    }

    [Theory]
    [InlineData("EVALUATE {0} WHEN 1 CONTINUE END-EVALUATE")]
    [InlineData("EVALUATE N WHEN {0} CONTINUE END-EVALUATE")]
    [InlineData("EVALUATE TRUE WHEN {0} = 1 CONTINUE END-EVALUATE")]
    [InlineData("EVALUATE N WHEN {0} THRU 9 CONTINUE END-EVALUATE")]
    public void SixNestedFunctionCallsInAnEvaluateOperand_Compile(string statement)
        => Assert.True(Check(statement, 6).Success, "a legal EVALUATE with six nested FUNCTION calls must compile");

    private static long CheckAllocation(int depth)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = Check("EVALUATE {0} WHEN 1 CONTINUE END-EVALUATE", depth);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(result.Success, $"depth {depth}: {string.Join("; ", result.Errors)}");
        return allocated;
    }

    private static CompilerDriver.Result Check(string statement, int depth)
    {
        string operand = "N";
        for (int i = 0; i < depth; i++) operand = $"FUNCTION ABS({operand})";
        string source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB3504U.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 N PIC S9(8).\nPROCEDURE DIVISION.\nMAIN-PARA.\n" + string.Format(statement, operand) + "\nSTOP RUN.\n";
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB3504_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb3504u.cob");
            File.WriteAllText(src, source);
            return CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3504u.dll"), DialectLevel: 2014, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
