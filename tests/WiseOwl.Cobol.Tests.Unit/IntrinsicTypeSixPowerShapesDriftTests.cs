// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SHAPE OF THE ISO §2023:15.3 TYPE 6 EXPRESSION THAT <c>IntrinsicArgumentRules.ProvablyIntegral</c> DOES NOT PROVE IS EITHER A
/// WITNESS ARM OF THE SCREEN OR A RECORDED FAIL-OPEN SHAPE (kb/Work PB5345; PB5208's own fix direction asked for this test).
/// </summary>
/// <remarks>
/// <para>§2023:15.3 type 6 (cite.py --check): "An arithmetic expression that will always result in an integer value or an integer data
/// item shall be specified." The screen refuses an argument only on a WITNESS valuation whose value is not an integer; every
/// other shape fails open. Two tests pin the two halves of that: <see cref="EveryBoundExpressionShape_HasAVerdict"/> forces a decision for
/// a NEW <c>BoundExpr</c> node (a shape nobody classified fails open without anyone having chosen it), and
/// <see cref="PowerShapes_AreRefusedOrAdmittedAsRecorded"/> compiles each recorded power shape and reads the screen's answer.</para>
/// <para>A shape in <see cref="PowerShapes"/> marked admitted is a FAIL-OPEN shape with its reason: the screen cannot prove a
/// witness (or a sound witness does not exist), so the program compiles. A refused one has the witness in its reason.</para>
/// </remarks>
public sealed class IntrinsicTypeSixPowerShapesDriftTests : WiseOwlCobolTestBase
{
    /// <summary>The verdict of every <c>BoundExpr</c> node type at a type 6 position, and why. Proven: ProvablyIntegral has an arm.
    /// Witness: the screen refuses some valuations of it. FailOpen: neither; the screen admits it.</summary>
    private static readonly Dictionary<string, string> NodeVerdicts = new()
    {
        ["BoundNumLiteral"] = "Proven when its value is an integer; Witness (a literal constant that is not an integer)",
        ["BoundNumRef"] = "Proven for an item whose description admits only integers; Witness (ItemWitness) for a scaled or floating-point item",
        ["BoundBinary"] = "Proven for + - *; Witness for a root quotient of a bare item by a literal and an additive spine; FailOpen for a product or quotient the walk cannot value",
        ["BoundNegate"] = "Proven over a proven operand; Witness through the additive spine",
        ["BoundPower"] = "Proven for an integral base to a non-negative integer literal; Witness (PowerWitness); the remaining shapes are PowerShapes' admitted rows",
        ["BoundIndexRef"] = "FailOpen: an index-name is an integer by usage, but ProvablyIntegral names no arm, so it is never a witness",
        ["BoundLinageCounterRef"] = "Proven: a counter register is an unsigned integer data item (AlgebraicRanges.IsCounterRegister)",
        ["BoundReportCounterRef"] = "Proven: a counter register is an unsigned integer data item (AlgebraicRanges.IsCounterRegister)",
        ["BoundReportVaryingRef"] = "FailOpen: a report-varying counter has no arm in ProvablyIntegral and no witness",
        ["BoundIntrinsicCall"] = "Proven for an INTEGER-type function (§2023:15.2 type 5 by the function's type); FailOpen for a numeric one: its value is not read",
        ["BoundOdoExtent"] = "FailOpen: the run-time length of a variable-length group is an integer, but ProvablyIntegral names no arm and no witness exists",
        ["BoundExprError"] = "FailOpen: an expression already diagnosed",
        ["BoundUdfEvaluatedExpr"] = "FailOpen: a user-defined function's value is not read by the screen",
    };

