// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A CHILD'S PHYSICAL ADVANCE IS DECIDED ONCE (kb/Work PB3256). <c>RecordLayout</c> is the one offset/width authority, and the
/// bytes a child adds to its parent's running position — a Tier-B class backing at its canonical, nothing at a forwarded Tier-A
/// view or any other redefiner (§2023:13.18.44.4 GR1, "Storage association for the subject of the entry starts at the first bit
/// of the data item referenced by data-name-2"), otherwise one occurrence times its OCCURS count — is
/// <c>RecordLayout.ExactPhysicalAdvance</c>. It was written three times (the width sum and two offset walks), and the width's
/// copy had lost the redefiner arm, so a redefiner outside a class widened the record without moving the offsets after it.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PhysicalAdvanceDriftTests
{
    /// <summary>Behaviour: a redefining child asked before <c>ClassifyRedefinesClassesPass</c> gives it a class (the type is
    /// phase-free, so a binder may ask) adds nothing to its group's width, exactly as it adds nothing to the offsets.</summary>
    [Fact]
    public void UnclassifiedRedefiner_AddsNothing_ToWidthOrOffsets()
    {
        var group = new DataItem { Level = 1, CobolName = "G", ClrName = "G" };
        var a = Leaf("A", "X(4)");
        var b = Leaf("B", "9(6)");
        b.RedefinesTargetName = "A";
        b.SetRedefinition(a, RedefinitionKind.Clause);
        var c = Leaf("C", "X(2)");
        foreach (var child in new[] { a, b, c })
        {
            child.Parent = group;
            group.AddMember(child);
        }

        Assert.Equal(4, RecordLayout.OffsetOf(c));
        Assert.Equal(6, RecordLayout.PhysicalWidth(group));
    }

    /// <summary>Structure: no member of <c>RecordLayout</c> but the one advance asks a CHILD's redefines tier. (<c>AreaWidth</c>
    /// asks a RECORD's own tier, the frame it spans, which is not a child's advance.)</summary>
    [Fact]
    public void OnlyThePhysicalAdvance_AsksAChildsRedefinesTier()
    {
        string path = Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "Binding", "Model", "RecordLayout.cs");
        string text = File.ReadAllText(path);
        int start = text.IndexOf("private static long ExactPhysicalAdvance(DataItem child)", StringComparison.Ordinal);
        Assert.True(start >= 0, "RecordLayout.ExactPhysicalAdvance is the one per-child advance; its signature moved");
        int open = text.IndexOf('{', start);
        int depth = 0, end = open;
        for (; end < text.Length; end++)
        {
            if (text[end] == '{') depth++;
            else if (text[end] == '}' && --depth == 0) break;
        }
        string outside = text[..start] + text[(end + 1)..];
        var offenders = outside.Split('\n')
            .Select(l => l.Split("//", 2)[0])
            .Where(code => Regex.IsMatch(code, @"\b(?!record\b)\w+\.Class\s+is\s*\{\s*Tier:\s*RedefinesTier\."))
            .Select(code => code.Trim())
            .ToList();
        Assert.True(offenders.Count == 0,
            "a child's redefines tier is asked outside RecordLayout.ExactPhysicalAdvance (kb/Work PB3256):\n  "
            + string.Join("\n  ", offenders));
    }

    private static DataItem Leaf(string name, string picture)
    {
        var pic = PictureAnalyzer.Analyze(picture, Usage.Display, new EditionContext(2023), $"data item '{name}'",
            currencies: new Dictionary<char, CurrencyDefinition>(), decimalPointIsComma: false);
        Assert.False(pic.IsRecovery, $"PIC {picture} did not analyze");
        return new DataItem { Level = 5, CobolName = name, ClrName = name, Pic = pic, PictureText = picture };
    }
}
