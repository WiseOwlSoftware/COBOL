// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE OVERLAPPING-MOVE RULE IS DECIDED ONCE, AT BIND TIME, AND RENDERED BY ONE SWITCH (kb/Work PB1907;
/// docs/CONFORMANCE.md §3 D-OVL1/D-OVL2).
/// <para>ISO §14.6.10 1) leaves the result of an overlapping MOVE undefined (Annex A.2 item 36), so WiseOwl COBOL
/// documents what it does and the corpus golden <c>85/pb1907_overlap_move</c> pins it. This class pins the SHAPE
/// that keeps the rule from spreading: <c>MoveOverlap.Classify</c> is the only place an
/// <see cref="OverlapStore"/> is built, <c>StorageExtent</c> is the only static-overlap predicate, and
/// <c>MoveEmitter.EmitOverlapStore</c> renders every <see cref="OverlapStoreKind"/> — so a new corner is one
/// enum member, one classifier arm and one emitter arm, and cannot be classified without being rendered.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class MoveOverlapDriftTests
{
    private static string Src(params string[] parts) =>
        File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", .. parts]));

    [Fact]
    public void OnlyTheClassifier_BuildsAnOverlapStore()
    {
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is "MoveOverlap.cs") continue;
            Assert.False(Regex.IsMatch(File.ReadAllText(file), @"new\s+OverlapStore\s*\("),
                $"{name} builds an OverlapStore. The overlapping-MOVE decision is MoveOverlap.Classify's alone, "
                + "carried on MoveStore.Overlap, so the emitter renders it and re-derives nothing (kb/Work PB1907).");
        }
    }

    [Fact]
    public void OnlyTheClassifier_AsksWhetherTwoOperandsStaticallyOverlap()
    {
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is "MoveOverlap.cs" or "StorageExtent.cs") continue;
            Assert.False(Regex.IsMatch(File.ReadAllText(file), @"StorageExtent\s*\.\s*Of\s*\("),
                $"{name} asks StorageExtent.Of. Static overlap is a bind-time fact read through MoveOverlap "
                + "(kb/Work PB1907); a second asker is a second rule.");
        }
    }

    [Fact]
    public void TheEmitterRendersEveryStoreKind_AndPassesTheStoresOverlap()
    {
        string emitter = Src("CodeGen", "Verbs", "MoveEmitter.cs");
        foreach (var kind in Enum.GetValues<OverlapStoreKind>().Where(k => k is not OverlapStoreKind.None))
            Assert.Contains($"case OverlapStoreKind.{kind}:", emitter);
        Assert.Equal(2, Regex.Matches(emitter, @"m\.Stores\[i\]\.Overlap").Count);
    }

    /// <summary>Every in-place kind IS a runtime algorithm of the same name (<c>OverlappingMove.&lt;Kind&gt;</c>) with its
    /// emitted spelling in <c>RuntimeApi</c>, so a kind cannot be classified without an algorithm to call.</summary>
    [Fact]
    public void EveryInPlaceKind_IsARuntimeAlgorithm_AndHasItsRuntimeApiSpelling()
    {
        string api = Src("CodeGen", "Roslyn", "RuntimeApi.cs");
        foreach (var kind in Enum.GetValues<OverlapStoreKind>().Where(k => new OverlapStore(k).IsInPlace))
        {
            Assert.NotNull(typeof(CobolNet.Runtime.OverlappingMove).GetMethod(kind.ToString()));
            Assert.Contains($"nameof(OverlappingMove.{kind})", api);
        }
    }
}
