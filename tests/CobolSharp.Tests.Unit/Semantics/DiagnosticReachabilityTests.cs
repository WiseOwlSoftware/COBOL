// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Tests that key diagnostic descriptors are reachable via their validation logic.
/// Uses direct API calls rather than full compilation to isolate the validation layer.
/// </summary>
public class DiagnosticReachabilityTests
{
    // ── Helpers ──

    private static DataSymbol MakeElem(string name, int level, string pic,
        UsageKind usage = UsageKind.Display)
    {
        var sym = new DataSymbol(name, name, level, pic, usage, null, null, 1);
        sym.HasExplicitUsage = usage != UsageKind.Display;
        var diagBag = new DiagnosticBag();
        sym.ResolvedType = PicUsageResolver.ResolveForDataItem(name, pic, usage, diagBag, 1, false, PicEnvironment.Default);
        return sym;
    }

    private static DataSymbol MakeGroup(string name, int level, params DataSymbol[] children)
    {
        var sym = new DataSymbol(name, name, level, null, UsageKind.Display, null, null, 1);
        foreach (var c in children) sym.AddChild(c);
        return sym;
    }

    private static BoundIdentifierExpression MakeId(CobolCategory cat, string name = "X")
    {
        var pic = cat == CobolCategory.Numeric ? "9(5)" : "X(5)";
        var sym = MakeElem(name, 77, pic);
        return new BoundIdentifierExpression(sym, cat);
    }

    // ── CBL0801: OCCURS on 01/77 ──
    [Fact]
    public void CBL0801_Occurs_on_level01()
    {
        var item = MakeGroup("REC", 1);
        item.Occurs = new OccursInfo(5, 5);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0801");
    }

    // ── CBL0802: BLANK WHEN ZERO on non-numeric-DISPLAY ──
    [Fact]
    public void CBL0802_BlankWhenZero_on_alpha()
    {
        var item = MakeElem("A", 77, "X(5)");
        // Force BlankWhenZero by recreating with the flag
        var diagBag = new DiagnosticBag();
        item.ResolvedType = PicUsageResolver.ResolveForDataItem("A", "X(5)", UsageKind.Display, diagBag, 1, true, PicEnvironment.Default);
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0802");
    }

    // ── CBL0803: JUSTIFIED on numeric ──
    [Fact]
    public void CBL0803_Justified_on_numeric()
    {
        var item = MakeElem("N", 77, "9(5)");
        item.IsJustifiedRight = true;
        var (diags, _) = RunClassifier(item);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL0803");
    }

    // ── CBL2601: Non-numeric arithmetic operand ──
    [Fact]
    public void CBL2601_alpha_operand()
    {
        var alpha = MakeId(CobolCategory.Alphanumeric);
        var num = MakeId(CobolCategory.Numeric, "T");
        var stmt = new BoundArithmeticStatement(ArithmeticKind.Add,
            new BoundExpression[] { alpha }, null,
            new[] { new BoundArithmeticTarget(num, false) });
        var diags = new DiagnosticBag();
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags,
            new Compiler.Common.SourceLocation("<t>", 0, 1, 0), new Compiler.Common.TextSpan(0, 0));
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL2601");
    }

    // ── CBL2602: Non-numeric arithmetic target ──
    [Fact]
    public void CBL2602_alpha_target()
    {
        var num = MakeId(CobolCategory.Numeric);
        var alpha = MakeId(CobolCategory.Alphanumeric, "T");
        var stmt = new BoundArithmeticStatement(ArithmeticKind.Add,
            new BoundExpression[] { num }, null,
            new[] { new BoundArithmeticTarget(alpha, false) });
        var diags = new DiagnosticBag();
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags,
            new Compiler.Common.SourceLocation("<t>", 0, 1, 0), new Compiler.Common.TextSpan(0, 0));
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL2602");
    }

    // ── CBL1103: OCCURS key not subordinate ──
    [Fact]
    public void CBL1103_key_not_subordinate()
    {
        var outsider = MakeElem("OUT", 77, "9(5)");
        var child = MakeElem("CHILD", 5, "X(5)");
        child.Occurs = new OccursInfo(3, 3, ascendingKeys: new[] { "OUT" });
        var group = MakeGroup("REC", 1, child);
        var (diags, _) = RunClassifier(group, child, outsider);
        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL1103");
    }

    // ── DiagnosticDescriptor registry completeness ──
    [Fact]
    public void All_descriptor_codes_are_unique()
    {
        var fields = typeof(DiagnosticDescriptors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
            .ToList();

        var codes = fields.Select(d => d.Code).ToList();
        var duplicates = codes.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Descriptor_count_is_at_least_90()
    {
        var count = typeof(DiagnosticDescriptors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Count(f => f.FieldType == typeof(DiagnosticDescriptor));
        Assert.True(count >= 90, $"Expected >= 90 descriptors, found {count}");
    }

    // ── Shared classifier runner ──
    private static (DiagnosticBag diags, SemanticModel model) RunClassifier(params DataSymbol[] items)
    {
        var prog = new ProgramSymbol("TEST", 1);
        var symbols = new SymbolTable("TEST", 1);
        var diagnostics = new DiagnosticBag();
        var model = new SemanticModel(prog, symbols, diagnostics);
        foreach (var item in items)
        {
            if (item.LevelNumber is 1 or 77) model.AddDataRecord(item);
            symbols.Program.DataDivisionScope.TryDeclare(item, out _);
        }
        model.SetDataItemsInOrder(items);
        StorageLayoutComputer.ComputeLayout(model);
        var classifierDiags = new DiagnosticBag();
        DataItemClassifier.Validate(model, classifierDiags);
        return (classifierDiags, model);
    }
}
