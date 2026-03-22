// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class ArithmeticDiagnosticTests
{
    private static BoundIdentifierExpression MakeId(string name, CobolCategory category)
    {
        var sym = new DataSymbol(name, name, 77, category == CobolCategory.Numeric ? "9(5)" : "X(5)",
            UsageKind.Display, null, null, 1);
        var diagBag = new DiagnosticBag();
        sym.ResolvedType = PicUsageResolver.ResolveForDataItem(name, sym.PicString!, sym.Usage,
            diagBag, 1, false, PicEnvironment.Default);
        return new BoundIdentifierExpression(sym, category);
    }

    [Fact]
    public void Alphanumeric_operand_reports_CBL2601()
    {
        var alphaOp = MakeId("ALPHA1", CobolCategory.Alphanumeric);
        var numTarget = MakeId("NUM1", CobolCategory.Numeric);
        var stmt = new BoundArithmeticStatement(
            ArithmeticKind.Add,
            new BoundExpression[] { alphaOp },
            null,
            new[] { new BoundArithmeticTarget(numTarget, false) });

        var diags = new DiagnosticBag();
        var loc = new Compiler.Common.SourceLocation("<source>", 0, 1, 0);
        var span = new Compiler.Common.TextSpan(0, 0);
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags, loc, span);

        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL2601");
    }

    [Fact]
    public void Alphanumeric_result_reports_CBL2602()
    {
        var numOp = MakeId("NUM1", CobolCategory.Numeric);
        var alphaTarget = MakeId("ALPHA1", CobolCategory.Alphanumeric);
        var stmt = new BoundArithmeticStatement(
            ArithmeticKind.Add,
            new BoundExpression[] { numOp },
            null,
            new[] { new BoundArithmeticTarget(alphaTarget, false) });

        var diags = new DiagnosticBag();
        var loc = new Compiler.Common.SourceLocation("<source>", 0, 1, 0);
        var span = new Compiler.Common.TextSpan(0, 0);
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags, loc, span);

        Assert.Contains(diags.Diagnostics, d => d.Code == "CBL2602");
    }

    [Fact]
    public void Valid_numeric_arithmetic_no_diagnostics()
    {
        var numOp = MakeId("A", CobolCategory.Numeric);
        var numTarget = MakeId("B", CobolCategory.Numeric);
        var stmt = new BoundArithmeticStatement(
            ArithmeticKind.Add,
            new BoundExpression[] { numOp },
            null,
            new[] { new BoundArithmeticTarget(numTarget, false) });

        var diags = new DiagnosticBag();
        var loc = new Compiler.Common.SourceLocation("<source>", 0, 1, 0);
        var span = new Compiler.Common.TextSpan(0, 0);
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags, loc, span);

        Assert.Empty(diags.Diagnostics);
    }

    [Fact]
    public void Numeric_edited_target_is_valid_result()
    {
        var numOp = MakeId("A", CobolCategory.Numeric);
        // Create numeric-edited target
        var editSym = new DataSymbol("EDIT", "EDIT", 77, "Z(5).99", UsageKind.Display, null, null, 1);
        var diagBag = new DiagnosticBag();
        editSym.ResolvedType = PicUsageResolver.ResolveForDataItem("EDIT", "Z(5).99",
            UsageKind.Display, diagBag, 1, false, PicEnvironment.Default);
        var editTarget = new BoundIdentifierExpression(editSym, CobolCategory.NumericEdited);

        var stmt = new BoundArithmeticStatement(
            ArithmeticKind.Add,
            new BoundExpression[] { numOp },
            null,
            new[] { new BoundArithmeticTarget(editTarget, false) });

        var diags = new DiagnosticBag();
        var loc = new Compiler.Common.SourceLocation("<source>", 0, 1, 0);
        var span = new Compiler.Common.TextSpan(0, 0);
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, diags, loc, span);

        Assert.Empty(diags.Diagnostics);
    }
}
