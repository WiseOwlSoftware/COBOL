// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ §14.6.13.1.3 ITEM 8's ROLLBACK IS EMITTED BY ONE RULE, IN ONE PLACE (kb/Work PB3542, PB3543, PB3544). Item 8
/// (<i>"If checking for the exception condition is not enabled, … However, if there is a fatal exception condition for a
/// file subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed unconditionally."</i>) has three
/// parts, and each was once wrong at a different call site: the ROLLBACK was written after the hook, so a RESUME AT
/// procedure-name jumped past it (PB3542); it never asked whether checking was enabled (PB3543); and the SORT/MERGE
/// transfer hooks emitted it although item 2 hands those statements to their own rules (PB3544).
/// <c>SequentialIoEmitter.Item8Applies</c>, <c>EmitItem8Question</c> and <c>EmitItem8Rollback</c> are now the rule, and
/// every hook arm consumes them. These guards keep it so: the runtime's question and ROLLBACK are written into emitted C#
/// by exactly one method each, in <c>SequentialIoEmitter</c>; the runtime entry points are named only by
/// <c>RuntimeApi</c>; and every SORT/MERGE transfer hook passes <c>verbDisposes: true</c>, the fact by which the rule
/// leaves item 8 out of them. The scan walks the whole compiler project, so the class is a tree-wide sweep
/// (kb/Work PB3022).</summary>
[Trait("Sweep", "tree")]
public sealed class Item8RollbackSiteDriftTests
{
    private static readonly Regex Item8Write = new(@"\bRuntimeApi\.(RollbackOnFatal|RollbackDueOnFatal)\s*\(", RegexOptions.Compiled);
    private static readonly Regex Item8EntryName = new(@"\bnameof\s*\(\s*CobolFile\.(RollbackOnFatal|RollbackDueOnFatal)\s*\)", RegexOptions.Compiled);
    private static readonly Regex UseHookCall = new(@"\.EmitUseHook\(([^;]*);", RegexOptions.Compiled);

    /// <summary>The compiler project's C# with each line's comment removed (a citation may name the methods).</summary>
    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal)) continue;
            yield return (rel, string.Join('\n', File.ReadAllLines(file).Select(l =>
            {
                int c = l.IndexOf("//", StringComparison.Ordinal);
                return c < 0 ? l : l[..c];
            })));
        }
    }

    [Fact]
    public void Item8sQuestionAndRollback_AreWrittenOnlyByTheOneRuleSite()
    {
        var writes = new List<string>();
        foreach (var (rel, text) in CompilerSources())
            foreach (Match m in Item8Write.Matches(text))
                writes.Add($"{rel}: RuntimeApi.{m.Groups[1].Value}");
        Assert.Equal(
            ["CodeGen/Verbs/SequentialIoEmitter.cs: RuntimeApi.RollbackDueOnFatal",
             "CodeGen/Verbs/SequentialIoEmitter.cs: RuntimeApi.RollbackOnFatal"],
            writes.Order(StringComparer.Ordinal).ToArray());
        string emitter = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "SequentialIoEmitter.cs"));
        Assert.Contains("private string EmitItem8Question(", emitter, StringComparison.Ordinal);
        Assert.Contains("private void EmitItem8Rollback(", emitter, StringComparison.Ordinal);
    }

    [Fact]
    public void Item8sRuntimeEntryPoints_AreNamedOnlyByRuntimeApi()
    {
        var names = new List<string>();
        foreach (var (rel, text) in CompilerSources())
            foreach (Match m in Item8EntryName.Matches(text))
                names.Add($"{rel}: {m.Groups[1].Value}");
        Assert.Equal(
            ["CodeGen/Roslyn/RuntimeApi.cs: RollbackDueOnFatal", "CodeGen/Roslyn/RuntimeApi.cs: RollbackOnFatal"],
            names.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>§14.6.13.1.3 item 2 (<i>"If the executed statement is a MERGE or SORT statement, then the rules for those
    /// statements apply."</i>): every hook the SORT/MERGE emitter writes for its implicit transfers says so with
    /// <c>verbDisposes: true</c>, which is how <c>Item8Applies</c> leaves item 8 out of them.</summary>
    [Fact]
    public void EverySortMergeTransferHook_PassesVerbDisposes()
    {
        string sort = string.Join('\n', File.ReadAllLines(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "SortEmitter.cs")));
        var calls = UseHookCall.Matches(sort).Select(m => m.Groups[1].Value).ToList();
        Assert.True(calls.Count > 0, "the guard saw no EmitUseHook call in SortEmitter.cs: the emitter or the hook moved");
        Assert.All(calls, args => Assert.Contains("verbDisposes: true", args, StringComparison.Ordinal));
    }
}
