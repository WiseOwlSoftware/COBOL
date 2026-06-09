// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text;
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Regression for the deeply-nested-group emission blowup (DEVLOG 502): a CCVS NC test (NC126A/NC207A/NC246A) nests a
/// group ~49 levels deep with a leaf and a subgroup at each level. The DATA-DIVISION emitter recomputed each node's
/// physical width and composed initializer independently, each recursing — O(2^depth) time, so the compiler HUNG.
/// <see cref="CobolNet.CodeGen.Emit.FieldEmitter"/> now memoizes the per-item physical-field list, making emission
/// linear. This test compiles a group deeper than the exponential cliff (~28 levels) and asserts it finishes promptly.
/// </summary>
public sealed class DeepNestingTests : CobolNetTestBase
{
    [Fact]
    public void DeeplyNestedGroup_CompilesInLinearTime()
    {
        const int depth = 45;   // far past the old exponential cliff (~28 levels hung)
        var sb = new StringBuilder();
        sb.AppendLine("IDENTIFICATION DIVISION.");
        sb.AppendLine("PROGRAM-ID. DEEP.");
        sb.AppendLine("DATA DIVISION.");
        sb.AppendLine("WORKING-STORAGE SECTION.");
        sb.AppendLine("01 G.");
        for (int i = 2; i < depth; i++)
        {
            sb.AppendLine($"{i:00} GP-{i} PIC 9.");   // a leaf AND a subgroup at every level (the CCVS shape)
            sb.AppendLine($"{i:00} GL-{i}.");          // nesting is by level number — no indentation required
        }
        sb.AppendLine($"{depth:00} LEAF PIC 9.");
        sb.AppendLine("PROCEDURE DIVISION.");
        sb.AppendLine("MAIN. DISPLAY \"OK\". STOP RUN.");

        string srcPath = Path.Combine(TempDir, "deep.cob");
        File.WriteAllText(srcPath, sb.ToString());

        var sw = Stopwatch.StartNew();
        var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, Path.Combine(TempDir, "deep.dll")));
        sw.Stop();

        Assert.True(result.Success, $"deep-nesting compile failed: {result.Status}: {string.Join("\n", result.Errors)}");
        // Linear emission compiles a 45-level group in well under a second of emit time; the exponential bug took
        // minutes by depth 30. A generous ceiling (Roslyn dominates the wall time) still catches a regression.
        Assert.True(sw.Elapsed.TotalSeconds < 20, $"compile took {sw.Elapsed.TotalSeconds:F1}s — the nested-group blowup may have regressed");
    }
}
