// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB2723 — <b>THE §2023:14.9.49.4 GR3 SELECTION TIERS ARE DECIDED ONCE AND RENDERED ONCE.</b> §2023:14.9.28.4 GR17
/// makes a Format-3 PERFORM's WHEN match the USE rule ("The rules for determining a match are specified in General rules 3a
/// to 3g of the USE statement"), and the code generator wrote the tier tests twice: <c>EcEmitter.EmitDispatchSelector</c> for
/// USE Format 3 and <c>ControlFlowEmitter.ClassifyOperand</c> for the WHEN operands, the second kept equal to the first by a
/// comment, and the two already disagreed about a name the catalog does not resolve. The tier is now the bound operand's
/// (<see cref="BoundExceptionSelector.Tier"/>, one type for both operand lists) and the C# test is
/// <c>ExceptionSelectionEmitter</c>'s alone. These pins keep a third rendering from appearing.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ExceptionSelectionTierDriftTests
{
    private static string CodeGenDir =>
        Path.GetDirectoryName(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "EcEmitter.cs"))!;

    /// <summary>The per-tier run-time predicates (the level-2 family test and the EC-I-O test over the raised name) are
    /// written into emitted text by the one renderer alone.</summary>
    [Fact]
    public void TierTests_AreRenderedOnlyByExceptionSelectionEmitter()
    {
        var tierTest = new Regex(@"ExceptionCatalog\.UnderLevel2\(|ExceptionCatalog\.IsIoName\(__ec\)");
        var offenders = Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "ExceptionSelectionEmitter.cs")
            .SelectMany(f => File.ReadLines(f).Select((t, i) => (File: Path.GetFileName(f), Line: i + 1, Text: t)))
            .Where(l => !l.Text.TrimStart().StartsWith("//") && tierTest.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}  {l.Text.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "a GR3 tier test is rendered outside ExceptionSelectionEmitter (kb/Work PB2723):" + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The GR3 b) open-mode tier selects on ONE mode expression for both selectors, the USE Format-1 switch and the
    /// WHEN EXCEPTION INPUT | OUTPUT | I-O | EXTEND arms (§2023:14.9.28.4 GR17). The WHEN arms once tested "the attempted OR
    /// the opened mode" by their own expression and so ran the first-written phrase, where the USE switch runs the procedure
    /// of the mode the file is open in (kb/Work PB2723 review R1-1, D-USE-OPEN-MODE-BOTH). Only <c>RuntimeApi</c> (the
    /// renderer of the runtime call) and <c>ExceptionSelectionEmitter</c> name the mode selector.</summary>
    [Fact]
    public void OpenModeSelection_IsRenderedOnlyByExceptionSelectionEmitter()
    {
        var modeSelector = new Regex(@"\bFileUseModeOf\(");
        var offenders = Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) is not ("ExceptionSelectionEmitter.cs" or "RuntimeApi.cs"))
            .SelectMany(f => File.ReadLines(f).Select((t, i) => (File: Path.GetFileName(f), Line: i + 1, Text: t)))
            .Where(l => !l.Text.TrimStart().StartsWith("//") && modeSelector.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}  {l.Text.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "the GR3 b) open-mode selection is rendered outside ExceptionSelectionEmitter (kb/Work PB2723):" + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The retired per-emitter classifiers and the two-view open-mode renderers stay gone (CLAUDE.md rule 4: no
    /// second copy).</summary>
    [Fact]
    public void TheRetiredClassifiers_AreGone()
    {
        var stale = new Regex(@"\bClassifyOperand\(|\bModeOrdinal\(|\bBoundWhenOperand\b|\bFileOpenModeOf\(|\bFileOpenedModeOf\(");
        var offenders = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => stale.IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0, "a retired GR3 classifier survives: " + string.Join(", ", offenders));
    }

    /// <summary>The tier of an unpaired exception-name follows its catalog level (§2023:14.6.13.1.1's three levels), and the
    /// enum order is GR3's a) to g) order, which both selectors sort by.</summary>
    [Theory]
    [InlineData("EC-SIZE-OVERFLOW", ExceptionSelectionTier.Level3)]
    [InlineData("EC-SIZE", ExceptionSelectionTier.Level2)]
    [InlineData("EC-ALL", ExceptionSelectionTier.EcAll)]
    public void UnpairedName_TakesTheTierOfItsLevel(string name, ExceptionSelectionTier expected)
    {
        Assert.True(ExceptionCatalog.TryGet(name, out var info));
        var selector = BoundExceptionSelector.ForName(info, null);
        Assert.Equal(expected, selector.Tier);
        Assert.Equal(name, selector.Ec);
        Assert.Null(selector.File);
    }

    [Fact]
    public void TierOrder_IsGr3Order() =>
        Assert.Equal(
            [ExceptionSelectionTier.FileName, ExceptionSelectionTier.OpenMode, ExceptionSelectionTier.FileLevel3,
             ExceptionSelectionTier.FileLevel2, ExceptionSelectionTier.Level3, ExceptionSelectionTier.Level2,
             ExceptionSelectionTier.EcAll],
            Enum.GetValues<ExceptionSelectionTier>().OrderBy(t => (int)t));
}
