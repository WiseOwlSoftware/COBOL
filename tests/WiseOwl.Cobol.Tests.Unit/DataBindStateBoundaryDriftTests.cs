// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH SLICE OF THE BINDER'S STATE HAS ONE PRODUCER, AND EACH PROCEDURE-BIND SHAPE CHANGES ONLY THROUGH ITS OWN API
/// (kb/Work PB2296; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para>Step 1 of the <c>DataBinder</c> extract regrouped the binder's state: <c>DataBindState</c> holds the value slices
/// and the four append-only fact sets, <c>ProcedureBindState</c> the scoped override, the drain queues and the cursors.
/// The C# types hold most of it (a fact set admits no removal; a scope cursor and a scoped override change through a
/// restoring scope; a drain queue gives up entries only as a mark's suffix). What the types cannot say is WHO writes a
/// value slice, so this test says it, by a scan of the compiler's source:</para>
/// <list type="bullet">
/// <item>the checking slices are written only with <c>CheckingFold</c>'s answer (<c>State.UnitChecking = CheckingFold.…</c>
/// and <c>.Checking = CheckingFold.Method(…)</c>), so no second fold of a <c>&gt;&gt;TURN</c> query can appear;</item>
/// <item>the inherited-globals slice is written only by <c>InheritStep</c>, so no second inheritance path can
/// appear.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DataBindStateBoundaryDriftTests
{
    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Fact]
    public void TheCheckingSlices_AreWrittenOnlyWithCheckingFoldsAnswer()
    {
        var writes = new Regex(@"(\.UnitChecking|\.Checking)\s*=(?!=)\s*(?<rhs>[^;]*);");
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
            foreach (Match m in writes.Matches(text))
            {
                found++;
                if (!m.Groups["rhs"].Value.TrimStart().StartsWith("CheckingFold.", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        Assert.True(found >= 4, $"the scan found only {found} checking-slice writes — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "A checking slice is written with something other than CheckingFold's answer — fold it in CheckingFold "
            + "(codegen owns no TurnState, and one fold per element is the rule):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheInheritedGlobalsSlice_IsWrittenOnlyByInheritStep()
    {
        var writers = new Regex(@"\.(AddContainerFiles|AddContainerTypeDecls|OfferContainerGlobals)\(");
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
        {
            if (file.EndsWith("/Binding/InheritedGlobals.cs", StringComparison.Ordinal)) continue;
            foreach (Match m in writers.Matches(text))
            {
                found++;
                if (!file.EndsWith("/Binding/InheritStep.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        }
        Assert.True(found >= 3, $"the scan found only {found} inherited-globals writes — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "The inherited-globals slice is written outside InheritStep — a contained program's inheritance has ONE "
            + "producer (ISO §13.18.27.4 GR1–GR2):\n  " + string.Join("\n  ", offenders));
    }
}