    [Fact]
    public void EveryBoundExpressionShape_HasAVerdict()
    {
        var nodes = typeof(BoundExpr).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsSubclassOf(typeof(BoundExpr)))
            .Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var missing = nodes.Where(n => !NodeVerdicts.ContainsKey(n)).ToList();
        var stale = NodeVerdicts.Keys.Where(k => !nodes.Contains(k)).ToList();
        Assert.True(missing.Count == 0,
            "a BoundExpr node type has no recorded verdict at an ISO §2023:15.3 type 6 position — decide whether ProvablyIntegral proves it, "
            + "the screen has a witness for it, or it fails open, and record it in NodeVerdicts (kb/Work PB5345):\n  " + string.Join("\n  ", missing));
        Assert.True(stale.Count == 0, "NodeVerdicts names a node type that no longer exists:\n  " + string.Join("\n  ", stale));
    }

    /// <summary>(expression over N PIC 9, M PIC S9, K PIC 9V9, U PIC 9; refused; why). The data names are fixed by <see cref="PowerShapes_AreRefusedOrAdmittedAsRecorded"/>.</summary>
    private static readonly (string Expression, bool Refused, string Why)[] PowerShapes =
    [
        ("N ** -1", true, "a bare item to a negative literal: N = 3 gives 1/3"),
        ("N ** 0.5", true, "a bare item to a fraction literal: N = 3 gives an irrational"),
        ("N ** M", true, "a different signed item as the exponent: M = -1"),
        ("N ** K", true, "a different item with fraction digits as the exponent: K = 0.1"),
        ("N ** (- U)", true, "the negation of an unsigned item as the exponent: U = 1"),
        ("2 ** M", true, "a literal base 2 or more, a signed exponent: M = -1 gives 1/2"),
        ("2 ** (- U)", true, "a literal base 2 or more, a negated exponent: U = 1 gives 1/2"),
        ("2 ** K", true, "a literal base that is no perfect 10th power, a fraction exponent: K = 0.1 gives 2 ** 0.1, between 1 and 2"),
        ("2 ** 0.5", true, "a literal base to a fraction literal: the fold values only an integer exponent, so the power arm reads it (R1-1): 2 ** 0.5 is irrational"),
        ("3 ** 0.5 + 1", true, "a literal power to a fraction literal plus an integral constant"),
        ("2 ** -0.5", true, "a literal base to a negative fraction literal: between 0 and 1"),
        ("2 ** -65", true, "a literal base to a negative integer beyond MaxFoldedExponent, which the fold leaves unvalued: 1 / 2^65"),
        ("4 ** 0.5", false, "a literal base that is a perfect square to 0.5 is 2, an integer"),
        ("(-2) ** 0.5", false, "FAIL OPEN: a negative base at a fraction has no real value"),
        ("M ** M", true, "one item as base and exponent: M = -2 gives 1/4"),
        ("K ** K", true, "one item with fraction digits as base and exponent: K = 0.1 gives a value between 0 and 1"),
        ("N ** (- N)", true, "one unsigned item as base and negated exponent: N = 2 gives 1/4"),
        ("- (N ** -1)", true, "a power under a negation is the negation of its witness"),
        ("(N ** -1) + 1", true, "a power plus an integral constant"),
        ("1 - N ** M - N", true, "a power among integral terms"),
        ("(N + 1) ** -1", false, "FAIL OPEN: an expression base - (N - N + 1) ** -1 is 1, so no valuation is a witness in general"),
        ("(N ** -1) * 2520", false, "FAIL OPEN: a power under a product - 2520 is divisible by every 1-digit N, so the product is an integer"),
        ("1024 ** K", false, "FAIL OPEN: 1024 is the 10th power of 2, so 1024 ** K is an integer for every K of one fraction digit"),
        ("1 ** M", false, "FAIL OPEN: a literal base below 2 in magnitude - 1 ** M is always 1"),
        ("N ** -1 - N ** -1", false, "FAIL OPEN: two power terms may cancel"),
        ("N ** -1 + 0.5", false, "FAIL OPEN: a non-integral constant may complete the power's value to an integer (N = 2)"),
        ("K + K ** -1", false, "FAIL OPEN: the same scaled item is another term of the sum"),
        ("N ** U", false, "an unsigned integer exponent is an integer power"),
        ("2 ** U", false, "a literal base to an unsigned integer item is an integer power"),
        ("N ** 2", false, "a non-negative integer literal exponent is an integer power"),
    ];

    [Fact]
    public void PowerShapes_AreRefusedOrAdmittedAsRecorded()
    {
        var proc = new StringBuilder();
        for (int i = 0; i < PowerShapes.Length; i++)
            proc.AppendLine($"    COMPUTE R = FUNCTION MOD ({PowerShapes[i].Expression}, 7)");
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB5345SHAPES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC 9 VALUE 3.
            01 M PIC S9 VALUE -2.
            01 K PIC 9V9 VALUE 0.5.
            01 U PIC 9 VALUE 2.
            01 R PIC S9(4).
            PROCEDURE DIVISION.
            {proc}    STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb5345shapes.cob");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, Path.Combine(TempDir, "pb5345shapes.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free));

        string[] lines = source.Split('\n');
        int firstShapeLine = Array.FindIndex(lines, l => l.Contains("COMPUTE R", StringComparison.Ordinal)) + 1;
        var refusedLines = compiled.Errors
            .Select(e => Regex.Match(e, @"\((\d+),\d+\): error COBOL1627"))
            .Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();
        // R1-3: an admitted row counts only when its statement compiled without any other diagnostic (a parse error would admit it falsely).
        var otherErrors = compiled.Errors.Where(e => !e.Contains("error COBOL1627", StringComparison.Ordinal)).ToList();
        Assert.True(otherErrors.Count == 0, "the power-shape table's source drew a diagnostic other than COBOL1627:\n  " + string.Join("\n  ", otherErrors));
        var wrong = new List<string>();
        for (int i = 0; i < PowerShapes.Length; i++)
        {
            bool refused = refusedLines.Contains(firstShapeLine + i);
            if (refused != PowerShapes[i].Refused)
                wrong.Add($"{PowerShapes[i].Expression}: {(refused ? "refused" : "admitted")}, recorded as "
                    + $"{(PowerShapes[i].Refused ? "refused" : "admitted")} ({PowerShapes[i].Why})");
        }
        Assert.True(wrong.Count == 0,
            "the type 6 power screen answered differently from the shape table (kb/Work PB5345). Errors:\n  "
            + string.Join("\n  ", compiled.Errors) + "\nWrong:\n  " + string.Join("\n  ", wrong));
    }
}
