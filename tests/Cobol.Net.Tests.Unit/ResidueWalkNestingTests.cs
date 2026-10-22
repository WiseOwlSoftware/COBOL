// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Parsing;
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
/// did not answer in 90 seconds. The walk now shares its stacks as one graph (<c>CallStacks</c>), merged after every
/// token as ANTLR's own prediction merges its contexts.
/// <para>⛔ These tests count the WORK, never the clock (kb/Work PB1590): the walk reports the configurations its
/// closures visited (<see cref="CobolErrorStrategy.ResidueWalkObserver"/>), and the count must grow by the same amount
/// for every added level. The depths stay inside the walk's 16-token window (five calls is 16 tokens to FROM), so every
/// depth exercises the walk. Measured: merged, depths 1 to 5 visit 11,098 · 20,952 · 30,806 · 40,660 · 50,514; unmerged,
/// depths 1 and 2 visited 19,585 and 320,041 and depth 3 did not finish. So depths 1 and 2 are asked FIRST, against a
/// bound the unmerged walk breaks by an order of magnitude, and a regression fails there by its count before a deeper
/// level could hang the run.</para>
/// </summary>
public sealed class ResidueWalkNestingTests
{
    [Fact]
    public void NestedFunctionCallSyntaxError_WalkWorkIsLinearInDepth()
    {
        long one = WalkVisits(1).Visits, two = WalkVisits(2).Visits;
        long step = two - one;
        Assert.True(step > 0 && step <= one,
            $"one more nesting level cost {step} walk visits against {one} for the first: the call stacks multiply per "
            + "level — the blowup of kb/Work PB3489 has regressed");

        long[] visits = [one, two, .. Enumerable.Range(3, 3).Select(depth => WalkVisits(depth).Visits)];
        for (int d = 2; d < visits.Length; d++)
            Assert.True(visits[d] - visits[d - 1] == step,
                $"the closed-format walk's work per nesting level changed at depth {d + 1} ({string.Join(", ", visits)}): "
                + "not linear — the call-stack blowup of kb/Work PB3489 has regressed");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void NestedFunctionCallSyntaxError_IsReportedAtTheOffendingWord(int depth)
    {
        var errors = WalkVisits(depth).Errors;
        Assert.Contains(errors, e => e.Contains("COBOL0001") && e.Contains("near 'FROM'"));
    }

    /// <summary>Compile the fuzz program's shape with <paramref name="depth"/> FUNCTION calls around the malformed
    /// argument list, and return the closed-format walk's visit count with the compiler's errors.</summary>
    private static (long Visits, IReadOnlyList<string> Errors) WalkVisits(int depth)
    {
        string call = "A FROM 1 FOR 4";
        for (int i = 0; i < depth; i++) call = $"FUNCTION NUMVAL({call})";
        string source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB3489U.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 A PIC X(10).\n01 N PIC 9(8).\nPROCEDURE DIVISION.\nMAIN-PARA.\n"
            + $"MOVE {call} TO N\nSTOP RUN.\n";
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB3489_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        long visits = 0;
        CobolErrorStrategy.ResidueWalkObserver.Value = n => visits += n;   // AsyncLocal: this test's own parse only
        try
        {
            string src = Path.Combine(dir, "pb3489u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3489u.dll"), DialectLevel: 2014, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
            Assert.False(r.Success, "the source is non-conforming and must be rejected");
            return (visits, r.Errors);
        }
        finally
        {
            CobolErrorStrategy.ResidueWalkObserver.Value = null;
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
