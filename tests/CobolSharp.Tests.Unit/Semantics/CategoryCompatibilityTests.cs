using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class CategoryCompatibilityTests
{
    private static readonly CobolCategory[] AllCategories =
    {
        CobolCategory.Numeric,
        CobolCategory.NumericEdited,
        CobolCategory.Alphanumeric,
        CobolCategory.AlphanumericEdited,
        CobolCategory.National,
        CobolCategory.NationalEdited,
    };

    // ══════════════════════════════════════
    // MOVE matrix
    // ══════════════════════════════════════

    [Fact]
    public void Numeric_can_move_to_any_category()
    {
        foreach (var dst in AllCategories)
            Assert.True(CategoryCompatibility.IsMoveLegal(CobolCategory.Numeric, dst),
                $"Numeric → {dst} should be legal");
    }

    [Theory]
    [InlineData(CobolCategory.NumericEdited)]
    [InlineData(CobolCategory.Alphanumeric)]
    [InlineData(CobolCategory.AlphanumericEdited)]
    [InlineData(CobolCategory.National)]
    [InlineData(CobolCategory.NationalEdited)]
    public void Non_numeric_cannot_move_to_Numeric(CobolCategory src)
    {
        Assert.False(CategoryCompatibility.IsMoveLegal(src, CobolCategory.Numeric),
            $"{src} → Numeric should be illegal");
    }

    [Theory]
    [InlineData(CobolCategory.Alphanumeric)]
    [InlineData(CobolCategory.AlphanumericEdited)]
    [InlineData(CobolCategory.National)]
    [InlineData(CobolCategory.NationalEdited)]
    public void Non_numeric_cannot_move_to_NumericEdited(CobolCategory src)
    {
        Assert.False(CategoryCompatibility.IsMoveLegal(src, CobolCategory.NumericEdited),
            $"{src} → NumericEdited should be illegal");
    }

    [Fact]
    public void NumericEdited_can_move_to_NumericEdited()
    {
        Assert.True(CategoryCompatibility.IsMoveLegal(
            CobolCategory.NumericEdited, CobolCategory.NumericEdited));
    }

    [Fact]
    public void Move_matrix_is_complete_and_consistent()
    {
        // Verify every legal combination has a LoweringTable entry
        foreach (var src in AllCategories)
        foreach (var dst in AllCategories)
        {
            var legal = CategoryCompatibility.IsMoveLegal(src, dst);
            var helper = LoweringTable.ResolveHelper(OperationKind.Move, src, dst);

            if (legal)
                Assert.NotNull(helper);
            else
                Assert.Null(helper);
        }
    }

    // ══════════════════════════════════════
    // Arithmetic matrix
    // ══════════════════════════════════════

    [Fact]
    public void Only_Numeric_is_legal_arithmetic_operand()
    {
        foreach (var c in AllCategories)
        {
            var legal = CategoryCompatibility.IsArithmeticOperand(c);
            if (c == CobolCategory.Numeric)
                Assert.True(legal, "Numeric should be legal operand");
            else
                Assert.False(legal, $"{c} should not be legal operand");
        }
    }

    [Fact]
    public void Only_Numeric_and_NumericEdited_are_legal_results()
    {
        foreach (var c in AllCategories)
        {
            var legal = CategoryCompatibility.IsArithmeticResult(c);
            if (c is CobolCategory.Numeric or CobolCategory.NumericEdited)
                Assert.True(legal, $"{c} should be legal result");
            else
                Assert.False(legal, $"{c} should not be legal result");
        }
    }

    [Fact]
    public void Lowering_respects_arithmetic_compatibility()
    {
        var ops = new[] { OperationKind.Add, OperationKind.Subtract,
                          OperationKind.Multiply, OperationKind.Divide };

        foreach (var op in ops)
        foreach (var operandCat in AllCategories)
        foreach (var resultCat in AllCategories)
        {
            var helper = LoweringTable.ResolveHelper(op, operandCat, resultCat);
            var legalOperand = CategoryCompatibility.IsArithmeticOperand(operandCat);
            var legalResult = CategoryCompatibility.IsArithmeticResult(resultCat);

            if (legalOperand && legalResult)
                Assert.NotNull(helper);
            else
                Assert.Null(helper);
        }
    }

    // ══════════════════════════════════════
    // Comparison matrix
    // ══════════════════════════════════════

    [Theory]
    [InlineData(CobolCategory.Numeric, CobolCategory.Numeric, true)]
    [InlineData(CobolCategory.Numeric, CobolCategory.NumericEdited, true)]
    [InlineData(CobolCategory.NumericEdited, CobolCategory.Numeric, true)]
    [InlineData(CobolCategory.Alphanumeric, CobolCategory.Alphanumeric, true)]
    [InlineData(CobolCategory.Alphanumeric, CobolCategory.AlphanumericEdited, true)]
    [InlineData(CobolCategory.National, CobolCategory.National, true)]
    [InlineData(CobolCategory.National, CobolCategory.NationalEdited, true)]
    [InlineData(CobolCategory.Numeric, CobolCategory.Alphanumeric, false)]
    [InlineData(CobolCategory.Numeric, CobolCategory.National, false)]
    [InlineData(CobolCategory.Alphanumeric, CobolCategory.National, false)]
    public void Comparison_legality_matches_family_rules(
        CobolCategory left, CobolCategory right, bool expected)
    {
        Assert.Equal(expected, CategoryCompatibility.IsComparisonLegal(left, right));
    }

    [Fact]
    public void Lowering_respects_comparison_compatibility()
    {
        foreach (var left in AllCategories)
        foreach (var right in AllCategories)
        {
            var legal = CategoryCompatibility.IsComparisonLegal(left, right);
            var helper = LoweringTable.ResolveHelper(OperationKind.Compare, left, right);

            if (legal)
                Assert.NotNull(helper);
            else
                Assert.Null(helper);
        }
    }

    // ══════════════════════════════════════
    // Family helpers
    // ══════════════════════════════════════

    [Theory]
    [InlineData(CobolCategory.Numeric, true)]
    [InlineData(CobolCategory.NumericEdited, true)]
    [InlineData(CobolCategory.Alphanumeric, false)]
    public void IsNumericFamily(CobolCategory c, bool expected)
        => Assert.Equal(expected, CategoryCompatibility.IsNumericFamily(c));

    [Theory]
    [InlineData(CobolCategory.Alphanumeric, true)]
    [InlineData(CobolCategory.AlphanumericEdited, true)]
    [InlineData(CobolCategory.Numeric, false)]
    public void IsAlphanumericFamily(CobolCategory c, bool expected)
        => Assert.Equal(expected, CategoryCompatibility.IsAlphanumericFamily(c));

    [Theory]
    [InlineData(CobolCategory.National, true)]
    [InlineData(CobolCategory.NationalEdited, true)]
    [InlineData(CobolCategory.Numeric, false)]
    public void IsNationalFamily(CobolCategory c, bool expected)
        => Assert.Equal(expected, CategoryCompatibility.IsNationalFamily(c));
}
