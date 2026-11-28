// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// kb/Work PB6141 — <b>THE TIER-A (ALIAS) REDEFINES VIEW RULE IS WRITTEN ONCE.</b> A Tier-A view owns no field and renders its
/// canonical's (§2023:13.18.44.4 GR1: one storage area). The test "a non-canonical member of an Alias class" was spelled inline at
/// thirteen sites across the binder, the model and the emitter, in two guard shapes (one also asked class membership), so a
/// change to the rule had to find every copy. It is now <see cref="DataItem.AliasCanonical"/>,
/// <see cref="DataItem.IsAliasView"/> and, for the canonical's side, <see cref="DataItem.IsPlainMember"/>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AliasViewRuleDriftTests
{
    /// <summary>The files that may name the Alias tier in code: the enum, the classifier that decides it, and the one rule.</summary>
    private static readonly string[] Owners = ["RedefinesModel.cs", "ClassifyRedefinesClassesPass.cs", "DataItem.cs"];

    /// <summary>Structure: no other compiler file spells the tier test.</summary>
    [Fact]
    public void OnlyTheOwners_NameTheAliasTier()
    {
        var tierTest = new Regex(@"\bRedefinesTier\.Alias\b|\bTier\s*:\s*RedefinesTier\.Alias\b");
        var offenders = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !Owners.Contains(Path.GetFileName(f)))
            .SelectMany(f => File.ReadLines(f).Select((t, i) => (File: Path.GetFileName(f), Line: i + 1, Code: t.Split("//", 2)[0])))
            .Where(l => tierTest.IsMatch(l.Code))
            .Select(l => $"{l.File}:{l.Line}  {l.Code.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "the Tier-A view rule is spelled outside DataItem.AliasCanonical / IsPlainMember (kb/Work PB6141):" + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The retired private copy stays gone (CLAUDE.md rule 4).</summary>
    [Fact]
    public void TheRetiredCopy_IsGone()
    {
        var offenders = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("CorrPlainMember", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0, "a retired Tier-A predicate survives: " + string.Join(", ", offenders));
    }

    /// <summary>Behaviour: the view forwards to its canonical; the canonical and a standalone item are plain members; a
    /// Tier-B member, canonical or not, is neither a view nor a plain member.</summary>
    [Fact]
    public void TheRule_AnswersEachShape()
    {
        var standalone = Item("S");
        Assert.Null(standalone.AliasCanonical);
        Assert.False(standalone.IsAliasView);
        Assert.True(standalone.IsPlainMember);

        var (aliasCanon, aliasView) = Pair(RedefinesTier.Alias);
        Assert.Same(aliasCanon, aliasView.AliasCanonical);
        Assert.True(aliasView.IsAliasView);
        Assert.False(aliasView.IsPlainMember);
        Assert.Null(aliasCanon.AliasCanonical);
        Assert.True(aliasCanon.IsPlainMember);

        var (stringCanon, stringView) = Pair(RedefinesTier.StringCanonical);
        Assert.Null(stringView.AliasCanonical);
        Assert.False(stringView.IsPlainMember);
        Assert.Null(stringCanon.AliasCanonical);
        Assert.False(stringCanon.IsPlainMember);
    }

    private static (DataItem Canonical, DataItem View) Pair(RedefinesTier tier)
    {
        var canonical = Item("A");
        var view = Item("B");
        var cls = new RedefinesClass { Canonical = canonical };
        cls.Members.Add(canonical);
        cls.Members.Add(view);
        cls.Classify(tier, 4, null);
        canonical.Class = cls;
        view.Class = cls;
        view.IsCanonical = false;
        return (canonical, view);
    }

    private static DataItem Item(string name) => new() { Level = 1, CobolName = name, ClrName = name };
}
