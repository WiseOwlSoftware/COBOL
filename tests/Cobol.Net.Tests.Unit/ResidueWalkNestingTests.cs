// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SYNTAX ERROR INSIDE NESTED FUNCTION CALLS IS REPORTED, NOT RE-SIMULATED FOREVER (kb/Work PB3489). The overnight
/// differential fuzz run found the compiler hanging on <c>MOVE FUNCTION DATE-OF-INTEGER(FUNCTION INTEGER-PART(FUNCTION
/// NUMVAL(FUNCTION SUBSTRING(A FROM 1 FOR 4)))) TO N</c>: the parse failed at FROM in under a second, and then the error
/// strategy's closed-format walk (<c>CobolErrorStrategy.ClosedFormatResidue</c>), which re-simulates the tokens the
/// failed prediction read, kept one call stack per path. Every argument reaches a function-identifier along many call
/// paths, so the distinct stacks multiplied with each nested argument list: one or two levels answered at once, three
/// did not answer in 90 seconds. The walk now runs on ANTLR's own full-context simulator (<c>ResidueWalk</c>), whose
/// graph-structured stacks merge the paths after every token.
/// <para>⛔ The witness is ALLOCATION, never the clock (kb/Work PB1590): the bytes the compiling thread allocates while
/// it checks the program, at depths 0, 1 and 2, after one discarded warm-up. A count of configurations cannot serve:
/// ANTLR's configuration set merges per (state, alternative, predicate), so the per-path walk's blow-up was in the
/// STACKS, which a configuration count never sees (branch review R1-1); every stack the walk builds is an allocation.
/// Depths 0–2 finish in a second even under the per-path walk, so a regression fails here by its count, never by
/// hanging the run. Measured (identical run to run): the walk on ANTLR's simulator 1.31 MB · 8.22 MB · 8.26 MB, the
/// per-path walk 1.29 MB · 10.4 MB · 63.7 MB. The bound — the second level costs at most three times the first — holds
/// at 0.006 and fails at 5.8.</para>
/// </summary>
public sealed class ResidueWalkNestingTests
{
    [Fact]
    public void NestedFunctionCallSyntaxError_WalkWorkIsLinearInDepth()
    {
        CheckAllocation(0);   // warm-up, discarded: the first compile in a process allocates its one-time state
        long zero = CheckAllocation(0), one = CheckAllocation(1), two = CheckAllocation(2);
        long first = one - zero, second = two - one;
        Assert.True(second <= 3 * Math.Max(first, 1),
            $"checking the error at depths 0, 1 and 2 allocated {zero:N0}, {one:N0} and {two:N0} bytes: the second level "
            + $"cost {second:N0} against {first:N0} for the first — the walk's stacks multiply per level, the blow-up of "
            + "kb/Work PB3489 has regressed");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void NestedFunctionCallSyntaxError_IsReportedAtTheOffendingWord(int depth)
        => Assert.Contains(Check(depth).Errors, e => e.Contains("COBOL0001") && e.Contains("near 'FROM'"));

    private static long CheckAllocation(int depth)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Check(depth);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>Compile the fuzz program's shape with <paramref name="depth"/> FUNCTION calls around the malformed
    /// argument list; it must be rejected.</summary>
    private static CompilerDriver.Result Check(int depth)
    {
        string call = "A FROM 1 FOR 4";
        for (int i = 0; i < depth; i++) call = $"FUNCTION NUMVAL({call})";
        string source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB3489U.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 A PIC X(10).\n01 N PIC 9(8).\nPROCEDURE DIVISION.\nMAIN-PARA.\n"
            + $"MOVE {call} TO N\nSTOP RUN.\n";
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB3489_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb3489u.cob");
            File.WriteAllText(src, source);
            var result = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3489u.dll"), DialectLevel: 2014, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
            Assert.False(result.Success, "the source is non-conforming and must be rejected");
            return result;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
