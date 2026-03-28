// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class DataItemClassifierTests
{
    // ── Helpers ──

    private static DataSymbol MakeElementary(string name, int level, string pic,
        UsageKind usage = UsageKind.Display, bool blankWhenZero = false,
        bool justifiedRight = false)
    {
        var sym = new DataSymbol(name, name, level, pic, usage, null, null, 1);
        sym.HasExplicitUsage = usage != UsageKind.Display;
        sym.IsJustifiedRight = justifiedRight;
        var picEnv = PicEnvironment.Default;
        var diagBag = new DiagnosticBag();
        sym.ResolvedType = PicUsageResolver.ResolveForDataItem(name, pic, usage, diagBag, 1, blankWhenZero, picEnv);
        return sym;
    }

    private static DataSymbol MakeGroup(string name, int level, params DataSymbol[] children)
    {
        var sym = new DataSymbol(name, name, level, null, UsageKind.Display, null, null, 1);
        foreach (var child in children)
            sym.AddChild(child);
        return sym;
    }

    private static (DiagnosticBag diags, SemanticModel model) RunClassifier(params DataSymbol[] items)
    {
        var prog = new ProgramSymbol("TEST", 1);
        var symbols = new SymbolTable("TEST", 1);
        var diagnostics = new DiagnosticBag();
        var model = new SemanticModel(prog, symbols, diagnostics);

        foreach (var item in items)
        {
            if (item.LevelNumber is 1 or 77)
                model.AddDataRecord(item);
            symbols.Program.DataDivisionScope.TryDeclare(item, out _);
        }
        model.SetDataItemsInOrder(items);
        StorageLayoutComputer.ComputeLayout(model);

        var classifierDiags = new DiagnosticBag();
        DataItemClassifier.Validate(model, classifierDiags);
        return (classifierDiags, model);
    }

    // ── OCCURS on 01/77 ──

    [Fact]
    public void Occurs_on_level_01_reports_CBL0801()
    {
        var item = MakeGroup("RECORD1", 1);
        item.Occurs = new OccursInfo(5, 5);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0801");
    }

    [Fact]
    public void Occurs_on_level_77_reports_CBL0801()
    {
        var item = MakeElementary("FIELD1", 77, "9(5)");
        item.Occurs = new OccursInfo(3, 3);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0801");
    }

    [Fact]
    public void Occurs_on_level_05_is_valid()
    {
        var child = MakeElementary("FIELD1", 5, "9(5)");
        child.Occurs = new OccursInfo(3, 3);
        var group = MakeGroup("RECORD1", 1, child);
        var (diags, _) = RunClassifier(group, child);
        Assert.DoesNotContain(diags.Diagnostics, d => d.Code == "CBL0801");
    }

    // ── BLANK WHEN ZERO ──

    [Fact]
    public void BlankWhenZero_on_numeric_display_is_valid()
    {
        var item = MakeElementary("NUM1", 77, "9(5)", blankWhenZero: true);
        var (diags, _) = RunClassifier(item);
        Assert.DoesNotContain(diags.Diagnostics, d => d.Code == "CBL0802");
    }

    [Fact]
    public void BlankWhenZero_on_alphanumeric_reports_CBL0802()
    {
        var item = MakeElementary("ALPHA1", 77, "X(5)", blankWhenZero: true);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0802");
    }

    [Fact]
    public void BlankWhenZero_on_comp_reports_CBL0802()
    {
        var item = MakeElementary("COMP1", 77, "9(5)", UsageKind.Comp, blankWhenZero: true);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0802");
    }

    // ── JUSTIFIED ──

    [Fact]
    public void Justified_on_alphanumeric_elementary_is_valid()
    {
        var item = MakeElementary("ALPHA1", 77, "X(10)", justifiedRight: true);
        var (diags, _) = RunClassifier(item);
        Assert.DoesNotContain(diags.Diagnostics, d => d.Code == "CBL0803");
    }

    [Fact]
    public void Justified_on_numeric_reports_CBL0803()
    {
        var item = MakeElementary("NUM1", 77, "9(5)", justifiedRight: true);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0803");
    }

    [Fact]
    public void Justified_on_group_reports_CBL0803()
    {
        var child = MakeElementary("CHILD1", 5, "X(5)");
        var group = MakeGroup("GRP1", 1, child);
        group.IsJustifiedRight = true;
        var (diags, _) = RunClassifier(group, child);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0803");
    }

    // ── OCCURS KEY validation ──

    [Fact]
    public void Occurs_key_not_subordinate_reports_CBL1103()
    {
        var outsider = MakeElementary("OUTSIDER", 77, "9(5)");
        var child = MakeElementary("CHILD1", 5, "X(5)");
        child.Occurs = new OccursInfo(3, 3, ascendingKeys: new[] { "OUTSIDER" });
        var group = MakeGroup("RECORD1", 1, child);
        var (diags, _) = RunClassifier(group, child, outsider);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL1103");
    }

    [Fact]
    public void Occurs_key_that_is_group_is_accepted()
    {
        // COBOL-85 allows group items as OCCURS keys — comparison uses
        // the group's alphanumeric representation.
        var innerChild = MakeElementary("INNER", 10, "X(5)");
        var subGroup = MakeGroup("SUB", 10, innerChild);
        var tableChild = MakeElementary("VAL", 10, "9(3)");
        var table = MakeGroup("TABLE1", 5, subGroup, tableChild);
        table.Occurs = new OccursInfo(5, 5, ascendingKeys: new[] { "SUB" });
        var root = MakeGroup("RECORD1", 1, table);
        var (diags, _) = RunClassifier(root, table, subGroup, innerChild, tableChild);
        Assert.DoesNotContain(diags.Diagnostics, d => d.Code == "CBL1104");
    }

    // ── No OCCURS → no diagnostics ──

    [Fact]
    public void No_occurs_no_diagnostics()
    {
        var item = MakeElementary("SIMPLE", 77, "X(10)");
        var (diags, _) = RunClassifier(item);
        Assert.Empty(diags.Diagnostics);
    }

    // ── PIC A classification ──

    [Fact]
    public void PicA_classified_as_Alphabetic()
    {
        var item = MakeElementary("ALPHA1", 77, "A(10)");
        Assert.Equal(CobolCategory.Alphabetic, item.ResolvedType!.Category);
    }

    [Fact]
    public void PicX_classified_as_Alphanumeric()
    {
        var item = MakeElementary("ALPHANUM1", 77, "X(10)");
        Assert.Equal(CobolCategory.Alphanumeric, item.ResolvedType!.Category);
    }

    [Fact]
    public void PicAX_classified_as_Alphanumeric()
    {
        // Mixed A and X → Alphanumeric
        var item = MakeElementary("MIXED1", 77, "AX");
        Assert.Equal(CobolCategory.Alphanumeric, item.ResolvedType!.Category);
    }

    // ── BLANK WHEN ZERO with JUSTIFIED (Check 4) ──

    [Fact]
    public void BlankWhenZero_with_Justified_reports_CBL0804()
    {
        var item = MakeElementary("NUM1", 77, "9(5)", blankWhenZero: true, justifiedRight: true);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0804");
    }

    // ── OCCURS on level-66 (Check 5) ──

    [Fact]
    public void Occurs_on_level_66_reports_CBL0805()
    {
        var item = MakeElementary("RENAME1", 66, "X(10)");
        item.Occurs = new OccursInfo(5, 5);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0805");
    }

    // ── VALUE on REDEFINES (Check 6) ──

    [Fact]
    public void Value_on_Redefines_reports_CBL0806()
    {
        var original = MakeElementary("ORIG", 5, "X(10)");
        var redef = MakeElementary("REDEF1", 5, "9(10)");
        redef.Redefines = original;
        redef.InitialValue = "0";
        var group = MakeGroup("GRP1", 1, original, redef);
        var (diags, _) = RunClassifier(group, original, redef);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0806");
    }

    // ── VALUE on OCCURS subordinate (Check 7) ──

    [Fact]
    public void Value_on_Occurs_subordinate_reports_CBL0807()
    {
        var child = MakeElementary("CHILD1", 10, "X(5)");
        child.InitialValue = "HELLO";
        var table = MakeGroup("TABLE1", 5, child);
        table.Occurs = new OccursInfo(5, 5);
        var root = MakeGroup("ROOT1", 1, table);
        var (diags, _) = RunClassifier(root, table, child);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0807");
    }
}
