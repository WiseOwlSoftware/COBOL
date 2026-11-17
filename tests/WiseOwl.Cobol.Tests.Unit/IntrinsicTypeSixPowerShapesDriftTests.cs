// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SHAPE OF THE ISO §2023:15.3 TYPE 6 EXPRESSION HAS A RECORDED VERDICT OF THE FAIL-CLOSED SCREEN (kb/Work PB5345, PB5886).
/// </summary>
/// <remarks>
/// <para>§2023:15.3 type 6 (cite.py --check): "An arithmetic expression that will always result in an integer value or an integer data
/// item shall be specified"; §1989:A.2.2 AR4 the same at COBOL-85. Since kb/Work PB5886 the screen admits only what it SHOWS to be an
/// integer for every valuation (<c>IntrinsicArgumentRules.NotAlwaysIntegral</c>) and refuses everything else.
/// <see cref="EveryBoundExpressionShape_HasAVerdict"/> forces a decision for a NEW <c>BoundExpr</c> node, and
/// <see cref="Shapes_AreRefusedOrAdmittedAsRecorded"/> compiles each recorded shape and reads the screen's answer.</para>
/// </remarks>
public sealed class IntrinsicTypeSixPowerShapesDriftTests : WiseOwlCobolTestBase
{
    /// <summary>The verdict of every <c>BoundExpr</c> node type at a type 6 position, and why (kb/Work PB5891: the classifier builds the
    /// expression's value as a polynomial; docs/INTRINSICS_DESIGN.md, the type-6 intake). Integer atom: a term that takes integer values
    /// only. Real atom: a term that must cancel. Operation: a polynomial operation whose intermediate must be exact. Refused: not shown.</summary>
    private static readonly Dictionary<string, string> NodeVerdicts = new()
    {
        ["BoundNumLiteral"] = "Constant: a literal fold at its exact value, trusted only when the arithmetic stores its quotients exactly",
        ["BoundNumRef"] = "Integer atom times 10^-scale for a fixed-point item (the same place is one atom); Real atom for a floating-point item",
        ["BoundBinary"] = "Operation: + - * of polynomials, a quotient by a nonzero literal is the product by its reciprocal; every other quotient Refused",
        ["BoundNegate"] = "Operation: the operand's polynomial negated, exact whatever it negates",
        ["BoundPower"] = "Operation: the n-th power for a non-negative integer literal n; Integer atom for an integer-valued base to an unsigned integer item or 1 to an integer; else Refused",
        ["BoundIndexRef"] = "Integer atom: an index-name's value is an occurrence number",
        ["BoundLinageCounterRef"] = "Integer atom: a counter register is an unsigned integer data item (AlgebraicRanges.IsCounterRegister)",
        ["BoundReportCounterRef"] = "Integer atom: a counter register is an unsigned integer data item (AlgebraicRanges.IsCounterRegister)",
        ["BoundReportVaryingRef"] = "Integer atom: a report VARYING counter is a scale-0 integer",
        ["BoundIntrinsicCall"] = "Integer atom for an INTEGER-type function; Real atom for every other (one atom only when its arguments fix its value)",
        ["BoundOdoExtent"] = "Integer atom: the run-time extent of a variable-length group is a scale-0 integer",
        ["BoundExprError"] = "Not refused again: the operand was refused where it was bound",
        ["BoundUdfEvaluatedExpr"] = "The verdict of the expression it carries",
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
            "a BoundExpr node type has no recorded verdict at an ISO §2023:15.3 type 6 position — decide whether the fail-closed screen "
            + "shows it integral or refuses it, and record it in NodeVerdicts (kb/Work PB5345, PB5886):\n  " + string.Join("\n  ", missing));
        Assert.True(stale.Count == 0, "NodeVerdicts names a node type that no longer exists:\n  " + string.Join("\n  ", stale));
    }

    /// <summary>(expression over N PIC 9, M PIC S9, K PIC 9V9, U PIC 9, A PIC X, F COMP-2, T PIC 9V9 OCCURS 2; refused; why). The data names are fixed by
    /// <see cref="Shapes_AreRefusedOrAdmittedAsRecorded"/>. Every refused row with "PB5886" is an integer argument the screen
    /// before kb/Work PB5886 admitted.</summary>
    private static readonly (string Expression, bool Refused, string Why)[] Shapes =
    [
        // Powers.
        ("N ** -1", true, "a negative exponent: N = 3 gives 1/3"),
        ("N ** 0.5", true, "a fraction exponent"),
        ("N ** M", true, "a signed exponent item: M = -1"),
        ("N ** K", true, "an exponent item with fraction digits"),
        ("N ** (- U)", true, "a negated unsigned exponent: U = 1"),
        ("2 ** M", true, "a signed exponent item under a literal base"),
        ("2 ** 0.5", true, "a fraction literal exponent"),
        ("2 ** -65", true, "a negative exponent beyond the fold"),
        ("4 ** 0.5", true, "PB5886: a fraction exponent is no integer power, although this one is 2"),
        ("(N + 1) ** -1", true, "PB5886: a negative exponent"),
        ("(N ** -1) * 2520", true, "PB5886: the power is no integer term"),
        ("1024 ** K", true, "PB5886: an exponent item with fraction digits"),
        ("1 ** M", false, "PB5886 round 2: the literal 1 to any integer exponent is 1"),
        ("N ** -1 - N ** -1", true, "PB5886: neither power is an integer term"),
        ("K + K ** -1", true, "PB5886: K has digits right of the decimal point"),
        ("N ** U", false, "an unsigned integer exponent item"),
        ("N ** - (- U)", false, "an unsigned integer exponent item under two negations"),
        ("2 ** U", false, "a literal base to an unsigned integer item"),
        ("N ** 2", false, "a non-negative integer literal exponent"),
        ("(K * 10) ** 2", false, "a base shown integral by its literal factor"),
        // Quotients (PB5886: an integer term only as a literal fold with an integral value).
        ("N / 2", true, "N = 3 gives 1.5"),
        ("N / 2 * 2", false, "PB5886 round 2: a quotient by 2 is the product by 0.5, so N's net factor is 1"),
        ("N / 3 * 3", true, "the reciprocal of 3 does not terminate, and the arithmetic truncates N / 3"),
        ("(N / 2) / 0.5", false, "the product by 0.5 and then by 2: N's net factor is 1"),
        ("N / N", true, "a quotient by an item: N = 0 is a size error, not an integer"),
        ("362880 / N", true, "PB5886: a quotient over an item"),
        ("K / 0.1", false, "PB5886 round 2: a quotient by 0.1 is the product by 10, which cancels K's fraction digit"),
        ("FUNCTION ORD (A) / 2", true, "PB5886: an integer function under the net factor 0.5, 33.5 for A = 'B'"),
        ("N - FUNCTION MAX (N)", false, "PB5886 round 2: MAX of integer arguments is an integer function (its type table)"),
        ("FUNCTION SUM (N U) / 2 * 2", false, "SUM of integer arguments is an integer function, under the net factor 1"),
        ("FUNCTION ABS (M) + 1", false, "ABS of an integer argument is an integer function"),
        // PB5886 round 2, R2-1: a function's type takes its argument's type from the SAME classifier as the screen.
        ("FUNCTION MAX (N / 2 * 2)", false, "N / 2 * 2 is an integer argument, so MAX of it is an integer function"),
        ("FUNCTION MAX (K * 10) - 12", false, "K * 10 is an integer argument, so MAX of it is an integer function"),
        ("FUNCTION MAX (FUNCTION MIN (N / 2 * 2), 1)", false, "two functions deep: MIN of an integer argument, then MAX of integers"),
        ("FUNCTION MAX (N / 2)", true, "N / 2 is no integer argument, so MAX of it is a numeric function"),
        ("FUNCTION INTEGER (N) / FUNCTION INTEGER (U)", true, "PB5886: a quotient of two integer functions"),
        ("N * N * N / 2", true, "PB5886: a quotient over a product of items"),
        ("1 / 3 * 3", true, "a literal quotient the arithmetic does not keep exactly (PB4758)"),
        ("4 / 2 + N", false, "a literal quotient that is the integer 2"),
        ("1.5 + 0.5 + N", false, "literal terms that sum to an integer"),
        // Function terms (PB5886: judged by the function's type).
        ("FUNCTION ORD (A) + 1", false, "an integer function is an integer term"),
        ("FUNCTION ORD (A) * FUNCTION MOD (N 3)", false, "a product of integer functions"),
        ("FUNCTION ORD (A) * 0.5 * 2", false, "an integer function under the net factor 1"),
        ("FUNCTION MOD (N 3) * 0.5", true, "PB5886: an integer function under the net factor 0.5"),
        ("FUNCTION SQRT (N) - FUNCTION SQRT (N) + 5", false, "PB5891: the same pure term cancels, so the value is the constant 5"),
        ("0 * FUNCTION SQRT (N) + 5", false, "PB5891: a real term under the coefficient 0 is the constant 0"),
        ("FUNCTION SQRT (N) + 1 - FUNCTION SQRT (N)", true, "PB5891: the intermediate SQRT (N) + 1 holds a real term and is not shown exact"),
        ("FUNCTION RANDOM - FUNCTION RANDOM + 1", true, "PB5891: RANDOM's value is not fixed by its arguments, so the two calls are two atoms"),
        ("K - FUNCTION MAX (K)", true, "PB5889's residue: MAX (K) and K are two atoms, equal only by MAX's meaning"),
        ("FUNCTION MAX (K) - K", true, "PB5886: MAX of a scaled item is a numeric function"),
        ("FUNCTION RANDOM (1) * 10", true, "PB5886: a numeric function"),
        // Items under literal factors (a product by a literal is exact in decimal arithmetic).
        ("K * 10", false, "the factor 10 cancels K's one fraction digit"),
        ("K - K + 1", false, "K nets to zero"),
        ("N * 0.5 + N * 0.5", false, "N's net factor is 1"),
        ("N * 0.5", true, "N's net factor 0.5"),
        ("K + 1", true, "K has digits right of the decimal point"),
        ("N * K", true, "a product of two items, K not shown integral"),
        ("N * U * 2", false, "a product of integer items"),
        // kb/Work PB5891: the value is a polynomial, integer-valued exactly when it is an integer on the grid of its degrees.
        ("N * (N + 1) / 2", false, "PB5891: a product of two consecutive integers is even"),
        ("N * (N + 1) * (N + 2) / 6", false, "PB5891: a product of three consecutive integers is a multiple of 6, so the quotient is exact"),
        ("N * (N - 1) / 2 + 65", false, "PB5891: N (N - 1) / 2 is an integer"),
        ("N ** 2 * (N + 1) ** 2 / 4", false, "PB5891: the square of N (N + 1) / 2"),
        ("N * (N + 1) / 3", true, "PB5891: 2/3 at N = 1"),
        ("(N ** 13 - N) / 13", true, "PB5891: an integer by Fermat, but degree 13 passes the grid cap and the quotient is not shown exact"),
        ("K * K * 100", false, "PB5891: K is a tenth of an integer atom, so K * K * 100 is that atom squared"),
        ("K * K * 10", true, "PB5891: 0.1 at K = 0.1"),
        ("T (1) - T (2)", true, "PB5891: two subscripts are two places (the item keyed by its description cancelled them)"),
        ("T (1) - T (1) + 1", false, "PB5891: one place is one atom"),
        ("F - F + 1", false, "PB5891: F - F is exactly 0 in binary64"),
        ("F + 1 - F", true, "PB5891: the intermediate F + 1 holds a floating-point term and is not shown exact"),
        ("(F - F + N * 0.1) * 10", true, "PB5891: the intermediate F - F + N * 0.1 is computed in binary64 and is not an integer"),
    ];

    [Fact]
    public void Shapes_AreRefusedOrAdmittedAsRecorded()
    {
        var proc = new StringBuilder();
        for (int i = 0; i < Shapes.Length; i++)
            proc.AppendLine($"    COMPUTE R = FUNCTION MOD ({Shapes[i].Expression}, 7)");
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB5345SHAPES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC 9 VALUE 3.
            01 M PIC S9 VALUE -2.
            01 K PIC 9V9 VALUE 0.5.
            01 U PIC 9 VALUE 2.
            01 A PIC X VALUE "B".
            01 F COMP-2 VALUE 0.5.
            01 TB.
               05 T PIC 9V9 OCCURS 2 VALUE 0.5.
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
        Assert.True(otherErrors.Count == 0, "the shape table's source drew a diagnostic other than COBOL1627:\n  " + string.Join("\n  ", otherErrors));
        var wrong = new List<string>();
        for (int i = 0; i < Shapes.Length; i++)
        {
            bool refused = refusedLines.Contains(firstShapeLine + i);
            if (refused != Shapes[i].Refused)
                wrong.Add($"{Shapes[i].Expression}: {(refused ? "refused" : "admitted")}, recorded as "
                    + $"{(Shapes[i].Refused ? "refused" : "admitted")} ({Shapes[i].Why})");
        }
        Assert.True(wrong.Count == 0,
            "the type 6 screen answered differently from the shape table (kb/Work PB5345, PB5886). Errors:\n  "
            + string.Join("\n  ", compiled.Errors) + "\nWrong:\n  " + string.Join("\n  ", wrong));
    }
}
