// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

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
/// it checks the program, at depths 0, 1 and 2. A count of configurations cannot serve: ANTLR's configuration set merges
/// per (state, alternative, predicate), so the per-path walk's blow-up was in the STACKS, which a configuration count
/// never sees (branch review R1-1); every stack the walk builds is an allocation. Depths 0–2 finish in a second even
/// under the per-path walk, so a regression fails here by its count, never by hanging the run.</para>
/// <para>⛔ Every depth is measured WARM, as the least of three compiles after a discarded one at that same depth (branch
/// review R2-1): the parse's adaptive prediction fills the parser's process-wide shared DFA cache, so a cold compile's
/// bytes depend on which compiles ran before it, on any thread, in this test process (measured cold, the second level
/// cost 0.006 times the first when nothing ran before and 13.5 times after one malformed one-level FUNCTION argument
/// list, so the correct walk failed or passed by the schedule). A warm compile adds nothing to that
/// cache, so its bytes are the walk's and the parse's own, whatever ran before; the least of three also drops a fill a
/// concurrent compile could cause. Measured warm (identical run to run, and after a cold start, a malformed one-level
/// FUNCTION list, a valid one, the Theory's depths, or another thread compiling throughout): the walk on ANTLR's
/// simulator 1,308,848 · 1,773,072 · 2,773,584 bytes, so the second level costs 2.16 times the first; the per-path walk
/// 1.29 MB · 3.98 MB · 58.2 MB, 20.1 times. The first level is cheaper than the levels above it (464 KB against about
/// 1 MB, then 916 KB at depths 3 and 4): depth 0 is not a FUNCTION argument at all. The bound, six, is the geometric
/// mean of the two ratios rounded down, so each sits about three times from it. Depth 3 cannot be measured: the
/// per-path walk does not answer there in 90 seconds.</para>
/// </summary>
public sealed class ResidueWalkNestingTests
{
    private const int MaxSecondToFirstLevelRatio = 6;

    [Fact]
    public void NestedFunctionCallSyntaxError_WalkWorkIsLinearInDepth()
    {
        long zero = WarmAllocation(0), one = WarmAllocation(1), two = WarmAllocation(2);
        long first = one - zero, second = two - one;
        Assert.True(first > 0, $"checking the error one FUNCTION call deep allocated {one:N0} bytes, no more than at depth "
            + $"0 ({zero:N0}): the program no longer exercises the walk, so the bound below proves nothing");
        Assert.True(second <= MaxSecondToFirstLevelRatio * first,
            $"checking the error at depths 0, 1 and 2 allocated {zero:N0}, {one:N0} and {two:N0} bytes warm: the second "
            + $"level cost {second:N0} against {first:N0} for the first, above {MaxSecondToFirstLevelRatio} times — the "
            + "walk's stacks multiply per level, the blow-up of kb/Work PB3489 has regressed");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void NestedFunctionCallSyntaxError_IsReportedAtTheOffendingWord(int depth)
        => Assert.Contains(Check(depth).Errors, e => e.Contains("COBOL0001") && e.Contains("near 'FROM'"));

    /// <summary>The bytes the compiling thread allocates to check the program at <paramref name="depth"/>, warm: the least
    /// of three compiles after a discarded one, so the parser's shared DFA cache holds everything this program's parse
    /// adds to it and the measure does not depend on what ran before (branch review R2-1).</summary>
    private static long WarmAllocation(int depth)
    {
        Check(depth);
        long least = long.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            Check(depth);
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return least;
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
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB3489_");
        try
        {
            string src = Path.Combine(dir, "pb3489u.cob");
            File.WriteAllText(src, source);
            var result = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3489u.dll"), DialectLevel: 2014, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
            Assert.False(result.Success, "the source is non-conforming and must be rejected");
            return result;
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
