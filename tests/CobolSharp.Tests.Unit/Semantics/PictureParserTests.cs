using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class PictureParserTests
{
    // ── Basic numeric ──

    [Fact]
    public void Parse_SimpleNumeric()
    {
        var pic = PictureParser.Parse("9(5)");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(0, pic.DecimalDigits);
        Assert.Equal(5, pic.Size);
        Assert.False(pic.IsSigned);
        Assert.False(pic.IsEdited);
    }

    [Fact]
    public void Parse_NumericWithDecimal()
    {
        var pic = PictureParser.Parse("9(3)V99");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(3, pic.IntegerDigits);
        Assert.Equal(2, pic.DecimalDigits);
        Assert.Equal(5, pic.Size); // display size: 3 integer + 2 decimal (V has no display pos)
    }

    [Fact]
    public void Parse_SignedNumeric()
    {
        var pic = PictureParser.Parse("S9(5)V99");
        Assert.True(pic.IsSigned);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(2, pic.DecimalDigits);
        Assert.Equal(7, pic.Size); // S has no display position
    }

    [Fact]
    public void Parse_RepeatedNines()
    {
        var pic = PictureParser.Parse("99999");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(5, pic.Size);
    }

    // ── Alphanumeric ──

    [Fact]
    public void Parse_Alphanumeric()
    {
        var pic = PictureParser.Parse("X(10)");
        Assert.Equal(PictureCategory.Alphanumeric, pic.Category);
        Assert.Equal(10, pic.Size);
    }

    [Fact]
    public void Parse_SimpleX()
    {
        var pic = PictureParser.Parse("XX");
        Assert.Equal(PictureCategory.Alphanumeric, pic.Category);
        Assert.Equal(2, pic.Size);
    }

    // ── Alphabetic ──

    [Fact]
    public void Parse_Alphabetic()
    {
        var pic = PictureParser.Parse("A(5)");
        Assert.Equal(PictureCategory.Alphabetic, pic.Category);
        Assert.Equal(5, pic.Size);
    }

    // ── Numeric edited: zero suppression ──

    [Fact]
    public void Parse_ZeroSuppressed()
    {
        var pic = PictureParser.Parse("Z(4)9");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(5, pic.Size);
        Assert.Equal(5, pic.IntegerDigits); // 4 Z + 1 nine
        Assert.True(pic.IsEdited);
    }

    [Fact]
    public void Parse_AsteriskSuppressed()
    {
        var pic = PictureParser.Parse("**(3)9.99");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsEdited);
    }

    // ── Numeric edited: floating signs ──

    [Fact]
    public void Parse_FloatingPlus()
    {
        var pic = PictureParser.Parse("+(4)9");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsSigned);
        Assert.True(pic.IsEdited);
        Assert.Equal(5, pic.Size);
        // First + is sign, remaining 3 are digit positions
        Assert.Equal(3 + 1, pic.IntegerDigits); // 3 from floating + (count-1), 1 from 9
    }

    [Fact]
    public void Parse_FloatingMinus()
    {
        var pic = PictureParser.Parse("--9");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsSigned);
        Assert.Equal(3, pic.Size);
    }

    // ── Numeric edited: currency ──

    [Fact]
    public void Parse_FloatingCurrency()
    {
        var pic = PictureParser.Parse("$$,$$9.99");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsEdited);
        // $$ = 1 digit pos (2 $, first is currency marker), $$9 = 3 digit pos
        // .99 = 2 decimal digits
        Assert.Equal(9, pic.Size); // $,$,$,9.99 = 9 chars
    }

    // ── Numeric edited: CR/DB ──

    [Fact]
    public void Parse_CreditSymbol()
    {
        var pic = PictureParser.Parse("9(5)CR");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsSigned);
        Assert.Equal(7, pic.Size); // 5 digits + 2 for CR
    }

    [Fact]
    public void Parse_DebitSymbol()
    {
        var pic = PictureParser.Parse("9(5)DB");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsSigned);
        Assert.Equal(7, pic.Size);
    }

    // ── Numeric edited: insertion characters ──

    [Fact]
    public void Parse_CommaInsertion()
    {
        var pic = PictureParser.Parse("9(3),9(3)");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(7, pic.Size); // 3 + comma + 3
    }

    [Fact]
    public void Parse_SlashInsertion()
    {
        var pic = PictureParser.Parse("99/99/9999");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(10, pic.Size);
    }

    [Fact]
    public void Parse_ZeroInsertion()
    {
        var pic = PictureParser.Parse("990099");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(6, pic.Size);
    }

    [Fact]
    public void Parse_BlankInsertion()
    {
        var pic = PictureParser.Parse("9(3)B9(3)");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(7, pic.Size);
    }

    // ── Numeric edited: decimal point ──

    [Fact]
    public void Parse_ActualDecimalPoint()
    {
        var pic = PictureParser.Parse("9(3).99");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.Equal(6, pic.Size); // 3 + dot + 2
        Assert.Equal(3, pic.IntegerDigits);
        Assert.Equal(2, pic.DecimalDigits);
    }

    // ── Alphanumeric edited ──

    [Fact]
    public void Parse_AlphanumericEdited()
    {
        var pic = PictureParser.Parse("X(5)BX(5)");
        Assert.Equal(PictureCategory.AlphanumericEdited, pic.Category);
        Assert.Equal(11, pic.Size); // 5 + B + 5
        Assert.True(pic.IsEdited);
    }

    // ── P (scaling) ──

    [Fact]
    public void Parse_ScalingP()
    {
        var pic = PictureParser.Parse("9(3)PPP");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(6, pic.IntegerDigits); // 3 nines + 3 P's
        Assert.Equal(3, pic.Size); // P has no display position
    }

    // ── Complex combinations ──

    [Fact]
    public void Parse_ComplexEditedNumeric()
    {
        // Common accounting format
        var pic = PictureParser.Parse("$$$,$$9.99CR");
        Assert.Equal(PictureCategory.NumericEdited, pic.Category);
        Assert.True(pic.IsSigned);
        Assert.True(pic.IsEdited);
    }

    // ── Symbol expansion ──

    [Fact]
    public void Parse_SymbolsPreserved()
    {
        var pic = PictureParser.Parse("S9(3)V99");
        Assert.NotNull(pic.Symbols);
        Assert.True(pic.Symbols.Count > 0);
        Assert.Equal(PictureSymbolKind.Sign, pic.Symbols[0].Kind);
    }
}
